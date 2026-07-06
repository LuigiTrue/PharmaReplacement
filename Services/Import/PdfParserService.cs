using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;

namespace RepyPharma.Services.Import;

public class PdfParserService
{
    private const double CodeMinX = 15.5;
    private const double CodeMaxX = 52.0;
    private const double NameMinX = 52.0;
    private const double NameMaxX = 155.0;
    private const double UnitMinX = 155.0;
    private const double UnitMaxX = 212.0;
    private const double StockMinX = 213.0;
    private const double StockMaxX = 256.0;
    private const double BatchMinX = 256.0;
    private const double BatchMaxX = 311.0;
    private const double ValidityMinX = 311.2;
    private const double ValidityMaxX = 358.0;
    private const double LocationMinX = 356.1;
    private const double LocationMaxX = 387.0;
    private const double QuantityMinX = 460.0;
    private const double QuantityMaxX = 520.0;

    private const double MaxLineContinuationGap = 15.0;

    public string ExtractText(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        var text = "";
        foreach (var page in document.GetPages())
            text += page.Text;
        return text;
    }

    public List<ProductStock> ParseProducts(string path)
    {
        var products = new List<ProductStock>();
        string lastCode = "";
        int lastIndex = -1;

        using var document = PdfDocument.Open(path);
        var pages = document.GetPages().ToList();

        for (int p = 0; p < pages.Count; p++)
        {
            var page = pages[p];
            var allWords = page.GetWords().ToList();
            var productLines = GetProductLines(allWords);

            foreach (var line in productLines)
            {
                double yAtual = line.Key;
                string codigo = line.Value;

                var nome = ExtractFieldWithContinuation(allWords, yAtual, NameMinX, NameMaxX);
                var unidade = ExtractFieldWithContinuation(allWords, yAtual, UnitMinX, UnitMaxX);
                var estoque = ExtractTotalStock(allWords, yAtual);
                var lotes = ExtractBatches(allWords, yAtual);

                if (codigo == lastCode && lastIndex >= 0)
                {
                    MergeWithPrevious(products[lastIndex], nome, unidade, lotes);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(nome))
                    continue;

                var produto = new ProductStock
                {
                    Code = codigo,
                    Name = nome,
                    Unit = unidade,
                    TotalStock = estoque,
                    Batches = lotes
                };

                products.Add(produto);
                lastCode = codigo;
                lastIndex = products.Count - 1;
            }
        }

        return products;
    }

    private List<KeyValuePair<double, string>> GetProductLines(List<Word> allWords)
    {
        return allWords
            .Where(w => w.BoundingBox.Left >= CodeMinX
                     && w.BoundingBox.Left < CodeMaxX
                     && Regex.IsMatch(w.Text, @"^\d{3,6}$"))
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
            .OrderByDescending(g => g.Key)
            .Select(g => new KeyValuePair<double, string>(
                g.Key,
                g.OrderBy(w => w.BoundingBox.Left).First().Text.Trim()))
            .ToList();
    }

    private string ExtractFieldWithContinuation(
        List<Word> allWords, double yAtual, double minX, double maxX)
    {
        var wordsInColumn = allWords
            .Where(w => w.BoundingBox.Left >= minX && w.BoundingBox.Left < maxX)
            .ToList();

        var wordsCode = allWords
            .Where(w => w.BoundingBox.Left >= CodeMinX && w.BoundingBox.Left < CodeMaxX)
            .ToList();

        var texto = ExtractLineText(wordsInColumn, yAtual);

        var linhasAbaixo = wordsInColumn
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
            .Where(g => g.Key < yAtual)
            .OrderByDescending(g => g.Key)
            .ToList();

        double yRef = yAtual;
        foreach (var linha in linhasAbaixo)
        {
            double yProximo = linha.Key;

            if (Math.Abs(yRef - yProximo) > MaxLineContinuationGap)
                break;

            bool temCodigo = wordsCode
                .Any(w => Math.Round(w.BoundingBox.Bottom, 0) == yProximo
                       && Regex.IsMatch(w.Text, @"^\d{3,6}$"));

            if (temCodigo)
                break;

            var continuacao = ExtractLineText(wordsInColumn, yProximo);
            if (!string.IsNullOrWhiteSpace(continuacao))
                texto += " " + continuacao;

            yRef = yProximo;
        }

        return texto.Trim();
    }

