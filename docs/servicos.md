# Documentação dos Serviços do RepyPharma

Este documento descreve os serviços registrados ou utilizados pelo projeto, seus métodos e onde cada serviço é consumido. As páginas e componentes são citados apenas como pontos de uso.

## Registro dos Serviços

Os serviços são registrados em `Program.cs` com ciclo de vida `Scoped`, exceto `ReplenishmentDataState`, que é `Singleton`.

- `IOrderService -> OrderService`
- `IProductStockService -> ProductStockService`
- `IStockJsonService -> StockJsonService`
- `IMinimumStockService -> MinimumStockService`
- `IReplenishmentService -> ReplenishmentService`
- `IReplenishmentDashboardService -> ReplenishmentDashboardService`
- `IReplacementSettingsService -> ReplacementSettingsService`
- `IFractionationSupplyService -> FractionationSupplyService`
- `IAuthService -> AuthService`
- `IUserSettingsService -> UserSettingsService`
- `IJsonImportService -> JsonImportService`
- `IConsumptionAverageReportService -> ConsumptionAverageReportService`
- `IGridColumnService -> GridColumnService`
- `PdfStorageService`
- `PdfValidationService`
- `PdfParserService`
- `LayoutState`
- `ThemeState`
- `ReportHtmlService`

## AuthService

Arquivo: `Services/Implementations/AuthService.cs`

Responsabilidade: manter o estado de autenticação do usuário no circuito Blazor, montar a sessão atual, validar login e notificar mudanças de autenticação.

Dependências:

- `AuthenticationStateProvider`
- `UserManager<ApplicationUser>`

Usado em:

- `Components/AuthRouteView.razor`
- `Components/Layout/MainLayout.razor`
- `Components/Layout/NavMenuOutlaw.razor`
- `Components/Layout/UserIdentityBadge.razor`
- `Components/Pages/Replacement/ReplenishmentGrid.razor`
- `Components/Pages/Replacement/ReplenishmentSettings.razor`
- `Components/Pages/User/UserSettings.razor`
- `UserSettingsService`

Métodos e membros:

- `InitializeAsync()`: carrega o `AuthenticationState` atual, monta `CurrentUser` quando o usuário está autenticado e ativo, marca `IsInitialized` como `true` e dispara `OnChange`. Se já estiver inicializado, retorna sem fazer nova leitura.
- `LoginAsync(string username, string password, bool rememberMe)`: procura o usuário por nome ou e-mail, valida se está ativo e confere a senha com `UserManager.CheckPasswordAsync`. Retorna `AuthLoginResult.Success` com uma sessão em memória quando válido, ou `Failure` com mensagem genérica quando inválido. O parâmetro `rememberMe` existe na assinatura, mas a autenticação persistente real é feita pelo endpoint `/auth/login` em `Program.cs`.
- `UpdateCurrentUserProfileAsync(string name, string avatarDataUrl)`: atualiza apenas os dados da sessão em memória (`Name` e `AvatarDataUrl`) e dispara `OnChange`; não grava diretamente no banco.
- `LogoutAsync()`: limpa `CurrentUser`, mantém o serviço como inicializado e notifica assinantes. O logout de cookie é feito pelo endpoint `/auth/logout`.
- `IsInRole(string role)`: verifica se a sessão atual possui uma role, ignorando diferença de maiúsculas/minúsculas.
- `HandleAuthenticationStateChanged(Task<AuthenticationState>)`: recebe eventos do `AuthenticationStateProvider` e delega a atualização assíncrona.
- `RefreshAuthenticationStateAsync(Task<AuthenticationState>)`: atualiza `CurrentUser` a partir do novo estado de autenticação, marca como inicializado e notifica assinantes.
- `SetCurrentUserAsync(AuthenticationState)`: quando o principal não está autenticado, limpa a sessão; quando está, carrega o `ApplicationUser`, rejeita usuários inativos e cria a sessão.
- `CreateSessionAsync(ApplicationUser)`: monta `AuthUserSession` com username, e-mail, nome, avatar, roles, flag de admin e timestamp.
- `Dispose()`: remove o handler de `AuthenticationStateChanged`.

