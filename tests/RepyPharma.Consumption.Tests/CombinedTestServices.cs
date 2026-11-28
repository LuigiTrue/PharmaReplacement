using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RepyPharma.Services.Import;
namespace RepyPharma.Consumption.Tests;
internal static class CombinedTestServices
{
    internal static IOptions<ConsumptionReportsImportOptions> Options() => Microsoft.Extensions.Options.Options.Create(new ConsumptionReportsImportOptions());
    internal static SectorConsumptionImportService Sector() => new(NullLogger<SectorConsumptionImportService>.Instance);
}
