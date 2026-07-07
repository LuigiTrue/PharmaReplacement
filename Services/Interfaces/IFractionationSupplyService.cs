using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IFractionationSupplyService
{
    Task<FractionationSupplyData> GetSupplyDataAsync(int coverageDays);
}