## UserSettingsService

Arquivo: `Services/Implementations/UserSettingsService.cs`

Responsabilidade: consultar e alterar preferências do usuário atual e gerenciar solicitações de troca de senha.

Dependências:

- `IAuthService`
- `UserManager<ApplicationUser>`

Usado em:

- `Components/Layout/MainLayout.razor`
- `Components/Pages/User/UserSettings.razor`

Métodos:

- `GetCurrentProfileAsync()`: busca o usuário atual a partir da sessão do `AuthService`; se não houver usuário, retorna `null`. Quando encontra, retorna `UserProfileSettings` com username, nome, e-mail, avatar, flag de admin e status de solicitação de senha.
- `UpdateCurrentProfileAsync(string name, string avatarDataUrl)`: exige usuário autenticado e nome preenchido. Atualiza nome e avatar no Identity, propaga os dados para `AuthService.UpdateCurrentUserProfileAsync` e dispara `OnChange`.
- `RequestPasswordChangeAsync()`: marca no usuário atual `PasswordChangeRequested = true` e grava `PasswordChangeRequestedAt` em UTC. Dispara `OnChange`.
- `GetPendingPasswordRequestCountAsync()`: conta usuários com solicitação de senha pendente.
- `GetPendingPasswordRequestsAsync()`: lista solicitações pendentes ordenadas por data, projetando para `UserPasswordChangeRequest`.
- `ChangeUserPasswordAsync(string username, string newPassword)`: exige que o usuário atual seja admin, valida usuário e senha, gera token de reset, troca a senha via Identity, limpa a solicitação pendente e notifica assinantes.
- `GetCurrentApplicationUserAsync()`: busca o usuário persistido usando username ou e-mail da sessão atual.
- `GetIdentityErrorMessage(string message, IdentityResult result)`: concatena as mensagens de erro retornadas pelo Identity.

## OrderService

Arquivo: `Services/Implementations/OrderService.cs`

Responsabilidade: fornecer uma lista simulada de pedidos/itens para visualização.

Usado em:

- `Shared/Components/RadialBarComponent.razor`

Método:

- `GetOrdersAsync()`: cria quatro itens fixos (`Dipirona`, `Ondasetrona`, `Hiocina + Dipirona`, `Tramadol`) com percentuais de estoque e valores brutos aleatórios. Retorna a lista em `Task.FromResult`. É um serviço mockado, sem acesso a banco.

## ProductStockService

Arquivo: `Services/Inventory/ProductStockService.cs`

Responsabilidade: contrato inicial para consulta de estoque de produtos.

Usado em:

- Registrado no DI, mas não há referências diretas encontradas fora do registro.

Método:

- `GetProductStocksAsync()`: retorna uma lista vazia de `ProductStock`. A implementação atual é placeholder e não consulta banco nem arquivos.

## StockJsonService

Arquivo: `Services/Import/StockJsonService.cs`

Responsabilidade: consultar o estoque persistido no banco e expor uma visão agregada por produto, lote e localização. Também lê a lista de códigos ignorados em `storage/ignorados.json`.

Dependências:

- `IDbContextFactory<AppDbContext>`
- `IWebHostEnvironment`

Usado por:

- `ReplenishmentService`
- `ReplenishmentDashboardService`
- `ReplacementSettingsService`
- `MinimumStockService`

Métodos:

- `GetAllAsync()`: consulta todos os itens ativos com saldos, lotes e localizações. Projeta cada item para `ProductStock`, calcula `TotalStock`, agrupa saldos por lote, ordena lotes por validade e localizações por código.
- `GetByCodeAsync(string code)`: consulta um item ativo pelo código informado e retorna a mesma projeção de `ProductStock`. Retorna `null` quando não encontra.
- `GetLocationSummaryAsync()`: usa `GetAllAsync()`, soma as quantidades por localização conhecida (`996`, `997`, `998`, `999`, `1059`) e retorna apenas localizações com quantidade maior que zero, ordenadas por maior estoque.
- `GetIgnoredCodesAsync()`: se `storage/ignorados.json` não existir ou estiver vazio, retorna conjunto vazio. Caso exista, desserializa uma lista de strings e retorna um `HashSet<string>`.

