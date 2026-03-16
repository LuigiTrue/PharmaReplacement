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

    public List<string> ExtractLines(string path)
    {
        var lines = new List<string>();

        using var document = PdfDocument.Open(path);

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords()
                .OrderByDescending(w => w.BoundingBox.Bottom)
                .ThenBy(w => w.BoundingBox.Left)
                .ToList();

            double currentY = -1;
            string currentLine = "";

            foreach (var word in words)
            {
                if (currentY == -1)
                    currentY = word.BoundingBox.Bottom;

                if (Math.Abs(word.BoundingBox.Bottom - currentY) > 2)
                {
                    lines.Add(currentLine);
                    currentLine = "";
                    currentY = word.BoundingBox.Bottom;
                }

                currentLine += word.Text + " ";
            }

            if (!string.IsNullOrWhiteSpace(currentLine))
                lines.Add(currentLine);
        }

        return lines;
    }

    public List<string> BuildProductLines(List<string> rawLines)
    {
        var result = new List<string>();

        // Código real: número seguido de letra (início do nome do produto)
        // Exclui linhas onde o número é seguido diretamente de uma data (= lote)
        var productStart = new Regex(@"^\d{3,5}\s+[A-ZÀ-Ú]");

        string current = null;

        foreach (var line in rawLines)
        {
            var clean = line.Trim();

            if (string.IsNullOrWhiteSpace(clean))
                continue;

            if (productStart.IsMatch(clean))
            {
                if (current != null)
                    result.Add(current);

                current = clean;
            }
            else
            {
                if (current != null)
                    current += " " + clean;
            }
        }

        if (current != null)
            result.Add(current);

        return result;
    }

    public List<ProductStock> ParseProducts(List<string> lines)
    {
        var produtos = new List<ProductStock>();

        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
                continue;

            if (!int.TryParse(parts[0], out _))
                continue;

            var produto = new ProductStock
            {
                Code = parts[0],
                Name = ExtractProductName(line),
                Unit = ExtractUnit(line),
                CurrentStock = ExtractCurrentStock(line),
                Batch = ExtractBatch(line),
                Validity = ExtractValidity(line),
                Quantity = ExtractQuantity(line),
                Manufacturer = ExtractManufacturer(line)
            };

            produtos.Add(produto);
        }

        return produtos;
    }

    private string ExtractProductName(string line)
    {
        line = RemovePageHeader(line);

        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length < 3)
            return "";

        var stockPattern = new Regex(@"^\d{1,3}(\.\d{3})*,\d{3}$");

        // Padrão que marca fim de um lote: [estoque_id] 0,000 [quantidade]
        // Estoque ID: 996, 997, 998, 999, 1059
        var stockIdPattern = new Regex(@"^(996|997|998|999|1059)$");

        // 1. Encontra o índice do estoque atual (primeiro número decimal da linha)
        int currentStockIndex = -1;
        for (int i = 1; i < tokens.Length; i++)
        {
            if (stockPattern.IsMatch(tokens[i]))
            {
                currentStockIndex = i;
                break;
            }
        }

        if (currentStockIndex == -1)
            return "";

        // Tokens entre código e estoque atual = "ACETILCISTEINA ENVELOPE"
        var part1Tokens = tokens.Skip(1).Take(currentStockIndex - 1).ToList();

        // 2. Encontra nome_parte2: busca padrão [stockId] [0,000] [quantidade] após o estoque atual
        // e captura tudo que vier depois da quantidade
        string part2 = "";

        for (int i = currentStockIndex + 1; i < tokens.Length - 2; i++)
        {
            if (stockIdPattern.IsMatch(tokens[i])
                && tokens[i + 1] == "0,000"
                && stockPattern.IsMatch(tokens[i + 2]))
            {
                // Tudo após tokens[i+2] até o próximo lote (ou fim) é nome_parte2
                int nameStart = i + 3;
                var part2Tokens = new List<string>();

                for (int j = nameStart; j < tokens.Length; j++)
                {
                    // Para se encontrar outro lote iniciando
                    // (lote é seguido de data dd/MM/yyyy)
                    if (j + 1 < tokens.Length
                        && Regex.IsMatch(tokens[j + 1], @"^\d{2}/\d{2}/\d{4}$"))
                        break;

                    // Para se encontrar outro stockId iniciando sequência de lote
                    if (stockIdPattern.IsMatch(tokens[j])
                        && j + 1 < tokens.Length
                        && tokens[j + 1] == "0,000")
                        break;

                    part2Tokens.Add(tokens[j]);
                }

                part2 = string.Join(" ", part2Tokens).Trim();
                break;
            }
        }

        // 3. Remove a unidade do final de part1 para obter o nome limpo
        var units = new HashSet<string>
    {
        "UNIDADE", "AMP", "ENVELOPE", "FR", "COMP", "COMPRIMI", "BOLSA", "FRASCO"
    };

        // Remove tokens do final de part1 que sejam unidade ou detalhe de apresentação
        var dosePattern = new Regex(@"^\d+(\.\d+)?(MG|ML|G|UI|MCG|MG/ML).*$",
                                    RegexOptions.IgnoreCase);

        while (part1Tokens.Count > 0
               && (units.Contains(part1Tokens.Last())
                   || dosePattern.IsMatch(part1Tokens.Last())))
        {
            part1Tokens.RemoveAt(part1Tokens.Count - 1);
        }

        var part1 = string.Join(" ", part1Tokens).Trim();

        // 4. Concatena as duas partes
        return string.IsNullOrEmpty(part2)
            ? part1
            : $"{part1} {part2}".Trim();
    }

    private string ExtractUnit(string line)
    {
        var units = new[]
        {
        "UNIDADE","AMP","ENVELOPE","FR","COMP","COMPRIMI","BOLSA"
    };

        foreach (var unit in units)
        {
            if (line.Contains($" {unit} "))
                return unit;
        }

        return "";
    }

    private decimal ExtractCurrentStock(string line)
    {
        var match = Regex.Match(line, @"\b\d{1,3}(\.\d{3})*,\d{3}\b");

        if (!match.Success)
            return 0;

        return ParseDecimal(match.Value);
    }

    private string ExtractBatch(string line)
    {
        var tokens = line.Split(' ');

        for (int i = 0; i < tokens.Length - 1; i++)
        {
            if (Regex.IsMatch(tokens[i + 1], @"\d{2}/\d{2}/\d{4}"))
                return tokens[i];
        }

        return "";
    }

    private DateTime? ExtractValidity(string line)
    {
        var match = Regex.Match(line, @"\d{2}/\d{2}/\d{4}");

        if (!match.Success)
            return null;

        if (DateTime.TryParse(match.Value, out var date))
            return date;

        return null;
    }

    private decimal ExtractQuantity(string line)
    {
        var match = Regex.Match(line, @"0,000\s+(\d{1,3}(\.\d{3})*,\d{3})");

        if (!match.Success)
            return 0;

        return ParseDecimal(match.Groups[1].Value);
    }

    private string ExtractManufacturer(string line)
    {
        var tokens = line.Split(' ');

        foreach (var token in tokens)
        {
            if (token.Contains("/"))
                return token;
        }

        return "";
    }

    private decimal ParseDecimal(string value)
    {
        value = value.Replace(".", "").Replace(",", ".");

        if (decimal.TryParse(value,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result))
            return result;

        return 0;
    }

    private string RemovePageHeader(string text)
    {
        // Remove tudo a partir de qualquer marcador conhecido do cabeçalho
        var headerMarkers = new[]
        {
        @"HEL\s*-\s*HOSPITAL ESTADUAL",
        @"SOULMV\s*-\s*Sistema de Gerenciamento",
        @"Relat[oó]rio de Confer[eê]ncia",
        @"P[aá]gina:\s*\d+\s*/\s*\d+",
        @"Emitido por:\s*\w+"
    };

        foreach (var marker in headerMarkers)
        {
            var match = Regex.Match(text, marker, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                // Descarta tudo a partir do marcador
                text = text[..match.Index].Trim();
                break;
            }
        }

        return text;
    }

}
