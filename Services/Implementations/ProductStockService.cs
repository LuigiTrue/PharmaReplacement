using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class ProductStockService : IProductStockService
{
    public Task<List<ProductStock>> GetProductStocksAsync()
    {
        var random = new Random();

        var stockList = new[]
        {
            "Farmácia Central",
            "Farmácia Centro Cirúrgico",
            "Fracionamento",
            "Almoxarifado"
        };

        var stocks = new List<ProductStock>();



        return Task.FromResult(stocks);
    }
}