## MinimumStockService

Arquivo: `Services/Inventory/MinimumStockService.cs`

Responsabilidade: gerenciar regras ativas de estoque mínimo (`ReplenishmentRule`) e migrar dados legados de `storage/minimos.json` quando necessário.

Dependências:

- `IDbContextFactory<AppDbContext>`
- `IWebHostEnvironment`
- `ReplenishmentDataState`
- `IStockJsonService`

Usado por:

- `ReplenishmentService`
- `ReplenishmentDashboardService`
- `ReplacementSettingsService`

Métodos:

- `GetAllAsync()`: garante a importação dos mínimos legados quando ainda não há regras no banco. Depois retorna regras ativas, incluindo item, ordenadas por nome, projetadas para `MinimumStock`.
- `GetByCodeAsync(string code)`: garante a importação legada, normaliza o código e retorna a regra ativa daquele item. Retorna `null` quando não existe.
- `SaveAsync(MinimumStock item)`: valida código, cria ou atualiza o `Item`, sincroniza nome/unidade com o estoque quando possível, classifica o tipo do item e cria ou atualiza a `ReplenishmentRule` com método `manual`, estoque mínimo e prioridade. Após salvar, chama `ReplenishmentDataState.NotifyChanged()`.
- `RemoveAsync(string code)`: localiza a regra pelo código do item e a desativa (`IsActive = false`). Não apaga fisicamente a regra. Notifica mudança nos dados de reposição.
- `GetProductsWithoutMinimumAsync()`: compara todos os produtos ativos com a lista de mínimos e retorna apenas produtos sem regra ativa.
- `EnsureLegacyMinimumsImportedAsync()`: se já existem regras no banco, não faz nada. Caso contrário, lê `storage/minimos.json`, evita códigos duplicados, cria itens ausentes como inativos e cria regras ativas com cálculo manual.

## ReplenishmentService

Arquivo: `Services/Replenishment/ReplenishmentService.cs`

Responsabilidade: gerar lista de reposição da Farmácia Central, resumo para dashboard e relatório separado por origem de abastecimento.

Dependências:

- `IStockJsonService`
- `IMinimumStockService`

Usado em:

- `Components/Pages/Shopping/ShoppingMain.razor`
- `Components/Pages/Replacement/Dashboard.razor`
- `Components/Pages/Replacement/ReplenishmentGrid.razor`

Métodos:

- `GenerateAsync()`: busca produtos, mínimos e códigos ignorados. Ignora psicotrópicos e sedativos, produtos sem mínimo e códigos ignorados. Calcula o estoque apenas da Farmácia Central (`997`), determina prioridade (`Critical`, `Warning` ou `Ok`), descarta itens `Ok`, escolhe lote recomendado por menor validade entre origens de reposição (`1059`, `999`, `996`) e retorna a lista ordenada por grupo de abastecimento, prioridade, prioridade manual e nome.
- `GetDashboardSummaryAsync()`: monta três listas: itens críticos (`NeedToBuy`), itens em alerta (`RunningLow`) e itens acima do normal (`AboveNormal`). Usa os mesmos filtros de tipo, mínimo e ignorados. Considera acima do normal quando o estoque atual é pelo menos duas vezes o mínimo.
- `GenerateReportAsync()`: chama `GenerateAsync()` e distribui os itens em seções de origem. A ordem de preferência é Fracionamento (`1059`), CAF (`999`) e Almoxarifado (`996`). Para cada seção, filtra os lotes disponíveis naquela origem e marca conflito de lote quando aplicável. Itens sem origem entram em `NoSourceAvailable`.
- `IsAboveNormal(decimal currentStock, decimal minimumQuantity)`: retorna verdadeiro quando o mínimo é maior que zero e o estoque atual é pelo menos `2x` o mínimo.
- `CalculatePriority(decimal currentStock, decimal minimumQuantity)`: retorna `Critical` quando o estoque está abaixo do mínimo; `Warning` quando está até 20% acima do mínimo; caso contrário retorna `Ok`.
- `SelectBatch(List<BatchStock> batches)`: escolhe o primeiro lote disponível em origem de reposição, ordenado por validade; se não houver validade, usa `DateTime.MaxValue`.
- `IsAvailableForReplenishment(BatchStock batch)`: verifica se o lote tem quantidade positiva em uma das origens `1059`, `999` ou `996`.
- `GetStockAtLocation(ProductStock product, string locationId)`: soma todas as quantidades do produto em uma localização específica.
- `GetAvailableBatches(List<BatchStock> batches)`: retorna todos os lotes disponíveis para reposição, ordenados por validade.
- `ShouldHideFromReplenishment(ItemType itemType)`: oculta psicotrópicos e sedativos da reposição.
- `HasStockAt(List<BatchStock> batches, string locationId)`: indica se existe lote com quantidade positiva na localização informada.
- `FilterBatchesByLocation(List<BatchStock> batches, string locationId)`: mantém somente lotes que possuem estoque na localização informada.
- `CheckLotConflict(ReplenishmentItem item)`: compara o lote recomendado com os lotes disponíveis na origem filtrada. Retorna verdadeiro quando nenhum lote disponível coincide com o recomendado.

