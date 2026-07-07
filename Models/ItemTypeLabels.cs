using RepyPharma.Domain.Entities;

namespace RepyPharma.Models;

public static class ItemTypeLabels
{
    public static string GetLabel(ItemType itemType) => itemType switch
    {
        ItemType.CommonMedication => "Medicamento comum",
        ItemType.Antibiotic => "Antibiótico",
        ItemType.HighAlertMedication => "MAV",
        ItemType.Psychotropic => "Psicotrópico",
        ItemType.Sedative => "Sedativo",
        ItemType.Material => "Material",
        _ => itemType.ToString()
    };
}
