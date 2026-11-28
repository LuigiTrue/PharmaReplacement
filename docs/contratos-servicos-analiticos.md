# Contratos dos serviços analíticos

Os serviços analíticos foram criados inicialmente como contratos para desenvolvimento incremental. Desde 24/11/2026, `ConsumptionImportService` implementa leitura e validação de CSV, consolidação mensal, persistência transacional e consulta do histórico, ligado aos cards de consumo. Os demais serviços analíticos continuam sem implementação de negócio. Os detalhes do fluxo estão em [Consumo e ABC](consumo-e-abc.md).

## Organização e injeção de dependência

Os sete serviços de análise seguem `Services/Interfaces` e `Services/Implementations`. Foi mantido o namespace legado `RepyPharma.Services.Implementatios` das implementações existentes. Importação e consolidação seguem `Services/Import` e `Services/Import/Interfaces`, como o importador JSON atual. Todos os nove registros novos são `Scoped` em `Program.cs`.

| Interface | Implementação | Métodos |
| --- | --- | --- |
| [IConsumptionAnalysisService](../Services/Interfaces/IConsumptionAnalysisService.cs) | [ConsumptionAnalysisService](../Services/Implementations/ConsumptionAnalysisService.cs) | 9 |
| [IAbcClassificationService](../Services/Interfaces/IAbcClassificationService.cs) | [AbcClassificationService](../Services/Implementations/AbcClassificationService.cs) | 7 |
| [IXyzCriticalityService](../Services/Interfaces/IXyzCriticalityService.cs) | [XyzCriticalityService](../Services/Implementations/XyzCriticalityService.cs) | 6 |
| [IConsumptionForecastService](../Services/Interfaces/IConsumptionForecastService.cs) | [ConsumptionForecastService](../Services/Implementations/ConsumptionForecastService.cs) | 7 |
| [IInventoryAnalysisService](../Services/Interfaces/IInventoryAnalysisService.cs) | [InventoryAnalysisService](../Services/Implementations/InventoryAnalysisService.cs) | 7 |
| [IPurchasePlanningService](../Services/Interfaces/IPurchasePlanningService.cs) | [PurchasePlanningService](../Services/Implementations/PurchasePlanningService.cs) | 7 |
| [IConsumptionDataQualityService](../Services/Interfaces/IConsumptionDataQualityService.cs) | [ConsumptionDataQualityService](../Services/Implementations/ConsumptionDataQualityService.cs) | 5 |
| [IConsumptionImportService](../Services/Import/Interfaces/IConsumptionImportService.cs) | [ConsumptionImportService](../Services/Import/ConsumptionImportService.cs) | 7 |
| [ISectorConsumptionImportService](../Services/Import/Interfaces/ISectorConsumptionImportService.cs) | [SectorConsumptionImportService](../Services/Import/SectorConsumptionImportService.cs) | 1 |
| [IConsumptionAggregationService](../Services/Import/Interfaces/IConsumptionAggregationService.cs) | [ConsumptionAggregationService](../Services/Import/ConsumptionAggregationService.cs) | 3 |

Os contratos públicos usam `Task<T>` e recebem `CancellationToken` como último parâmetro. As consultas coletivas usam filtros e paginação quando retornam listas de tamanho variável. Distribuições por classes/grupos têm conjuntos pequenos e definidos e não precisam de paginação.

`ConsumptionImportService` recebe `IDbContextFactory<AppDbContext>`, `ILogger<ConsumptionImportService>`, `TimeProvider`, `ISectorConsumptionImportService` e as opções da importação. Os serviços ainda não implementados permanecem sem dependências de banco desnecessárias e não são chamados pela importação CSV.

## DTOs e enums

Os DTOs estão no namespace `RepyPharma.Models.Analytics`. São reutilizados os contratos analíticos e os DTOs da importação conjunta, sem exposição de entidades EF. Os enums de domínio existentes `ItemType` e `AbcClassification` são reutilizados; nenhuma entidade EF é exposta.

