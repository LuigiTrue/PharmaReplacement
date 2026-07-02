using System.Globalization;
using System.Text;
using RepyPharma.Models;

namespace RepyPharma.Services.Implementatios;

internal static class ReplenishmentPriorityPolicy
{
    private const int CriticalMedicineRank = 0;
    private const int MedicineRank = 1;
    private const int MaterialRank = 2;

    private static readonly string[] CriticalMedicineTerms =
    {
        "DIPIRONA",
        "PANTOPRAZOL",
        "MAV",
        "CEF",
        "CILINA",
        "CICLINA",
        "MICINA",
        "FLOXACINO",
        "MEROPENEM",
        "IMIPENEM",
        "ERTAPENEM",
        "PIPERACILINA",
        "TAZOBACTAM",
        "VANCOMICINA",
        "METRONIDAZOL",
        "CLINDAMICINA",
        "AZITROMICINA",
        "CLARITROMICINA",
        "GENTAMICINA",
        "AMICACINA",
        "AMPICILINA",
        "AMOXICILINA",
        "OXACILINA",
        "CIPRO",
        "LEVOFLOX",
        "MOXIFLOX",
        "LINEZOLIDA",
        "POLIMIXINA",
        "COLISTINA",
        "INSULINA",
        "HEPARINA",
        "ADRENALINA",
        "EPINEFRINA",
        "NORADRENALINA",
        "NOREPINEFRINA",
        "DOPAMINA",
        "DOBUTAMINA",
        "NITROPRUSSIATO",
        "NITROGLICERINA",
        "MORFINA",
        "FENTANIL",
        "MIDAZOLAM",
        "PROPOFOL",
        "KETAMINA",
        "CETAMINA",
        "POTASSIO",
        "KCL",
        "METOTREXATO",
        "WARFARINA"
    };

    private static readonly string[] MaterialTerms =
    {
        "AGULHA",
        "SERINGA",
        "COLETOR",
        "CATETER",
        "SONDA",
        "EQUIPO",
        "EXTENSOR",
        "LUVA",
        "GAZE",
        "COMPRESSA",
        "ATADURA",
        "ESPARADRAPO",
        "CURATIVO",
        "MASCARA",
        "AVENTAL",
        "LAMINA",
        "BOLSA",
        "TUBO",
        "DISPOSITIVO",
        "SCALP",
        "ABAIXADOR",
        "ELETRODO",
        "CAMPO",
        "DRENO",
        "TORNEIRA",
        "CONECTOR",
        "FIXADOR",
        "ALGODAO",
        "FIO",
        "PAPEL",
        "COPO",
        "SACO"
    };

    private static readonly string[] MedicineTerms =
    {
        " SOL INJ",
        " PO P/ SOL",
        " AMP",
        " AMPOLA",
        " COMP",
        " CAPS",
        " DRAGEA",
        " XAROPE",
        " SUSP",
        " GOTAS",
        " COLIRIO",
        " CREME",
        " POMADA",
        " MG",
        " MCG",
        " UI"
    };

    public static int GetSupplyRank(string itemName)
    {
        var normalizedName = Normalize(itemName);

        if (ContainsAny(normalizedName, CriticalMedicineTerms))
            return CriticalMedicineRank;

        if (ContainsAny(normalizedName, MaterialTerms))
            return MaterialRank;

        if (ContainsAny(normalizedName, MedicineTerms))
            return MedicineRank;

        return MaterialRank;
    }

    public static int GetSupplyRank(string itemName, ItemPriority itemPriority)
    {
        var normalizedName = Normalize(itemName);

        if (ContainsAny(normalizedName, MaterialTerms))
            return MaterialRank;

        if (itemPriority == ItemPriority.UltraHigh || ContainsAny(normalizedName, CriticalMedicineTerms))
            return CriticalMedicineRank;

        if (itemPriority == ItemPriority.High)
            return MedicineRank;

        if (ContainsAny(normalizedName, MedicineTerms))
            return MedicineRank;

        return MaterialRank;
    }

    public static string GetSupplyGroupLabel(string itemName)
    {
        return GetSupplyRank(itemName) switch
        {
            CriticalMedicineRank => "Medicamentos prioritários",
            MedicineRank => "Demais medicamentos",
            _ => "Materiais"
        };
    }

    public static string GetSupplyGroupLabel(string itemName, ItemPriority itemPriority)
    {
        return GetSupplyRank(itemName, itemPriority) switch
        {
            CriticalMedicineRank => "Medicamentos prioritários",
            MedicineRank => "Demais medicamentos",
            _ => "Materiais"
        };
    }

    public static ItemPriority GetEffectiveItemPriority(MinimumStock minimum, string itemName)
    {
        return GetSupplyRank(itemName, minimum.itemPriority) switch
        {
            CriticalMedicineRank => ItemPriority.UltraHigh,
            MedicineRank when minimum.itemPriority > ItemPriority.High => ItemPriority.High,
            _ => minimum.itemPriority
        };
    }

    private static bool ContainsAny(string value, IEnumerable<string> terms)
    {
        return terms.Any(value.Contains);
    }

    private static string Normalize(string value)
    {
        var normalized = value
            .ToUpperInvariant()
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
