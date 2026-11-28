using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;

namespace RepyPharma.Services.Import;

internal static class CsvReportFormat
{
    internal static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");
    internal static CsvReader CreateReader(Stream stream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return CsvReader.Create(stream, new CsvReaderOptions
        {
            Separator = ',', HasHeader = false, FormatProvider = Culture,
            Encoding = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),
            FieldCountMismatch = CsvFieldCountHandling.Allow, MalformedData = CsvMalformedDataHandling.Throw,
            TrackLineNumbers = true
        }, leaveOpen: true);
    }
    internal static string Label(string value) => string.Concat(value.Trim().Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).ToUpperInvariant();
    internal static string Header(string value) => Regex.Replace(Label(value), @"[^A-Z0-9]", "");
    internal static bool TryDecimal(string text, out decimal value)
    {
        value = 0;
        return Regex.IsMatch(text.Trim(), @"^[+-]?(?:[0-9]+|[0-9]{1,3}(?:\.[0-9]{3})+)(?:,[0-9]+)?$", RegexOptions.CultureInvariant)
            && decimal.TryParse(text, NumberStyles.Number, Culture, out value);
    }
    internal static string Field(string[] fields, int index) => index >= 0 && index < fields.Length ? fields[index].Trim() : "";
}
