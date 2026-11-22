using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class QuotationService(IDbContextFactory<AppDbContext> dbContextFactory) : IQuotationService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<Quotation?> GetQuotationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Quotations.AsNoTracking()
            .FirstOrDefaultAsync(quotation => quotation.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Quotation>> GetAllQuotationsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Quotations.AsNoTracking()
            .OrderBy(quotation => quotation.QuotationNumber)
            .ThenBy(quotation => quotation.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Quotation?> GetQuotationByNumberAsync(string quotationNumber, CancellationToken cancellationToken = default)
    {
        var number = NormalizeNumber(quotationNumber);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Quotations.AsNoTracking()
            .FirstOrDefaultAsync(quotation => quotation.QuotationNumber == number, cancellationToken);
    }

    public async Task<Quotation> CreateQuotationAsync(Quotation quotation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quotation);
        var number = NormalizeNumber(quotation.QuotationNumber);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureUniqueNumberAsync(context, number, null, cancellationToken);

        var entity = new Quotation { QuotationNumber = number };
        await context.Quotations.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Quotation?> UpdateQuotationAsync(int id, Quotation quotation, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(quotation);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Quotations.FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (entity is null)
            return null;

        var number = NormalizeNumber(quotation.QuotationNumber);
        await EnsureUniqueNumberAsync(context, number, id, cancellationToken);
        entity.QuotationNumber = number;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> DeleteQuotationAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Quotations.FirstOrDefaultAsync(quotation => quotation.Id == id, cancellationToken);
        if (entity is null)
            return false;

        context.Quotations.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeNumber(string quotationNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quotationNumber);
        var number = quotationNumber.Trim();
        if (number.Length > 50)
            throw new ArgumentException("O número da cotação deve ter até 50 caracteres.", nameof(quotationNumber));

        return number;
    }

    private static async Task EnsureUniqueNumberAsync(AppDbContext context, string number, int? excludedId, CancellationToken cancellationToken)
    {
        var query = context.Quotations.AsNoTracking().Where(quotation => quotation.QuotationNumber == number);
        if (excludedId is int id)
            query = query.Where(quotation => quotation.Id != id);

        if (await query.AnyAsync(cancellationToken))
            throw new InvalidOperationException("Já existe uma cotação com este número.");
    }
}
