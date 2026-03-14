using UglyToad.PdfPig;
using System.Text.RegularExpressions;

public class PdfParserService
{
    public string ExtractText(string filePath)
    {
        using var document = PdfDocument.Open(filePath);

        var text = "";

        foreach (var page in document.GetPages())
        {
            text += page.Text;
        }

        return text;
    }

    public List<ProductStock> Parse(string text)
    {
        var produtos = new List<ProductStock>();

        var regex = new Regex(@"\b(\d{3,5})([A-Z][A-Z\s\-\/]+)");

        var matches = regex.Matches(text);

        foreach (Match match in matches)
        {
            var codigo = match.Groups[1].Value;

            var nome = match.Groups[2].Value;

            var produto = new ProductStock
            {
                Code = codigo,
                Name = nome.Trim()
            };

            produtos.Add(produto);
        }

        return produtos;
    }
}