    private decimal ExtractTotalStock(List<Word> allWords, double yAtual)
    {
        var texto = ExtractLineText(allWords
            .Where(w => w.BoundingBox.Left >= StockMinX && w.BoundingBox.Left < StockMaxX)
            .ToList(), yAtual);

        return ParseDecimal(texto);
    }

    private List<BatchStock> ExtractBatches(List<Word> allWords, double yAtual)
    {
        var batches = new List<BatchStock>();

        var wordsBatch = allWords.Where(w => w.BoundingBox.Left >= BatchMinX && w.BoundingBox.Left < BatchMaxX).ToList();
        var wordsValidity = allWords.Where(w => w.BoundingBox.Left >= ValidityMinX && w.BoundingBox.Left < ValidityMaxX).ToList();
        var wordsLocation = allWords.Where(w => w.BoundingBox.Left >= LocationMinX && w.BoundingBox.Left < LocationMaxX).ToList();
        var wordsQuantity = allWords.Where(w => w.BoundingBox.Left >= QuantityMinX && w.BoundingBox.Left < QuantityMaxX).ToList();
        var wordsCode = allWords.Where(w => w.BoundingBox.Left >= CodeMinX && w.BoundingBox.Left < CodeMaxX).ToList();

        var linhasLote = wordsBatch
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
            .Where(g => g.Key <= yAtual)
            .OrderByDescending(g => g.Key)
            .ToList();

        foreach (var linhaLote in linhasLote)
        {
            double yLote = linhaLote.Key;

            if (yLote < yAtual)
            {
                bool temCodigo = wordsCode
                    .Any(w => Math.Round(w.BoundingBox.Bottom, 0) == yLote
                           && Regex.IsMatch(w.Text, @"^\d{3,6}$"));

                if (temCodigo)
                    break;
            }

            var textoLote = ExtractLineText(wordsBatch, yLote);
            var validade = ParseDate(ExtractLineText(wordsValidity, yLote));
            var locationId = ExtractLineText(wordsLocation, yLote);
            var quantidade = ParseDecimal(ExtractLineText(wordsQuantity, yLote));


            if (!IsValidBatch(textoLote))
                continue;

            var batchExistente = batches.FirstOrDefault(b => b.Batch == textoLote);

            if (batchExistente != null)
            {
                batchExistente.Locations.Add(new StockLocation
                {
                    LocationId = locationId,
                    Quantity = quantidade
                });
            }
            else
            {
                batches.Add(new BatchStock
                {
                    Batch = textoLote,
                    Validity = validade,
                    Locations = new List<StockLocation>
                    {
                        new StockLocation
                        {
                            LocationId = locationId,
                            Quantity   = quantidade
                        }
                    }
                });
            }
        }

        return batches;
    }

    private bool IsValidBatch(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        return Regex.IsMatch(value, @"^[A-Z0-9\-/\.]+$", RegexOptions.IgnoreCase)
            && value.Any(char.IsDigit) && value != "DE LUZIANIA";
    }

    private void MergeWithPrevious(
        ProductStock produto, string nome, string unidade, List<BatchStock> lotes)
    {
        if (!string.IsNullOrWhiteSpace(nome))
            produto.Name = (produto.Name + " " + nome).Trim();

        if (!string.IsNullOrWhiteSpace(unidade))
            produto.Unit = (produto.Unit + " " + unidade).Trim();

        foreach (var lote in lotes)
        {
            var existente = produto.Batches.FirstOrDefault(b => b.Batch == lote.Batch);
            if (existente != null)
                existente.Locations.AddRange(lote.Locations);
            else
                produto.Batches.Add(lote);
        }
    }

    private string ExtractLineText(List<Word> words, double y)
    {
        return string.Join(" ", words
            .Where(w => Math.Round(w.BoundingBox.Bottom, 0) == y)
            .OrderBy(w => w.BoundingBox.Left)
            .Select(w => w.Text));
    }

    private decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        value = value.Replace(".", "").Replace(",", ".");
        return decimal.TryParse(value,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result) ? result : 0;
    }

    private DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var match = Regex.Match(value, @"\d{2}/\d{2}/\d{4}");
        if (!match.Success) return null;

        return DateTime.TryParseExact(
            match.Value,
            "dd/MM/yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var date) ? date : null;
    }
}
