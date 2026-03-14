using System.Text;

public class PdfValidationService
{
    public bool IsValidHospitalStockPdf(string text)
    {
        if (!text.Contains("SOULMV - Sistema de Gerenciamento de Estoque"))
            return false;

        if (!text.Contains("Relatório de Conferência dos Lotes"))
            return false;

        if (!text.Contains("Produto"))
            return false;

        return true;
    }
}
