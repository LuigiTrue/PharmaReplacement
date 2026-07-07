namespace RepyPharma.Models;

using RepyPharma.Domain.Entities;

public class ReplacementSettingsItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal MinimumQuantity { get; set; }
    public ItemPriority ItemPriority { get; set; }
    public ItemType ItemType { get; set; } = ItemType.CommonMedication;
}
