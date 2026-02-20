using RepyPharma.Models;

namespace RepyPharma.Services;

public class OrderService : IOrderService
{
    public Task<List<Order>> GetOrdersAsync()
    {
        var random = new Random();

        var remedy = new[]
        {
            "Dipirona",
            "Ondasetrona",
            "Hiocina + Dipirona",
            "Tramadol"
        };

        var orders = new List<Order>();

        for (int i = 0; i < remedy.Length; i++)
        {
            orders.Add(new Order
            {
                Id = i + 1,
                RemedyName = remedy[i],
                PercentageStock = random.Next(5, 40),
                GrossValue = random.Next(1000, 10000)
            });
        }

        return Task.FromResult(orders);
    }
}
