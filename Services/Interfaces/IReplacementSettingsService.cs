using RepyPharma.Models;
using RepyPharma.Domain.Entities;

namespace RepyPharma.Services.Interfaces;

public interface IReplacementSettingsService
{
    Task<List<ReplacementSettingsItem>> SearchPriorityItemsAsync(string searchText);
    Task<ReplacementSettingsItem?> GetPriorityItemAsync(string code);
    Task UpdateItemSettingsAsync(string code, ItemPriority priority, decimal minimumQuantity, ItemType itemType);
    Task AddMinimumStockItemAsync(string code, string name, ItemPriority priority, decimal minimumQuantity);
}
