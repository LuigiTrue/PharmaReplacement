namespace RepyPharma.Models;

public class ReplenishmentReport
{
    public List<ReplenishmentItem> FromFractionation { get; set; } = new(); // Disponível no fracionamento
    public List<ReplenishmentItem> FromCafOnly { get; set; } = new(); // Somente na CAF
    public List<ReplenishmentItem> FromStockOnly { get; set; } = new(); // Somente no almoxarifado
    public List<ReplenishmentItem> NoSourceAvailable { get; set; } = new(); // Sem lote compatível
}