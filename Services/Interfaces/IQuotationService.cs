using RepyPharma.Domain.Entities;

namespace RepyPharma.Services.Interfaces;

public interface IQuotationService
{
    Task<Quotation?> GetQuotationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Quotation>> GetAllQuotationsAsync(CancellationToken cancellationToken = default);
    Task<Quotation?> GetQuotationByNumberAsync(string quotationNumber, CancellationToken cancellationToken = default);
    Task<Quotation> CreateQuotationAsync(Quotation quotation, CancellationToken cancellationToken = default);
    Task<Quotation?> UpdateQuotationAsync(int id, Quotation quotation, CancellationToken cancellationToken = default);
    Task<bool> DeleteQuotationAsync(int id, CancellationToken cancellationToken = default);
}
