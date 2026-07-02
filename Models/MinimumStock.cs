using System.Text.Json.Serialization;

public class MinimumStock
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal MinimumQuantity { get; set; }

    [JsonPropertyName("ItemPriority")]
    public ItemPriority itemPriority { get; set; }
}
