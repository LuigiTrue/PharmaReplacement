using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Infrastructure.Repositories.Interfaces;

namespace RepyPharma.Infrastructure.Repositories;

public class BatchRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IBatchRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<List<Batch>> GetByItemIdAsync(int itemId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Batches
            .AsNoTracking()
            .Where(batch => batch.ItemId == itemId)
            .OrderBy(batch => batch.Validity)
            .ToListAsync();
    }

    public async Task<Batch?> GetByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Batches
            .AsNoTracking()
            .FirstOrDefaultAsync(batch => batch.Id == id);
    }

    public async Task<Batch?> GetByItemAndBatchNumberAsync(int itemId, string batchNumber)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Batches
            .AsNoTracking()
            .FirstOrDefaultAsync(batch => batch.ItemId == itemId && batch.BatchNumber == batchNumber);
    }

    public async Task AddAsync(Batch batch)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        await context.Batches.AddAsync(batch);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Batch batch)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        context.Batches.Update(batch);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var batch = await context.Batches.FindAsync(id);
        if (batch is null)
            return;

        context.Batches.Remove(batch);
        await context.SaveChangesAsync();
    }
}
