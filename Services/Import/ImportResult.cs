namespace RepyPharma.Services.Import;

public class ImportResult
{
    public int ItemsCreated { get; set; }
    public int ItemsUpdated { get; set; }
    public int BatchesCreated { get; set; }
    public int LocationsCreated { get; set; }
    public int StockBalancesCreated { get; set; }
    public int StockBalancesUpdated { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = new();

    public void AddError(string message)
    {
        Errors++;
        ErrorMessages.Add(message);
    }

}
