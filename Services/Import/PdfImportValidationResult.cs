namespace RepyPharma.Services.Import;

public sealed class PdfImportValidationResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;

    public static PdfImportValidationResult Valid(string message = "Arquivo valido.")
    {
        return new PdfImportValidationResult
        {
            IsValid = true,
            Message = message
        };
    }

    public static PdfImportValidationResult Invalid(string message)
    {
        return new PdfImportValidationResult
        {
            IsValid = false,
            Message = message
        };
    }
}
