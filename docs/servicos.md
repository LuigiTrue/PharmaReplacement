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

Os serviços de dados usam `IDbContextFactory<AppDbContext>` diretamente. Os repositórios sem consumidores e seus registros no DI foram removidos.

## Fluxos disponíveis

- `/Estoque/Itens`: consulta dos itens ativos e importação do espelho PDF. A rota antiga `/Replacement/ReplacementGrid` é um alias da mesma tela, sem cálculos de reposição.
- `/FollowingDanfe/Following`: cadastro real de DANFEs, com diálogos de fornecedor e cotação. As abas demonstrativas foram removidas.
- `/User/Settings`, `/Login`, `/auth/login` e `/auth/logout`: autenticação e perfil preservados.
- `POST /dev/import-stock`: endpoint de desenvolvimento que importa `storage/estoque.json`. O importador de consumo e seu endpoint foram removidos.

`InputFileComponent` e `PopupComponent` aceitam somente o espelho PDF de estoque. Os modelos `ProductStock`, `BatchStock` e `StockLocation` continuam transportando os dados do estoque; `ItemType` e sua classificação foram preservados.

As telas demonstrativas de compras e chegada de produtos, os gráficos, a impressão de reposição e a dependência ApexCharts foram removidos. A nova análise de consumo e a geração de pedidos ainda precisam ser implementadas.

## Verificação da limpeza

A verificação está documentada em `limpeza-projeto.txt`. Nenhuma migration de exclusão deve ser aplicada ao banco de trabalho sem considerar a perda dos dados antigos de consumo e reposição. A aplicação não executa migrations automaticamente.
