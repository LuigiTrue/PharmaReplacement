# Serviços atuais

O projeto mantém a base de registro de estoque, DANFEs e autenticação. As implementações antigas de consumo, médias, estoque mínimo, reposição, fracionamento e pedidos demonstrativos foram removidas para permitir a nova lógica de consumo e compras.

| Serviço/estado | Registro | Responsabilidade |
| --- | --- | --- |
| `AuthService` | `IAuthService`, scoped | Sessão, permissões e identificação do usuário. |
| `UserSettingsService` | `IUserSettingsService`, scoped | Perfil e solicitações de alteração de senha. |
| `DanfeService` | `IDanfeService`, scoped | Cadastro e consulta de notas fiscais. |
| `SupplierService` | `ISupplierService`, scoped | Fornecedores associados às DANFEs. |
| `QuotationService` | `IQuotationService`, scoped | Identificação das cotações exigidas pelas DANFEs. |
| `StockJsonService` | `IStockJsonService`, scoped | Consulta itens ativos, lotes e saldos por local no banco; o nome legado não implica leitura de JSON. |
| `JsonImportService` | `IJsonImportService`, scoped | Importa exclusivamente itens, lotes, locais e saldos de estoque em JSON. |
| `PdfParserService` | scoped | Valida e extrai itens, lotes, locais e quantidades do espelho de estoque. |
| `PdfStorageService` | scoped | Persiste o espelho, atualiza saldos, inativa itens ausentes e remove saldos/lotes obsoletos. |
| `StockDataState` | singleton | Notifica a consulta de estoque após importações bem-sucedidas. |
| `ThemeState` | scoped | Mantém a seleção de tema da interface. |
| `ConsumptionImportService` | `IConsumptionImportService`, scoped | Reconstrói o diário, associa os preços informados por código, mantém a prévia e confirma o fechamento em uma transação. |
| `SectorConsumptionImportService` | `ISectorConsumptionImportService`, scoped | Resolve o custo histórico por código: preço positivo único ou ponderação setorial; registra exceção se o custo for inviável. |
| `LoadingOperationState` | scoped | Coordena o bloqueio visual e as mensagens de operações demoradas no circuito atual. |

Os serviços de dados usam `IDbContextFactory<AppDbContext>` diretamente. Os repositórios sem consumidores e seus registros no DI foram removidos.

## Fluxos disponíveis

- `/DataUpdates` e `/DateUpdates`: consumo por competência em CSV e atualização real do estoque por PDF, usando o mesmo `InputFileComponent`, `PdfParserService` e `PdfStorageService` da consulta de itens. O PDF deve ser o Relatório de Conferência dos Lotes do Soul MV, com até 10 MB.
- `/Estoque/Itens`: consulta dos itens ativos e importação do espelho PDF. A rota antiga `/Replacement/ReplacementGrid` é um alias da mesma tela, sem cálculos de reposição.
- `/FollowingDanfe/Following`: cadastro real de DANFEs, com diálogos de fornecedor e cotação. As abas demonstrativas foram removidas.
- `/User/Settings`, `/Login`, `/auth/login` e `/auth/logout`: autenticação e perfil preservados.
- `POST /dev/import-stock`: endpoint de desenvolvimento que importa `storage/estoque.json`. O importador de consumo e seu endpoint foram removidos.

`InputFileComponent` e `PopupComponent` aceitam somente o espelho PDF de estoque. `InputFileComponent.OnImported` informa o nome original do arquivo à tela somente depois de `PdfStorageService.SaveAsync` concluir a gravação. A aba de estoque de DataUpdates reutiliza esse componente; o histórico do nome do PDF nessa tela é da sessão, enquanto os itens, lotes, locais e saldos são persistidos pelo serviço existente. Os modelos `ProductStock`, `BatchStock` e `StockLocation` continuam transportando os dados do estoque; `ItemType` e sua classificação foram preservados.

As telas demonstrativas de compras e chegada de produtos, os gráficos, a impressão de reposição e a dependência ApexCharts foram removidos. A nova análise de consumo e a geração de pedidos ainda precisam ser implementadas.

## Verificação da limpeza

A verificação está documentada em `limpeza-projeto.txt`. Nenhuma migration de exclusão deve ser aplicada ao banco de trabalho sem considerar a perda dos dados antigos de consumo e reposição. A aplicação não executa migrations automaticamente.

## Contratos analíticos para desenvolvimento incremental

Foram adicionados nove serviços e suas interfaces para análise de consumo, ABC, XYZ manual, projeção, análise gerencial, planejamento de compras, qualidade dos dados, importação CSV e consolidação mensal. Todos estão registrados como `Scoped`. O importador CSV de consumo já implementa validação, persistência e histórico e está ligado aos cards mensais de `/DateUpdates`. Os demais serviços analíticos permanecem sem implementação de negócio.

O inventário completo de interfaces, assinaturas, DTOs e limitações do modelo está em [Contratos dos serviços analíticos](contratos-servicos-analiticos.md). A importação de consumo e suas regras de cobertura, atualização e valores negativos estão detalhadas em [Consumo e ABC](consumo-e-abc.md).

Para competências encerradas, a evolução do importador exige diário e setorial, prévia sem gravação e confirmação explícita. O fluxo parcial do mês corrente permanece separado. Veja [Importação mensal conjunta](importacao-mensal-conjunta.md) para preços informados, médias com três casas, reimportações, bloqueio da interface e limitações de rastreabilidade.
