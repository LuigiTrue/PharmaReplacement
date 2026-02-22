using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IOrderService
{
    Task<List<Order>> GetOrdersAsync();
}


