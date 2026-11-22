using RepyPharma.Domain.Entities;

namespace RepyPharma.Services.Interfaces;

public interface ISupplierService
{
    Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Supplier>> GetAllSuppliersAsync(CancellationToken cancellationToken = default);
    Task<Supplier?> GetSupplierByCnpjAsync(string cnpj, CancellationToken cancellationToken = default);
    Task<Supplier> CreateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task<Supplier?> UpdateSupplierAsync(int id, Supplier supplier, CancellationToken cancellationToken = default);
    Task<bool> DeleteSupplierAsync(int id, CancellationToken cancellationToken = default);
}