## ReplenishmentDashboardService

Arquivo: `Services/Replenishment/ReplenishmentDashboardService.cs`

Responsabilidade: montar dados agregados e séries de gráficos para o dashboard de reposição da Farmácia Central.

Dependências:

- `IStockJsonService`
- `IMinimumStockService`

Usado em:

- Registrado no DI. O arquivo `Components/Pages/Replacement/Dashboard.razor` injeta `IReplenishmentService` e `ReplenishmentDataState`; a injeção deste serviço não apareceu na busca atual, embora o serviço esteja registrado.

Métodos:

- `GetDashboardDataAsync()`: busca produtos, mínimos e códigos ignorados. Para cada mínimo ativo maior que zero, calcula estoque atual na Farmácia Central (`997`), quantidade coberta, quantidade faltante, percentual individual, rank e grupo de abastecimento. Calcula totais, itens completos, itens abaixo do mínimo e percentual geral baseado na quantidade de itens completos. Retorna também três coleções para gráficos.
- `BuildCompletionChart(int completedItems, int belowMinimumItems)`: cria dois pontos: itens no mínimo/acima e itens abaixo do mínimo.
- `BuildMissingByItemChart(List<ReplenishmentDashboardItem> items)`: agrupa itens abaixo do mínimo por grupo de prioridade de abastecimento e conta quantos itens há em cada grupo. Se não houver pendências, retorna ponto único `Sem pendências`.
- `BuildTopReplenishmentItemsChart(List<ReplenishmentDashboardItem> items)`: seleciona até 10 itens abaixo do mínimo, priorizando rank de abastecimento e maior quantidade faltante. Usa rótulos encurtados.
- `FormatChartLabel(string label)`: limita rótulos de gráfico a 42 caracteres, com reticências.
- `GetStockAtLocation(ProductStock product, string locationId)`: soma o estoque de um produto em uma localização.

## ReplacementSettingsService

Arquivo: `Services/Replenishment/ReplacementSettingsService.cs`

Responsabilidade: consultar e alterar configurações de reposição por item, incluindo estoque mínimo, prioridade e tipo do item.

Dependências:

- `IMinimumStockService`
- `IStockJsonService`
- `IDbContextFactory<AppDbContext>`

Usado em:

- `Components/Pages/Replacement/ReplenishmentSettings.razor`

Métodos:

