using System.Text.Json;

public class PdfStorageService
{
    private readonly string _path;

    public PdfStorageService(IWebHostEnvironment env)
    {
        var folder = Path.Combine(env.ContentRootPath, "storage");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        _path = Path.Combine(folder, "estoque.json");
    }

    public async Task SaveAsync(List<ProductStock> produtos)
    {
        var json = JsonSerializer.Serialize(produtos, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_path, json);
    }
}
