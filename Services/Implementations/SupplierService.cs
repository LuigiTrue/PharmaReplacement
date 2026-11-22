using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class SupplierService(IDbContextFactory<AppDbContext> dbContextFactory) : ISupplierService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Supplier>> GetAllSuppliersAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Suppliers.AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .ThenBy(supplier => supplier.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Supplier?> GetSupplierByCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpj);
        var normalizedCnpj = NormalizeCnpj(cnpj);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(supplier => supplier.Cnpj == normalizedCnpj, cancellationToken);
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ValidateSupplier(supplier);
        var cnpj = NormalizeCnpj(supplier.Cnpj);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureUniqueCnpjAsync(context, cnpj, null, cancellationToken);

        var entity = new Supplier
        {
            Name = supplier.Name.Trim(),
            Cnpj = cnpj,
            Observations = supplier.Observations
        };
        await context.Suppliers.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Supplier?> UpdateSupplierAsync(int id, Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(supplier);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Suppliers.FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (entity is null)
            return null;

        ValidateSupplier(supplier);
        var cnpj = NormalizeCnpj(supplier.Cnpj);
        await EnsureUniqueCnpjAsync(context, cnpj, id, cancellationToken);
        entity.Name = supplier.Name.Trim();
        entity.Cnpj = cnpj;
        entity.Observations = supplier.Observations;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> DeleteSupplierAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.Suppliers.FirstOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);
        if (entity is null)
            return false;

        context.Suppliers.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateSupplier(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        ArgumentException.ThrowIfNullOrWhiteSpace(supplier.Name);
        if (supplier.Name.Trim().Length > 255)
            throw new ArgumentException("O nome do fornecedor deve ter até 255 caracteres.", nameof(supplier));
        if (supplier.Observations?.Length > 255)
            throw new ArgumentException("As observações devem ter até 255 caracteres.", nameof(supplier));
    }

    private static string? NormalizeCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return null;

        var normalized = cnpj.Trim();
        if (normalized.Length > 14)
            throw new ArgumentException("O CNPJ deve ter até 14 caracteres.", nameof(cnpj));

        return normalized;
    }

    private static async Task EnsureUniqueCnpjAsync(AppDbContext context, string? cnpj, int? excludedId, CancellationToken cancellationToken)
    {
        if (cnpj is null)
            return;

        var query = context.Suppliers.AsNoTracking().Where(supplier => supplier.Cnpj == cnpj);
        if (excludedId is int id)
            query = query.Where(supplier => supplier.Id != id);

        if (await query.AnyAsync(cancellationToken))
            throw new InvalidOperationException("Já existe um fornecedor com este CNPJ.");
    }
}