- `SearchPriorityItemsAsync(string searchText)`: monta a lista configurável com produtos e mínimos, normaliza busca removendo acentos e caixa, filtra por nome ou código e limita a 20 resultados. Sem busca, retorna os 20 primeiros por nome.
- `GetPriorityItemAsync(string code)`: retorna o primeiro item configurável cujo código bate com o código informado, ignorando caixa.
- `UpdateItemSettingsAsync(string code, ItemPriority priority, decimal minimumQuantity, ItemType itemType)`: valida mínimo não negativo, localiza mínimo existente ou cria base a partir do estoque. Salva mínimo/prioridade via `MinimumStockService.SaveAsync()` e atualiza o tipo do item no banco.
- `AddMinimumStockItemAsync(string code, string name, ItemPriority priority, decimal minimumQuantity)`: valida código, nome e mínimo; impede duplicidade de mínimo; se o item existir no estoque, usa o nome do produto; salva nova regra de mínimo.
- `GetConfiguredItemsAsync()`: combina produtos ativos e mínimos cadastrados. Produtos sem mínimo aparecem com mínimo zero e prioridade baixa; mínimos sem produto ativo também entram na lista com tipo comum.
- `UpdateItemTypeAsync(string code, ItemType itemType)`: localiza o `Item` no banco e atualiza `ItemType` e `UpdatedAt`.
- `Normalize(string value)`: converte para maiúsculas e remove marcas diacríticas para busca insensível a acentos.

## FractionationSupplyService

Arquivo: `Services/Replenishment/FractionationSupplyService.cs`

Responsabilidade: sugerir abastecimento da área de fracionamento com base em médias de saída e, quando não houver média, apontar faltas críticas pela regra de mínimo.

Dependências:

- `IDbContextFactory<AppDbContext>`

Usado em:

- `Components/Pages/Replacement/FractionationSupplyPanel.razor`

Métodos:

- `GetSupplyDataAsync(int coverageDays)`: limita cobertura entre 1 e 30 dias. Carrega itens ativos exceto psicotrópicos/sedativos, regras, saldos, lotes e localizações. Busca a média de consumo mais recente por item. Para itens com média semanal, calcula consumo diário, quantidade necessária para o período e sugestão de reposição para fracionamento (`1059`). Para itens sem média, inclui em faltas por mínimo quando o estoque da Farmácia Central (`997`) está até 50% do mínimo. Retorna listas ordenadas de reposição e faltas.
- `BuildSupplyItem(...)`: monta `FractionationSupplyItem` com dados do item, médias, estoque no fracionamento, estoque na farmácia, mínimo, sugestão, período de referência e lotes disponíveis.
- `GetWeeklyAverageOutput(ItemConsumptionAverage average)`: prioriza média semanal; quando só existe média mensal ou atual, converte para média semanal usando `CoverageDays`.
- `GetStockAtLocation(Item item, string locationId)`: soma saldos do item em uma localização.
- `GetAvailableBatches(IEnumerable<StockBalance> stockBalances)`: considera apenas origens `999` e `996` com quantidade positiva, agrupa por lote, ordena por validade e monta `BatchStock`.

## ReportHtmlService

Arquivo: `Services/Replenishment/ReportHtmlService.cs`

Responsabilidade: gerar HTML imprimível para o relatório de reposição.

Usado em:

- `Components/Pages/Replacement/ReplenishmentGrid.razor`

Métodos:

- `GenerateReplacementHtml(ReplenishmentReport report)`: cria um documento HTML completo com estilos inline, data de geração, total de itens, avisos e três seções: Fracionamento, CAF e Almoxarifado. Usa `AppendSection` para renderizar cada etapa.
- `GetReportGenerationTime()`: retorna `DateTime.Now` para exibir no relatório.
- `GetTotalItemsCount(ReplenishmentReport report)`: soma itens de Fracionamento, CAF, Almoxarifado e sem origem disponível.
- `AppendSection(StringBuilder sb, string title, List<ReplenishmentItem> items, string locationId)`: renderiza uma seção. Se não houver itens, exibe mensagem vazia. Caso contrário, cria tabela com código, nome e lotes disponíveis naquela localização. Destaca o lote recomendado e marca linhas com conflito.

## ReplenishmentPriorityPolicy

Arquivo: `Services/Replenishment/ReplenishmentPriorityPolicy.cs`

Responsabilidade: classificar itens para ordenação de abastecimento com base no nome do item e na prioridade manual.

Usado por:

- `ReplenishmentService`
- `ReplenishmentDashboardService`

Métodos:

