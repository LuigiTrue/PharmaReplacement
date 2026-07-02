namespace RepyPharma.Models;

public class ReplacementSettingsItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal MinimumQuantity { get; set; }
    public ItemPriority ItemPriority { get; set; }
}
