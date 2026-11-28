namespace RepyPharma.Services.Import;

/// <summary>Falha na obtenção do custo mensal de um produto; impede atribuição de custo arbitrário.</summary>
internal sealed class UnitPriceProcessingException(string code, string productCode, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public string Code { get; } = code;
    public string ProductCode { get; } = productCode;
}
