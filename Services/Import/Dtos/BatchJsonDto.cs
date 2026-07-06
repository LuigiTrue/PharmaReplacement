namespace RepyPharma.Services.Import.Dtos;

public class BatchJsonDto
{
    public string Batch { get; set; } = string.Empty;
    public DateTime? Validity { get; set; }
    public List<LocationJsonDto> Locations { get; set; } = new();
}
