using RepyPharma.Models;

namespace RepyPharma.Services;

public interface IOrderService
{
    Task<List<Order>> GetOrdersAsync();
}


