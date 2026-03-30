using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class ProductStockService : IProductStockService
{
    public Task<List<ProductStock>> GetProductStocksAsync()
    {

        var stocks = new List<ProductStock>();

        return Task.FromResult(stocks);
    }
}
