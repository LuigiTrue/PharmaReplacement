using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Infrastructure.Repositories.Interfaces;

namespace RepyPharma.Infrastructure.Repositories;

public class ItemRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IItemRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<List<Item>> GetAllAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Items
            .AsNoTracking()
            .Include(item => item.ReplenishmentRule)
            .OrderBy(item => item.Name)
            .ToListAsync();
    }

    public async Task<Item?> GetByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Items
            .AsNoTracking()
            .Include(item => item.ReplenishmentRule)
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<Item?> GetByCodeAsync(string code)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Items
            .AsNoTracking()
            .Include(item => item.ReplenishmentRule)
            .FirstOrDefaultAsync(item => item.Code == code);
    }

    public async Task<List<Item>> SearchByNameAsync(string searchTerm)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Items
            .AsNoTracking()
            .Where(item => EF.Functions.ILike(item.Name, $"%{searchTerm}%"))
            .OrderBy(item => item.Name)
            .ToListAsync();
    }

    public async Task AddAsync(Item item)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        await context.Items.AddAsync(item);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Item item)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        context.Items.Update(item);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var item = await context.Items.FindAsync(id);
        if (item is null)
            return;

        context.Items.Remove(item);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Items
            .AsNoTracking()
            .AnyAsync(item => item.Code == code);
    }
}
