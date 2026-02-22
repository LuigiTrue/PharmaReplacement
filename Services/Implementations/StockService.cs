using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class StockService : IStockService
{
    public Task<List<Stock>> GetStocksAsync()
    {
        var random = new Random();

        var stockList = new[]
        {
            "Farmácia Central",
            "Farmácia Centro Cirúrgico",
            "Fracionamento",
            "Almoxarifado"
        };

        var stocks = new List<Stock>();

        for (int i = 0; i < stockList.Length; i++)
        {
            stocks.Add(new Stock
            {
                Id = i + 1,
                StockName = stockList[i],
                DangerLevel = random.Next(5, 40),
                StockCode = random.Next(1000, 10000),
                TestText = "Isso é um teste"
            });
        }

        return Task.FromResult(stocks);
    }
}
