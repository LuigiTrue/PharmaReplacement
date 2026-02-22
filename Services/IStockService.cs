using RepyPharma.Models;

namespace RepyPharma.Services;

public interface IStockService
{
    Task<List<Stock>> GetStocksAsync();
}


