using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class DanfeService(IDbContextFactory<AppDbContext> dbContextFactory) : IDanfeService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<Danfe?> GetDanfeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Danfes.AsNoTracking()
            .FirstOrDefaultAsync(danfe => danfe.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetAllDanfesAsync(CancellationToken cancellationToken = default)
    {
        return await GetDanfesAsync(danfe => true, cancellationToken);
    }

    public async Task<Danfe> CreateDanfeAsync(Danfe danfe, CancellationToken cancellationToken = default)
    {
        ValidateDanfe(danfe);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await ValidateRelationshipsAsync(context, danfe, cancellationToken);
        await EnsureUniqueNumberAsync(context, danfe, null, cancellationToken);

        // Copia apenas campos editáveis para não inserir o grafo de navegação recebido.
        var entity = new Danfe();
        CopyEditableProperties(danfe, entity);
        await context.Danfes.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Danfe?> UpdateDanfeAsync(int id, Danfe danfe, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(danfe);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Danfes.FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (entity is null)
            return null;

        ValidateDanfe(danfe);
        await ValidateRelationshipsAsync(context, danfe, cancellationToken);
        await EnsureUniqueNumberAsync(context, danfe, id, cancellationToken);

        CopyEditableProperties(danfe, entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> DeleteDanfeAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Danfes.FirstOrDefaultAsync(danfe => danfe.Id == id, cancellationToken);
        if (entity is null)
            return false;

        context.Danfes.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesByQuotationAsync(int quotationId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quotationId);
        return await GetDanfesAsync(danfe => danfe.QuotationId == quotationId, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesBySupplierAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(supplierId);
        return await GetDanfesAsync(danfe => danfe.SupplierId == supplierId, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesByQuotationNumberAsync(string quotationNumber, CancellationToken cancellationToken = default)
    {
        var number = NormalizeNumber(quotationNumber, 50, nameof(quotationNumber));
        return await GetDanfesAsync(danfe => danfe.Quotation.QuotationNumber == number, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesByPaymentDueDateAsync(DateTime dueDate, CancellationToken cancellationToken = default)
    {
        var date = dueDate.Date;
        return await GetDanfesAsync(danfe => danfe.PaymentDueAt == date, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesByPaymentDueDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        ValidateDateRange(start, end);
        return await GetDanfesAsync(danfe => danfe.PaymentDueAt >= start && danfe.PaymentDueAt <= end, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesDueSoonAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(daysAhead);
        var today = DateTime.UtcNow.Date;
        if (daysAhead > (DateTime.MaxValue.Date - today).Days)
            throw new ArgumentOutOfRangeException(nameof(daysAhead), "O intervalo excede a data máxima suportada.");

        var end = today.AddDays(daysAhead);
        return await GetDanfesAsync(danfe => danfe.PaymentDueAt >= today && danfe.PaymentDueAt <= end, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetOverdueDanfesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await GetDanfesAsync(danfe => danfe.PaymentDueAt < today, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesWithoutPaymentDueDateAsync(CancellationToken cancellationToken = default)
    {
        return await GetDanfesAsync(danfe => danfe.PaymentDueAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesByReceivedDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        ValidateDateRange(start, end);
        return await GetDanfesAsync(danfe => danfe.ReceivedAt >= start && danfe.ReceivedAt <= end, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesNotReceivedAsync(CancellationToken cancellationToken = default)
    {
        return await GetDanfesAsync(danfe => danfe.ReceivedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetThermolabileDanfesAsync(CancellationToken cancellationToken = default)
    {
        return await GetDanfesAsync(danfe => danfe.IsThermolabile, cancellationToken);
    }

    public async Task<IReadOnlyList<Danfe>> GetDanfesWithBoletoAsync(CancellationToken cancellationToken = default)
    {
        return await GetDanfesAsync(danfe => danfe.Boleto, cancellationToken);
    }

    public async Task<Danfe?> GetDanfeByNumberAsync(string danfeNumber, int supplierId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(supplierId);
        var number = NormalizeNumber(danfeNumber, 255, nameof(danfeNumber));
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Danfes.AsNoTracking()
            .Where(danfe => danfe.SupplierId == supplierId && danfe.DanfeNumber == number)
            .OrderBy(danfe => danfe.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Danfe>> GetDanfesAsync(Expression<Func<Danfe, bool>> predicate, CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Danfes.AsNoTracking()
            .Where(predicate)
            .OrderBy(danfe => danfe.Id)
            .ToListAsync(cancellationToken);
    }

    private static void ValidateDanfe(Danfe danfe)
    {
        ArgumentNullException.ThrowIfNull(danfe);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(danfe.SupplierId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(danfe.QuotationId);
        NormalizeNumber(danfe.DanfeNumber, 255, nameof(danfe.DanfeNumber));
        if (danfe.UserReceiver?.Length > 255)
            throw new ArgumentException("O responsável pelo recebimento deve ter até 255 caracteres.", nameof(danfe));
    }

    private static string NormalizeNumber(string number, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, parameterName);
        var normalized = number.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"O número deve ter até {maxLength} caracteres.", parameterName);

        return normalized;
    }

    private static void ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("A data inicial não pode ser posterior à data final.", nameof(endDate));
    }

    private static async Task ValidateRelationshipsAsync(AppDbContext context, Danfe danfe, CancellationToken cancellationToken)
    {
        if (!await context.Suppliers.AsNoTracking().AnyAsync(supplier => supplier.Id == danfe.SupplierId, cancellationToken))
            throw new ArgumentException("Fornecedor não encontrado.", nameof(danfe));

        if (!await context.Quotations.AsNoTracking().AnyAsync(quotation => quotation.Id == danfe.QuotationId, cancellationToken))
            throw new ArgumentException("Cotação não encontrada.", nameof(danfe));
    }

    private static async Task EnsureUniqueNumberAsync(AppDbContext context, Danfe danfe, int? excludedId, CancellationToken cancellationToken)
    {
        var number = danfe.DanfeNumber.Trim();
        var query = context.Danfes.AsNoTracking()
            .Where(existing => existing.SupplierId == danfe.SupplierId && existing.DanfeNumber == number);
        if (excludedId is int id)
            query = query.Where(existing => existing.Id != id);

        if (await query.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Já existe uma DANFE com este número para o fornecedor informado.");
        }
    }

    private static void CopyEditableProperties(Danfe source, Danfe target)
    {
        target.SupplierId = source.SupplierId;
        target.DanfeNumber = source.DanfeNumber.Trim();
        target.Value = source.Value;
        target.Boleto = source.Boleto;
        target.UserReceiver = source.UserReceiver;
        target.ReceivedAt = source.ReceivedAt?.Date;
        target.QuotationId = source.QuotationId;
        target.IsThermolabile = source.IsThermolabile;
        target.ItemsQuantity = source.ItemsQuantity;
        target.ContainsPurchase = source.ContainsPurchase;
        target.PaymentDueAt = source.PaymentDueAt?.Date;
    }
}