| Arquivo | Contratos |
| --- | --- |
| [AnalysisSharedDtos.cs](../Models/Analytics/AnalysisSharedDtos.cs) | `ConsumptionGranularity`, `AnalysisSeverity`, `ConsumptionOperationStatus`, `DateRangeDto`, `PageRequestDto`, `PagedResultDto<T>`, `DataAvailabilityDto<T>`, `ItemFilterDto`, `ItemReferenceDto`, `ConsumptionQueryDto`, `ItemPriorityDto` |
| [ConsumptionAnalysisDtos.cs](../Models/Analytics/ConsumptionAnalysisDtos.cs) | `DailyConsumptionDto`, `MonthlyConsumptionDto`, `ConsumptionSummaryDto`, `AverageConsumptionDto`, `ConsumptionVariationDto`, `ConsumptionSeriesPointDto`, `MovingAverageDto`, `YearOverYearComparisonDto`, `ConsumptionRankingDto`, `ConsumptionGroupDto` |
| [AbcClassificationDtos.cs](../Models/Analytics/AbcClassificationDtos.cs) | `AbcAnalysisRequestDto`, `AbcClassificationScopeDto`, `AbcAnalysisDto`, `AbcClassificationDto`, `AbcDistributionEntryDto`, `AbcDistributionDto`, `ParetoAnalysisDto`, `AbcClassificationChangeDto` |
| [XyzCriticalityDtos.cs](../Models/Analytics/XyzCriticalityDtos.cs) | `XyzCriticality`, `ItemCriticalityDto`, `UpdateItemCriticalityDto`, `CriticalityHistoryEntryDto`, `CriticalityDistributionDto` |
| [ConsumptionForecastDtos.cs](../Models/Analytics/ConsumptionForecastDtos.cs) | `ConsumptionTrendDirection`, `ForecastMethodOptionsDto`, `ForecastRequestDto`, `ForecastPointDto`, `ConsumptionForecastDto`, `ForecastComparisonDto`, `ConsumptionTrendDto`, `SeasonalityPointDto`, `SeasonalityAnalysisDto`, `ForecastAccuracyMetricDto`, `ForecastAccuracyDto`, `DemandVariabilityDto`, `IntermittentDemandDto` |
| [InventoryAnalysisDtos.cs](../Models/Analytics/InventoryAnalysisDtos.cs) | `InventoryAnalysisQueryDto`, `ItemsOverviewDto`, `ItemAnalysisDto`, `AbcXyzMatrixCellDto`, `AbcXyzMatrixDto`, `ConsumptionAlertDto`, `FinancialConsumptionSummaryDto` |
| [PurchasePlanningDtos.cs](../Models/Analytics/PurchasePlanningDtos.cs) | `PurchasePlanningParametersDto`, `PurchaseRequirementDto`, `StockCoverageDto`, `ReorderPointDto`, `SafetyStockDto`, `PurchasePlanningDto`, `PurchasePlanningSummaryDto` |
| [ConsumptionDataQualityDtos.cs](../Models/Analytics/ConsumptionDataQualityDtos.cs) | `ConsumptionDataCoverageDto`, `MissingConsumptionPeriodDto`, `InsufficientHistoryItemDto`, `DataQualitySummaryDto`, `LastConsumptionImportDto` |
| [CombinedConsumptionImportDtos.cs](../Models/Analytics/CombinedConsumptionImportDtos.cs) | Progresso, linhas de preço, preço por produto, prévia, resultados e erros da importação conjunta. |
| [ConsumptionImportDtos.cs](../Models/Analytics/ConsumptionImportDtos.cs) | `ConsumptionImportOptionsDto`, `ConsumptionImportIssueDto`, `ConsumptionImportValidationDto`, `ConsumptionImportResultDto` |
| [ConsumptionAggregationDtos.cs](../Models/Analytics/ConsumptionAggregationDtos.cs) | `MonthlyAggregationRequestDto`, `ConsumptionAggregationStatusDto` |