- `GetSupplyRank(string itemName)`: normaliza o nome e classifica como medicamento prioritário, medicamento comum ou material com base em listas de termos.
- `GetSupplyRank(string itemName, ItemPriority itemPriority)`: considera prioridade manual. Materiais continuam como materiais; prioridade `UltraHigh` ou termos críticos viram medicamento prioritário; prioridade `High` vira medicamento comum; termos de medicamento também viram medicamento comum.
- `GetSupplyGroupLabel(string itemName)`: converte o rank em rótulo de grupo.
- `GetSupplyGroupLabel(string itemName, ItemPriority itemPriority)`: mesma conversão, usando prioridade manual.
- `GetEffectiveItemPriority(MinimumStock minimum, string itemName)`: ajusta a prioridade efetiva do item: medicamentos críticos viram `UltraHigh`; medicamentos comuns com prioridade acima de `High` são reduzidos para `High`; demais mantêm a prioridade configurada.
- `ContainsAny(string value, IEnumerable<string> terms)`: verifica se algum termo existe no texto normalizado.
- `Normalize(string value)`: converte para maiúsculas e remove acentos.

## JsonImportService

Arquivo: `Services/Import/JsonImportService.cs`

Responsabilidade: importar arquivos JSON de estoque e consumo diário para o banco.

Dependências:

- `IDbContextFactory<AppDbContext>`
- `IWebHostEnvironment`
- `ILogger<JsonImportService>`

Usado em:

- Endpoint de desenvolvimento `POST /dev/import-stock` em `Program.cs`

Métodos:

- `ImportStockAsync(string filePath)`: valida existência do arquivo, desserializa `List<StockJsonDto>`, abre transação e importa cada item. Cria/atualiza itens, lotes, localizações e saldos de estoque. Registra erros por item sem interromper toda a importação quando possível. Faz commit ao final ou rollback em falha de transação.
- `ImportDailyConsumptionAsync(string filePath)`: valida e desserializa o arquivo de consumo diário. Para cada registro, valida item e data, cria `DailyConsumption` e acumula contadores no `ImportResult`.
- `ImportAllAsync()`: importa `storage/estoque.json` e, se existir, `storage/consumo-diario.json`, combinando resultados com `Merge`.
- `ImportStockItemAsync(...)`: valida código, cria item se não existir, atualiza nome/unidade/tipo se mudou e delega importação dos lotes.
- `ImportBatchAsync(...)`: valida lote e validade, cria ou atualiza `Batch` e importa os saldos por localização.
- `ImportStockBalanceAsync(...)`: valida localização, cria localização ausente, cria ou atualiza saldo para a combinação item/lote/localização.
- `ImportDailyConsumptionItemAsync(...)`: valida código, existência do item e data; cria registro de consumo diário.
- `GetConsumptionItemCode(DailyConsumptionJsonDto consumptionDto)`: escolhe `Code` quando preenchido; caso contrário usa `ItemCode`.
- `ToUtc(DateTime date)`: normaliza `DateTime` para UTC.
- `IsInvalidDate(DateTime date)`: considera inválido `default` e `DateTime.MinValue`.
- `GetExceptionMessage(DbUpdateException ex)`: inclui mensagem da inner exception quando disponível.

## ConsumptionAverageReportService

Arquivo: `Services/Import/ConsumptionAverageReportService.cs`

Responsabilidade: ler um relatório PDF de médias de saída, extrair itens e gravar médias de consumo por item.

Dependências:

- `IDbContextFactory<AppDbContext>`
- `ILogger<ConsumptionAverageReportService>`

Usado em:

- `Shared/Components/InputFileComponent.razor`
- Endpoint de desenvolvimento `POST /dev/import-consumption-average-report` em `Program.cs`

Métodos:

