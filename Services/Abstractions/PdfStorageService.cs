using System.Text.Json;

public class PdfStorageService
{
    private readonly string path = "storage/estoque.json";

    public async Task SaveAsync(List<ProductStock> produtos)
    {
        var json = JsonSerializer.Serialize(produtos, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(path, json);
    }
}
