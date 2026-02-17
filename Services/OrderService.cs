using RepyPharma.Models;

namespace RepyPharma.Services;

public class OrderService : IOrderService
{
    public Task<List<Order>> GetOrdersAsync()
    {
        var random = new Random();

        var countries = new[]
        {
            "Brasil",
            "Argentina",
            "Chile",
            "Colômbia",
            "Peru"
        };

        var orders = new List<Order>();

        for (int i = 1; i <= 50; i++)
        {
            orders.Add(new Order
            {
                Id = i,
                Country = countries[random.Next(countries.Length)],
                DiscountPercentage = random.Next(5, 40),
                GrossValue = random.Next(1000, 10000)
            });
        }

        return Task.FromResult(orders);
    }
}
