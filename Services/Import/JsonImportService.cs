using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain;
using RepyPharma.Domain.Entities;
using RepyPharma.Services.Import.Dtos;
using RepyPharma.Services.Import.Interfaces;

namespace RepyPharma.Services.Import;

public class JsonImportService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<JsonImportService> logger,
    StockDataState stockDataState) : IJsonImportService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<JsonImportService> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ImportResult> ImportStockAsync(string filePath)
    {
        var result = new ImportResult();

        if (!File.Exists(filePath))
        {
            result.AddError($"Arquivo de estoque não encontrado: {filePath}");
            _logger.LogWarning("Arquivo de estoque não encontrado: {FilePath}", filePath);
            return result;
        }

        List<StockJsonDto>? stockItems;
        try
        {
            await using var stream = File.OpenRead(filePath);
            stockItems = await JsonSerializer.DeserializeAsync<List<StockJsonDto>>(stream, _jsonOptions);
        }
        catch (Exception ex)
        {
            result.AddError($"Falha ao ler arquivo de estoque '{filePath}': {ex.Message}");
            _logger.LogError(ex, "Falha ao ler arquivo de estoque {FilePath}", filePath);
            return result;
        }

        if (stockItems is null || stockItems.Count == 0)
        {
            _logger.LogInformation("Arquivo de estoque sem itens para importar: {FilePath}", filePath);
            return result;
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var itemsByCode = await context.Items.ToDictionaryAsync(item => item.Code);
            var locationsByCode = await context.Locations.ToDictionaryAsync(location => location.Code);

            foreach (var stockItem in stockItems)
            {
                try
                {
                    await ImportStockItemAsync(context, stockItem, itemsByCode, locationsByCode, result);
                }
                catch (DbUpdateException ex)
                {
                    var code = string.IsNullOrWhiteSpace(stockItem.Code) ? "<sem codigo>" : stockItem.Code;
                    var errorMessage = GetExceptionMessage(ex);
                    result.AddError($"Erro ao importar item {code}: {errorMessage}");
                    _logger.LogError(
                        ex,
                        "Erro ao importar item de estoque {Code}. InnerException: {InnerExceptionMessage}",
                        code,
                        ex.InnerException?.Message);
                }
                catch (Exception ex)
                {
                    var code = string.IsNullOrWhiteSpace(stockItem.Code) ? "<sem codigo>" : stockItem.Code;
                    result.AddError($"Erro ao importar item {code}: {ex.Message}");
                    _logger.LogError(ex, "Erro ao importar item de estoque {Code}", code);
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            var errorMessage = GetExceptionMessage(ex);
            result.AddError($"Falha na transação de importação de estoque: {errorMessage}");
            _logger.LogError(
                ex,
                "Falha na transação de importação de estoque. InnerException: {InnerExceptionMessage}",
                ex.InnerException?.Message);
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.AddError($"Falha na transação de importação de estoque: {ex.Message}");
            _logger.LogError(ex, "Falha na transação de importação de estoque");
            return result;
        }

        _logger.LogInformation(
            "Importação de estoque concluída. Itens criados: {ItemsCreated}, itens atualizados: {ItemsUpdated}, lotes criados: {BatchesCreated}, localizações criadas: {LocationsCreated}, saldos criados: {StockBalancesCreated}, saldos atualizados: {StockBalancesUpdated}, erros: {Errors}",
            result.ItemsCreated,
            result.ItemsUpdated,
            result.BatchesCreated,
            result.LocationsCreated,
            result.StockBalancesCreated,
            result.StockBalancesUpdated,
            result.Errors);

        stockDataState.NotifyChanged();
        return result;
    }

    private static async Task ImportStockItemAsync(
        AppDbContext context,
        StockJsonDto stockItem,
        Dictionary<string, Item> itemsByCode,
        Dictionary<string, Location> locationsByCode,
        ImportResult result)
    {
        if (string.IsNullOrWhiteSpace(stockItem.Code))
        {
            result.AddError("Item ignorado porque o campo Code está vazio.");
            return;
        }

        if (!itemsByCode.TryGetValue(stockItem.Code, out var item))
        {
            item = new Item
            {
                Code = stockItem.Code.Trim(),
                Name = stockItem.Name.Trim(),
                Unit = stockItem.Unit.Trim(),
                ItemType = ItemTypeClassifier.Classify(stockItem.Name),
                IsActive = true,
                CreatedAt = ToUtc(DateTime.UtcNow)
            };

            await context.Items.AddAsync(item);
            await context.SaveChangesAsync();
            itemsByCode[item.Code] = item;
            result.ItemsCreated++;
        }
        else if (item.Name != stockItem.Name || item.Unit != stockItem.Unit)
        {
            item.Name = stockItem.Name.Trim();
            item.Unit = stockItem.Unit.Trim();
            item.ItemType = ItemTypeClassifier.Classify(stockItem.Name);
            item.UpdatedAt = ToUtc(DateTime.UtcNow);
            result.ItemsUpdated++;
        }

        foreach (var batchDto in stockItem.Batches)
        {
            await ImportBatchAsync(context, item, batchDto, locationsByCode, result);
        }
    }

    private static async Task ImportBatchAsync(
        AppDbContext context,
        Item item,
        BatchJsonDto batchDto,
        Dictionary<string, Location> locationsByCode,
        ImportResult result)
    {
        if (string.IsNullOrWhiteSpace(batchDto.Batch))
        {
            result.AddError($"Lote ignorado para o item {item.Code} porque o campo Batch está vazio.");
            return;
        }

        if (batchDto.Validity is null || IsInvalidDate(batchDto.Validity.Value))
        {
            result.AddError($"Lote {batchDto.Batch} do item {item.Code} ignorado porque Validity está vazio ou inválido.");
            return;
        }

        var batchNumber = batchDto.Batch.Trim();
        var validity = ToUtc(batchDto.Validity.Value);
        var batch = await context.Batches
            .FirstOrDefaultAsync(existingBatch =>
                existingBatch.ItemId == item.Id &&
                existingBatch.BatchNumber == batchNumber);

        if (batch is null)
        {
            batch = new Batch
            {
                Item = item,
                BatchNumber = batchNumber,
                Validity = validity,
                CreatedAt = ToUtc(DateTime.UtcNow)
            };

            await context.Batches.AddAsync(batch);
            await context.SaveChangesAsync();
            result.BatchesCreated++;
        }
        else if (batch.Validity != validity)
        {
            batch.Validity = validity;
            batch.UpdatedAt = ToUtc(DateTime.UtcNow);
        }

        foreach (var locationDto in batchDto.Locations)
        {
            await ImportStockBalanceAsync(context, item, batch, locationDto, locationsByCode, result);
        }
    }

    private static async Task ImportStockBalanceAsync(
        AppDbContext context,
        Item item,
        Batch batch,
        LocationJsonDto locationDto,
        Dictionary<string, Location> locationsByCode,
        ImportResult result)
    {
        if (string.IsNullOrWhiteSpace(locationDto.LocationId))
        {
            result.AddError($"Localização ignorada para item {item.Code}, lote {batch.BatchNumber}, porque LocationId está vazio.");
            return;
        }

        var locationCode = locationDto.LocationId.Trim();
        if (!locationsByCode.TryGetValue(locationCode, out var location))
        {
            location = new Location
            {
                Code = locationCode,
                Name = locationCode,
                IsActive = true
            };

            await context.Locations.AddAsync(location);
            await context.SaveChangesAsync();
            locationsByCode[location.Code] = location;
            result.LocationsCreated++;
        }

        var stockBalance = await context.StockBalances
            .FirstOrDefaultAsync(balance =>
                balance.ItemId == item.Id &&
                balance.BatchId == batch.Id &&
                balance.LocationId == location.Id);

        if (stockBalance is null)
        {
            stockBalance = new StockBalance
            {
                Item = item,
                Batch = batch,
                Location = location,
                Quantity = locationDto.Quantity,
                UpdatedAt = ToUtc(DateTime.UtcNow)
            };

            await context.StockBalances.AddAsync(stockBalance);
            result.StockBalancesCreated++;
            return;
        }

        stockBalance.Quantity = locationDto.Quantity;
        stockBalance.UpdatedAt = ToUtc(DateTime.UtcNow);
        result.StockBalancesUpdated++;
    }

    private static DateTime ToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Utc)
            return date;

        if (date.Kind == DateTimeKind.Local)
            return date.ToUniversalTime();

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    private static bool IsInvalidDate(DateTime date)
    {
        return date == default || date == DateTime.MinValue;
    }

    private static string GetExceptionMessage(DbUpdateException ex)
    {
        return ex.InnerException is null
            ? ex.Message
            : $"{ex.Message} InnerException: {ex.InnerException.Message}";
    }
}
