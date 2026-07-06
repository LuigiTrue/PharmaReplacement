using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Infrastructure.Repositories.Interfaces;

namespace RepyPharma.Infrastructure.Repositories;

public class LocationRepository(IDbContextFactory<AppDbContext> dbContextFactory) : ILocationRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<List<Location>> GetAllAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Locations
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .ToListAsync();
    }

    public async Task<Location?> GetByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(location => location.Id == id);
    }

    public async Task<Location?> GetByCodeAsync(string code)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(location => location.Code == code);
    }

    public async Task AddAsync(Location location)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        await context.Locations.AddAsync(location);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Location location)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        context.Locations.Update(location);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var location = await context.Locations.FindAsync(id);
        if (location is null)
            return;

        context.Locations.Remove(location);
        await context.SaveChangesAsync();
    }
}