- `ImportPdfAsync(string filePath)`: valida arquivo, chama `ParseReport`, preenche metadados do resultado, abre transação, cruza códigos extraídos com itens existentes e cria ou atualiza `ItemConsumptionAverage` por item e período. Conta itens ausentes, registros criados, atualizados e erros.
- `ParseReport(string filePath)`: abre o PDF, extrai linhas por página, identifica período do relatório, data de geração e seção com coluna `Média`. Extrai itens, remove duplicados por código usando o último registro e determina tipo de período pela cobertura.
- `ExtractLines(Page page)`: agrupa palavras pela posição vertical, ordena por leitura e monta linhas de texto.
- `IsAverageSectionHeader(string line)`: identifica o cabeçalho da seção de médias quando a linha contém `Produto`, `Total` e `Média`.
- `TryParseAverageLine(string line)`: divide a linha, valida código e campos numéricos, extrai nome e valores de total, média, saldo e cobertura projetada.
- `ExtractItemName(string line, string code, string firstValue)`: recorta o nome entre o código do item e o primeiro valor numérico.
- `TryReadReportPeriod(string line, out DateTime? startDate, out DateTime? endDate)`: usa regex para identificar período `dd/MM/yyyy até dd/MM/yyyy`.
- `TryReadReportGeneratedAt(string line)`: identifica data/hora gerada no formato `dd/MM/yyyy HH:mm`.
- `ParseDate(string value)`: converte data `pt-BR`.
- `IsDecimal(string value)`: valida número decimal com regex.
- `ParseDecimal(string value)`: converte decimal com vírgula para `decimal`.
- `ToUtcDate(DateTime date)`: mantém apenas a data e marca como UTC.
- `GetAveragePeriodKind(int coverageDays)`: classifica até 8 dias como `weekly`, 28 ou mais como `monthly`, e o restante como `current`.

## PdfParserService

Arquivo: `Services/Import/PdfParserService.cs`

Responsabilidade: extrair dados de estoque de um PDF de conferência de lotes.

Usado em:

- `Shared/Components/InputFileComponent.razor`

Métodos:

- `ExtractText(string filePath)`: abre o PDF e concatena o texto bruto de todas as páginas.
- `ParseProducts(string path)`: percorre páginas, encontra linhas de produto pelo código, extrai nome, unidade, estoque total e lotes. Une continuação de produtos quando o mesmo código aparece em sequência.
- `GetProductLines(List<Word> allWords)`: encontra palavras na coluna de código que correspondem a códigos numéricos de 3 a 6 dígitos e agrupa por linha vertical.
- `ExtractFieldWithContinuation(List<Word> allWords, double yAtual, double minX, double maxX)`: extrai texto de uma coluna e continua lendo linhas abaixo enquanto não houver novo código e a distância vertical estiver dentro do limite.
- `ExtractTotalStock(List<Word> allWords, double yAtual)`: lê a coluna de estoque total e converte para decimal.
- `ExtractBatches(List<Word> allWords, double yAtual)`: lê lote, validade, localização e quantidade; agrupa localizações por lote e para ao encontrar o próximo código de produto.
- `IsValidBatch(string value)`: aceita lotes alfanuméricos com dígitos e alguns separadores; rejeita vazio e o texto `DE LUZIANIA`.
- `MergeWithPrevious(ProductStock produto, string nome, string unidade, List<BatchStock> lotes)`: concatena nome/unidade continuados e mescla lotes repetidos no produto anterior.
- `ExtractLineText(List<Word> words, double y)`: monta o texto de uma linha a partir das palavras na mesma coordenada vertical.
- `ParseDecimal(string? value)`: converte valores com separador brasileiro para decimal; retorna zero se não conseguir.
- `ParseDate(string? value)`: extrai data `dd/MM/yyyy` do texto e converte para `DateTime?`.

## PdfValidationService

Arquivo: `Services/Import/PdfValidationService.cs`

Responsabilidade: validar se o texto extraído parece ser um PDF de estoque hospitalar esperado.

Usado em:

- `Shared/Components/InputFileComponent.razor`

Método:

- `IsValidHospitalStockPdf(string text)`: retorna `true` apenas quando o texto contém `SOULMV - Sistema de Gerenciamento de Estoque`, `Relatório de Conferência dos Lotes` e `Produto`.

## PdfStorageService

Arquivo: `Infrastructure/Json/PdfStorageService.cs`

Responsabilidade: persistir no banco os produtos extraídos do PDF de estoque e sincronizar o estado ativo do estoque.

Dependências:

- `IDbContextFactory<AppDbContext>`
- `ReplenishmentDataState`