Contratos compartilhados evitam tipos de paginação, intervalo, seleção e identificação de item repetidos. Rankings ABC e pontos do Pareto reutilizam `AbcClassificationDto`; desvios atípicos reutilizam `ConsumptionAlertDto`; prioridades de estoque e compra reutilizam `ItemPriorityDto`.

## Decisões e compatibilidade com o modelo atual

- **Preço mensal:** a propriedade existente é `MonthlyConsumption.UnitPurchasePrice`, e não `UnitProductPrice`. A entidade foi preservada e `MonthlyConsumptionDto` usa o nome real, incluindo sua nulabilidade. Preço desconhecido pode continuar distinto de zero. A importação conjunta ignora preços zero e usa o preço positivo único ou, somente para preços positivos distintos, o quociente dos totais e quantidades setoriais. Ausência de custo válido ou cálculo inviável gera exceção de processamento e bloqueia a confirmação.
- **Grupo de produtos:** não existe entidade de grupo. Os filtros e os agrupamentos usam o `ItemType` existente, sem adicionar tabelas ou identificadores de grupos fictícios.
- **Contexto ABC:** consultas e comparações preservam `AnalysisId`. O escopo de classificação atual admite uma execução identificada ou um recorte explícito de período/conjunto. A futura implementação deverá validar esse escopo e evitar misturar classificações de conjuntos diferentes. Paginar um Pareto não deve redefinir o conjunto financeiro da análise.
- **XYZ manual:** `XyzCriticality` é apenas um enum de contrato em `Models/Analytics`. O modelo atual não possui propriedade/tabela para persistir criticidade. A atualização manual e a verificação de autorização continuam pendentes; não foi criada qualquer classificação automática por consumo.
- **Auditoria e metadados:** `ConsumptionImportRecord`, na tabela `consumption_imports`, registra as importações de consumo concluídas. Histórico XYZ e histórico de previsões continuam indisponíveis.
- **Importação existente:** `IJsonImportService` e `PdfParserService` tratam estoque, não CSV de consumo. Portanto, os novos contratos de consumo são separados e não duplicam esses fluxos. `ConsumptionImportResultDto` não substitui o `ImportResult` existente de estoque.
- **Streams CSV:** `ValidateImportAsync` e `ImportDailyConsumptionAsync` recebem `Stream`, nome do arquivo e `ReferenceMonth`, definido pelo card. O serviço lê o fluxo com CsvHelper; o chamador continua responsável por posicioná-lo e fechá-lo. A validação isolada não grava dados.
- **Identificadores de operações:** GUIDs de importações concluídas são persistidos em `consumption_imports`; os contratos de jobs de consolidação continuam reservados para evolução posterior.
- **Métodos de previsão:** `MethodCode` e parâmetros numéricos são extensíveis. Nenhum método ou algoritmo é declarado como implementado ou selecionado automaticamente.
- **Unidades:** intervalos usam `DateOnly`; datas operacionais usam `DateTimeOffset`; horizontes e prazos são expressos em dias; médias móveis usam uma janela de observações na granularidade indicada; quantidades, valores e percentuais usam `decimal`.

## Assinaturas disponíveis

### ConsumptionAnalysisService

```csharp
Task<PagedResultDto<DailyConsumptionDto>> GetDailyHistoryAsync(int itemId, DateRangeDto period, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<PagedResultDto<MonthlyConsumptionDto>> GetMonthlyHistoryAsync(int itemId, DateRangeDto period, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<AverageConsumptionDto> GetAverageConsumptionAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);
Task<ConsumptionVariationDto> GetConsumptionVariationAsync(int itemId, DateRangeDto referencePeriod, DateRangeDto comparisonPeriod, CancellationToken cancellationToken = default);
Task<ConsumptionSummaryDto> GetConsumptionSummaryAsync(int itemId, DateRangeDto period, CancellationToken cancellationToken = default);
Task<MovingAverageDto> GetMovingAverageAsync(int itemId, DateRangeDto period, int windowSize, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);
Task<YearOverYearComparisonDto> GetYearOverYearComparisonAsync(int itemId, DateRangeDto referencePeriod, DateRangeDto comparisonPeriod, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);
Task<PagedResultDto<ConsumptionRankingDto>> GetConsumptionRankingAsync(ConsumptionQueryDto query, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<IReadOnlyList<ConsumptionGroupDto>> GetConsumptionByGroupAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default);
```

