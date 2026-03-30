using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IProductStockService
{
    Task<List<ProductStock>> GetProductStocksAsync();
}