Usado em:

- `Shared/Components/InputFileComponent.razor`

Métodos:

- `SaveAsync(List<ProductStock> produtos)`: abre transação, cria/atualiza itens, lotes, localizações e saldos vindos do PDF. Remove saldos que não apareceram na importação atual, marca itens ausentes como inativos, remove lotes órfãos, confirma a transação e chama `ReplenishmentDataState.NotifyChanged()`.
- `GetLocationName(string code)`: retorna o nome amigável para localizações conhecidas (`996`, `997`, `998`, `999`, `1059`) ou o próprio código quando desconhecido.
- `ToUtc(DateTime date)`: normaliza datas para UTC.
- `StockBalanceKey`: record interno usado para comparar saldos importados contra saldos antigos e remover registros obsoletos.

## IGridColumnService / GridColumnService

Arquivo: `Services/Abstractions/IGridColumnService.cs`

Responsabilidade: gerar definições de colunas de grid a partir de expressões de propriedades.

Usado em:

- Registrado no DI, sem referências diretas encontradas na busca atual.

Métodos:

- `Generate<T>(params Expression<Func<T, object>>[] properties)`: para cada expressão de propriedade, cria `GridColumnDefinition<T>` com propriedade, título gerado e `Sortable = true`.
- `GetPropertyName<T>(Expression<Func<T, object>> expression)`: obtém o nome da propriedade a partir de `MemberExpression` ou `UnaryExpression`. Lança exceção se a expressão não representar uma propriedade.
- `GenerateTitle(string propertyName)`: divide nomes em PascalCase inserindo espaço entre letras minúsculas e maiúsculas.

## ThemeState

Arquivo: `Services/Abstractions/ThemeState.cs`

Responsabilidade: manter e notificar o estado de tema claro/escuro.

Usado em:

- `Components/Layout/MainLayout.razor`
- `Components/Layout/NavMenuOutlaw.razor`
- `Shared/Components/InputFileComponent.razor`

Métodos:

- `Toggle()`: alterna `IsDarkMode` e dispara `OnChange`.
- `NotifyStateChanged()`: invoca o evento `OnChange`.

## LayoutState

Arquivo: `Services/Abstractions/LayoutState.cs`

Responsabilidade: manter e notificar o estado de colapso do menu.

Usado em:

- Registrado no DI, sem referências diretas encontradas na busca atual.

Métodos:

- `Toggle()`: alterna `IsMenuCollapsed` e dispara `OnChange`.
- `Collapse()`: força `IsMenuCollapsed = true` e dispara `OnChange`.
- `NotifyStateChanged()`: invoca `OnChange`.

## ReplenishmentDataState

Arquivo: `Services/Abstractions/ReplenishmentDataState.cs`

Responsabilidade: atuar como barramento simples de notificação quando os dados de reposição/estoque mudam.

Usado em:

- `Shared/Components/InputFileComponent.razor`
- `Components/Pages/Replacement/Dashboard.razor`
- `Components/Pages/Replacement/ReplenishmentGrid.razor`
- `MinimumStockService`
- `PdfStorageService`

Método:

- `NotifyChanged()`: dispara `OnChange` para que dashboards e grids recarreguem dados.

## ImportResult

Arquivo: `Services/Import/ImportResult.cs`

Responsabilidade: carregar contadores e mensagens de erro da importação JSON.

Usado por:

- `JsonImportService`

Métodos:

- `AddError(string message)`: incrementa `Errors` e adiciona a mensagem em `ErrorMessages`.
- `Merge(ImportResult other)`: soma todos os contadores de outro resultado e concatena as mensagens de erro. Usado por `ImportAllAsync()`.

## ConsumptionAverageImportResult

Arquivo: `Services/Import/ConsumptionAverageImportResult.cs`

Responsabilidade: carregar contadores, metadados do relatório e mensagens de erro da importação de médias de saída por PDF.

Usado por:

- `ConsumptionAverageReportService`
- `Shared/Components/InputFileComponent.razor`

Método:

- `AddError(string message)`: incrementa `Errors` e adiciona a mensagem em `ErrorMessages`.