### AbcClassificationService

```csharp
Task<AbcAnalysisDto> CalculateAbcClassificationAsync(AbcAnalysisRequestDto request, CancellationToken cancellationToken = default);
Task<AbcClassificationDto?> GetCurrentClassificationAsync(int itemId, AbcClassificationScopeDto scope, CancellationToken cancellationToken = default);
Task<PagedResultDto<AbcClassificationDto>> GetClassificationHistoryAsync(int itemId, AbcClassificationScopeDto scope, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<PagedResultDto<AbcClassificationDto>> GetClassificationRankingAsync(Guid analysisId, ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<AbcDistributionDto> GetAbcDistributionAsync(Guid analysisId, CancellationToken cancellationToken = default);
Task<ParetoAnalysisDto> GetParetoAnalysisAsync(Guid analysisId, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<PagedResultDto<AbcClassificationChangeDto>> GetAbcClassificationChangesAsync(Guid referenceAnalysisId, Guid comparisonAnalysisId, ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default);
```

### XyzCriticalityService

```csharp
Task<ItemCriticalityDto?> GetItemCriticalityAsync(int itemId, CancellationToken cancellationToken = default);
Task<PagedResultDto<ItemCriticalityDto>> GetItemsByCriticalityAsync(XyzCriticality criticality, ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<ItemCriticalityDto> UpdateItemCriticalityAsync(int itemId, UpdateItemCriticalityDto request, CancellationToken cancellationToken = default);
Task<PagedResultDto<ItemCriticalityDto>> GetUnclassifiedItemsAsync(ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<CriticalityDistributionDto> GetCriticalityDistributionAsync(ItemFilterDto filter, CancellationToken cancellationToken = default);
Task<DataAvailabilityDto<PagedResultDto<CriticalityHistoryEntryDto>>> GetCriticalityHistoryAsync(int itemId, PageRequestDto pagination, CancellationToken cancellationToken = default);
```

### ConsumptionForecastService

```csharp
Task<ConsumptionForecastDto> GetForecastAsync(int itemId, ForecastRequestDto request, ForecastMethodOptionsDto method, CancellationToken cancellationToken = default);
Task<ForecastComparisonDto> CompareForecastMethodsAsync(int itemId, ForecastRequestDto request, IReadOnlyList<ForecastMethodOptionsDto> methods, CancellationToken cancellationToken = default);
Task<ConsumptionTrendDto> GetConsumptionTrendAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);
Task<SeasonalityAnalysisDto> GetSeasonalityAnalysisAsync(int itemId, DateRangeDto period, CancellationToken cancellationToken = default);
Task<DataAvailabilityDto<ForecastAccuracyDto>> GetHistoricalForecastAccuracyAsync(int itemId, DateRangeDto evaluationPeriod, IReadOnlyList<string> methodCodes, CancellationToken cancellationToken = default);
Task<DemandVariabilityDto> GetDemandVariabilityAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);
Task<IntermittentDemandDto> GetIntermittentDemandAnalysisAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);
```

### InventoryAnalysisService

