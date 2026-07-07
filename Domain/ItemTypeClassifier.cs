using System.Globalization;
using System.Text;
using RepyPharma.Domain.Entities;

namespace RepyPharma.Domain;

public static class ItemTypeClassifier
{
    private static readonly string[] MaterialTerms =
    {
        "ABAIXADOR", "AGULHA", "ALGODAO", "APARELHO P/ TRICOTOMIA", "ATADURA", "AVENTAL",
        "BOLSA", "CAMPO", "CANULA", "CATETER", "COLETOR", "COMPRESSA", "CONECTOR",
        "CURATIVO", "DISPOSITIVO", "DRENO", "ELETRODO", "EQUIPO", "ESPARADRAPO",
        "EXTENSOR", "FIO ", "FIXADOR", "FRALDA", "GAZE", "LANCETA", "LAMINA",
        "LUVA", "MANTA", "MASCARA", "PAPEL", "SACO", "SCALP", "SERINGA", "SONDA",
        "TESTE RAPIDO", "TIRA TESTE", "TORNEIRA", "TRANSDUTOR", "TUBO"
    };

    private static readonly string[] HighAlertMedicationTerms =
    {
        "ADRENALINA", "AMIODARONA", "DOBUTAMINA", "DOPAMINA", "ENOXAPARINA",
        "EPINEFRINA", "FENTANIL", "FENTANILA", "GLUCONATO DE CALCIO", "HEPARINA",
        "INSULINA", "KCL", "MORFINA", "NITROGLICERINA", "NITROPRUSSIATO",
        "NORADRENALINA", "NOREPINEFRINA", "POTASSIO", "SULFATO DE MAGNESIO"
    };

    private static readonly string[] PsychotropicTerms =
    {
        "AMITRIPTILINA", "BIPERIDENO", "CARBAMAZEPINA", "CLONAZEPAM", "CLORPROMAZINA",
        "DIAZEPAM", "FENITOINA", "FENOBARBITAL", "FLUOXETINA", "HALOPERIDOL",
        "LEVOMEPROMAZINA", "LITIO", "OLANZAPINA", "QUETIAPINA", "RISPERIDONA",
        "SERTRALINA", "VALPROATO"
    };

    private static readonly string[] SedativeTerms =
    {
        "CETAMINA", "DEXMEDETOMIDINA", "ESCETAMINA", "ETOMIDATO", "KETAMINA",
        "MIDAZOLAM", "PROPOFOL", "SEVOFLURANO"
    };

    private static readonly string[] AntibioticTerms =
    {
        "AMICACINA", "AMOXICILINA", "AMPICILINA", "AZITROMICINA", "BENZILPENICILINA",
        "CEFA", "CEFEP", "CEFT", "CEFTR", "CIPROFLOXACINO", "CLARITROMICINA",
        "CLINDAMICINA", "COLISTINA", "ERTAPENEM", "GENTAMICINA", "IMIPENEM",
        "LEVOFLOXACINO", "LINEZOLIDA", "MEROPENEM", "METRONIDAZOL", "MOXIFLOXACINO",
        "OXACILINA", "PIPERACILINA", "POLIMIXINA", "TAZOBACTAM", "TIGECICLINA",
        "VANCOMICINA"
    };

    public static ItemType Classify(string itemName)
    {
        var normalizedName = Normalize(itemName);

        if (ContainsAny(normalizedName, HighAlertMedicationTerms))
            return ItemType.HighAlertMedication;

        if (ContainsAny(normalizedName, PsychotropicTerms))
            return ItemType.Psychotropic;

        if (ContainsAny(normalizedName, SedativeTerms))
            return ItemType.Sedative;

        if (ContainsAny(normalizedName, AntibioticTerms))
            return ItemType.Antibiotic;

        if (ContainsAny(normalizedName, MaterialTerms))
            return ItemType.Material;

        return ItemType.CommonMedication;
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
