using RepyPharma.Domain.Entities;

namespace RepyPharma.Services.Interfaces;

public interface IDanfeService
{
    Task<Danfe?> GetDanfeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetAllDanfesAsync(CancellationToken cancellationToken = default);
    Task<Danfe> CreateDanfeAsync(Danfe danfe, CancellationToken cancellationToken = default);
    Task<Danfe?> UpdateDanfeAsync(int id, Danfe danfe, CancellationToken cancellationToken = default);
    Task<bool> DeleteDanfeAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetDanfesByQuotationAsync(int quotationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetDanfesBySupplierAsync(int supplierId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetDanfesByQuotationNumberAsync(string quotationNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetDanfesByPaymentDueDateAsync(DateTime dueDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetDanfesByPaymentDueDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetDanfesDueSoonAsync(int daysAhead, CancellationToken cancellationToken = default);

    /// <summary>Retorna vencimentos ultrapassados; não indica se as notas foram pagas.</summary>
    Task<IReadOnlyList<Danfe>> GetOverdueDanfesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Danfe>> GetDanfesWithoutPaymentDueDateAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetDanfesByReceivedDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetDanfesNotReceivedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetThermolabileDanfesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Danfe>> GetDanfesWithBoletoAsync(CancellationToken cancellationToken = default);
    Task<Danfe?> GetDanfeByNumberAsync(string danfeNumber, int supplierId, CancellationToken cancellationToken = default);
}