```csharp
Task<ItemsOverviewDto> GetItemsOverviewAsync(InventoryAnalysisQueryDto query, CancellationToken cancellationToken = default);
Task<ItemAnalysisDto?> GetItemAnalysisAsync(int itemId, InventoryAnalysisQueryDto query, CancellationToken cancellationToken = default);
Task<AbcXyzMatrixDto> GetAbcXyzMatrixAsync(Guid abcAnalysisId, ItemFilterDto filter, CancellationToken cancellationToken = default);
Task<PagedResultDto<ConsumptionAlertDto>> GetConsumptionAlertsAsync(InventoryAnalysisQueryDto query, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<PagedResultDto<ConsumptionAlertDto>> GetAbnormalConsumptionAsync(InventoryAnalysisQueryDto query, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<PagedResultDto<ItemPriorityDto>> GetHighPriorityItemsAsync(InventoryAnalysisQueryDto query, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<FinancialConsumptionSummaryDto> GetFinancialConsumptionSummaryAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default);
```

### PurchasePlanningService

```csharp
Task<PagedResultDto<PurchasePlanningDto>> GetPurchaseProjectionAsync(ItemFilterDto filter, PurchasePlanningParametersDto parameters, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<PurchaseRequirementDto> CalculatePurchaseRequirementAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);
Task<ItemPriorityDto> GetPurchasePriorityAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);
Task<StockCoverageDto> GetStockCoverageAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);
Task<ReorderPointDto> GetReorderPointAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);
Task<SafetyStockDto> GetSafetyStockAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);
Task<PurchasePlanningSummaryDto> GetPurchasePlanningSummaryAsync(ItemFilterDto filter, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);
```

### ConsumptionDataQualityService

```csharp
Task<ConsumptionDataCoverageDto> GetConsumptionDataCoverageAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default);
Task<PagedResultDto<MissingConsumptionPeriodDto>> GetMissingConsumptionPeriodsAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<PagedResultDto<InsufficientHistoryItemDto>> GetItemsWithInsufficientHistoryAsync(ConsumptionQueryDto query, int minimumHistoryDays, PageRequestDto pagination, CancellationToken cancellationToken = default);
Task<DataQualitySummaryDto> GetConsumptionDataQualitySummaryAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default);
Task<DataAvailabilityDto<LastConsumptionImportDto>> GetLastImportInformationAsync(ItemFilterDto filter, CancellationToken cancellationToken = default);
```

### ConsumptionImportService

```csharp
Task<ConsumptionImportValidationDto> ValidateImportAsync(Stream csvStream, string fileName, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default);
Task<ConsumptionImportResultDto> ImportDailyConsumptionAsync(Stream csvStream, string fileName, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default);
Task<ConsumptionImportResultDto?> GetImportResultAsync(Guid importId, CancellationToken cancellationToken = default);
Task<IReadOnlyList<ConsumptionImportResultDto>> GetImportsAsync(CancellationToken cancellationToken = default);
```

### ConsumptionAggregationService

```csharp
Task<ConsumptionAggregationStatusDto> ConsolidateMonthlyConsumptionAsync(MonthlyAggregationRequestDto request, CancellationToken cancellationToken = default);
Task<ConsumptionAggregationStatusDto> RecalculateMonthlyConsumptionAsync(MonthlyAggregationRequestDto request, CancellationToken cancellationToken = default);
Task<ConsumptionAggregationStatusDto?> GetAggregationStatusAsync(Guid aggregationId, CancellationToken cancellationToken = default);
```

## Validação da etapa inicial de contratos

- Compilação com `dotnet build --no-restore --nologo`: **zero erros e zero avisos**.
- Conferência estática das 54 assinaturas nas interfaces e implementações, do `CancellationToken`, dos comentários XML e do corpo contendo exclusivamente `throw new NotImplementedException();`.
- Conferência dos nove registros de DI e da ausência de entidades EF nas propriedades dos DTOs.
- Comparação de hashes dos arquivos preexistentes de entidades, contexto, migrations, componentes, projeto/pacotes, serviços e modelos, preservando inclusive alterações locais anteriores à tarefa.
- Nenhum método ainda não implementado foi chamado. Não houve acesso ao banco nem geração ou aplicação de migration nesta etapa.
