using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IStockService
{
    Task<List<Stock>> GetStocksAsync();
}


