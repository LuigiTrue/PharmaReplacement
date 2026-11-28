# Importação mensal de consumo e preços informados

Em `/DateUpdates`, os cards abrem o painel de anexos da competência. Consumo diário e Consolidação Mensal de Estoque são obrigatórios; Posição de Estoque permanece condicional e somente na interface. O processamento de CSV usa `R_CONS_DIARIO` para o histórico e `R_SAI_SETOR_ESP_CONSO` exclusivamente como fonte do preço unitário informado.

## Fontes e competência

A competência do registro é a selecionada no card. O preço do CSV setorial é atribuído a essa competência como informado pelo usuário. Datas, períodos, unidades, quantidades, valores totais e conjuntos de produtos dos dois documentos **não são comparados**. Isso contempla a consolidação do Soul MV no início do mês, que pode apresentar movimentações diferentes do histórico diário completo.

A associação usa exclusivamente o código Soul MV (`items.Code`), sem relacionar por descrição nem converter unidades ou valores. Produtos exclusivos do relatório de preços não criam itens, diários ou mensais. Um produto diário sem custo determinável gera uma exceção de processamento e impede a confirmação. Nenhum custo arbitrário é atribuído; registros anteriores não são alterados em uma tentativa malsucedida.

## Extração do preço unitário

`SectorConsumptionImportService` usa CsvHelper, Windows-1252, cultura `pt-BR`, campos separados por vírgula e delimitados por aspas, colunas variáveis e cabeçalhos repetidos. Produto e Vl Unitário são necessários à leitura. Qtd Moviment. e Valor Total só são necessários para calcular o custo de um produto com dois ou mais preços positivos distintos. Setor e espécie são contexto opcional da leitura. Cabeçalhos, observações, subtotais e total geral não são produtos; seus valores não são interpretados nem validados.

O serviço mantém o nome de campo existente `MonthlyConsumption.UnitPurchasePrice` (a solicitação utiliza o termo UnitProductPrice). Os registros são agrupados por código e resolvidos assim:

1. Valores unitários zerados são desconsiderados. Suas linhas não entram nos somatórios de custo.
2. Um único valor positivo distinto é copiado diretamente, mesmo repetido em linhas ou setores. Nesse caso não há exigência de quantidade/valor total válidos nem cálculo de médias.
3. Somente com dois ou mais valores positivos distintos aplica-se `soma de Valor Total / soma de Qtd Moviment.`, utilizando todas as linhas do produto com preço positivo. Não é média simples dos preços distintos. Repetições com preços positivos também participam dos somatórios.
4. Quantidades e valores de devoluções mantêm seus sinais. Denominador zero, campos de movimentação ausentes/inválidos quando necessários, resultado não positivo, overflow ou custo não representável na precisão existente produzem erro de processamento.

Não há comparação com as quantidades do diário, e nenhuma quantidade setorial é gravada como consumo. A competência continua sendo a do card.

A ausência de preço positivo também é erro, inclusive produto com todos os preços zerados. Preços unitários inválidos ou negativos não são convertidos silenciosamente. Cada falha de custo gera uma `UnitPriceProcessingException`, registrada com código de erro e produto no logger e apresentada nos resultados como erro impeditivo. O custo permanece indisponível na prévia; a confirmação e a transação não são executadas com esses erros.

Todos os operandos financeiros utilizam `decimal`, sem arredondamento intermediário. O preço positivo único precisa caber integralmente em `numeric(18,6)`; o custo ponderado é arredondado somente ao final para as seis casas da coluna existente, com `MidpointRounding.AwayFromZero`. Um resultado que se tornaria zero nessa precisão é rejeitado. Nenhuma alteração de esquema foi realizada.

O DTO `MonthlyProductFinancialDto` é reutilizado para o custo mensal resolvido. `SectorConsumptionRecordDto` inclui os valores decimais opcionais de movimentação necessários apenas à ponderação. A prévia apresenta custos disponíveis/indisponíveis e erros de obtenção do custo, sem contadores de conciliação quantitativa ou totais financeiros entre relatórios.

## Consumo e média diária

O diário é a única fonte de `daily_consumptions.Quantity`, `monthly_consumptions.TotalQuantity`, `AverageDailyConsumption`, `RecordedDays` e `IsComplete`. Permanecem a leitura dinâmica dos blocos, valores negativos, fracionados e zero. Lacunas não se transformam em zero. Cabeçalhos/dados inválidos, duplicidades, cobertura incompleta, precisão incompatível e produtos diários não cadastrados continuam impeditivos próprios do diário.

No fechamento conjunto, a competência deve estar encerrada e cada produto deve cobrir todos os dias do mês (28/29/30/31). A atualização parcial do mês corrente continua separada, com os dias de 1 até hoje em Brasília.

A média é `decimal.Round(total / dias disponíveis, 3, MidpointRounding.AwayFromZero)`, tanto na prévia quanto na reconsulta dos diários persistidos dentro da transação. A comparação com a média informada pelo diário arredonda também esse valor para três casas. Isso não modifica as quantidades diárias nem o total mensal. A coluna existente mantém sua precisão de seis casas, mas os valores gravados pela aplicação são calculados com três casas; não há migration.

A unidade do **diário** continua sendo validada contra `items.Unit`. Isso garante a coerência do próprio histórico com o cadastro e não compara os dois relatórios. Descrições/unidades conflitantes entre blocos do próprio diário também continuam sendo erros.

## Prévia, confirmação e reimportações

Anexar não grava dados. Processar gera uma prévia e um snapshot privado válido por 15 minutos por padrão. Trocar/remover um documento invalida a prévia. Confirmar recebe somente o token do snapshot, revalida cadastro e cobertura anterior e grava diário, mensal e histórico em uma transação. A prévia nunca consulta o preço atual do estoque para completar custos.

O advisory lock PostgreSQL da competência serializa confirmações entre circuitos. Um gate no serviço rejeita operações conjuntas simultâneas no mesmo circuito. As unicidades existentes `(ItemId, Date)` e `(ItemId, Year, Month)` impedem duplicação; reimportações válidas substituem as quantidades e o custo mensal. Um custo inviável na nova tentativa bloqueia a operação e preserva os registros anteriormente confirmados. Não se misturam silenciosamente conjuntos ou coberturas diferentes de históricos já persistidos, e não há exclusão automática de dados anteriores.

O consolidado mensal é conferido com os diários efetivamente gravados na transação. Falhas no mensal ou no histórico revertem também o diário. O fluxo parcial anterior preserva um preço já informado; a importação conjunta o atualiza a partir do documento de preços.

## Interface, logs e limitações

O layout dos cards e do painel compartilhado foi preservado. `LoadingOperationState` é Scoped por circuito. `LoadingOverlay` utiliza diálogo modal nativo, fundo inerte, spinner indeterminado, mensagens reais, controle de foco e `NavigationLock`. A interface é liberada entre prévia e confirmação e em todas as saídas, inclusive exceções. O navegador controla a apresentação do alerta externo; não é possível impedir fisicamente o fechamento de uma aba. A transação mantém a integridade independentemente da permanência na página.

O histórico mantém somente o nome do diário em `consumption_imports.SourceFileName`. O segundo nome é apresentado no resultado e nos logs, sem simular rastreabilidade persistente inexistente. O preço por item/mês não é um histórico por setor, lote ou nota fiscal. O total monetário setorial não é armazenado como consumo financeiro mensal; os valores das linhas são utilizados somente quando necessários ao custo ponderado. O preço informado pelo Soul MV não comprova, por si só, a metodologia de aquisição ou valoração histórica adotada pelo sistema de origem.

## Testes

`dotnet build` e `dotnet test --no-build` são executados na raiz. Os testes PostgreSQL usam bancos temporários com nomes aleatórios criados por `EnsureCreatedAsync`, sem aplicar migrations. Configure `CONSUMPTION_TEST_CONNECTION` somente com uma instância de testes que permita criar/remover bancos. Para os documentos reais disponíveis, configure `CONSUMPTION_DAILY_REFERENCE` e `CONSUMPTION_SECTOR_REFERENCE`. Sem essas variáveis os testes dependentes são explicitamente ignorados.

Os casos incluem preço único direto, repetição do mesmo preço, exclusão de preços zero, ponderação condicional, devoluções com sinal, precisão decimal, denominador zero, dados de custo ausentes/inválidos, ausência de preço positivo, médias do diário com três casas, reimportação, cobertura, cancelamento, concorrência e rollback. As contagens de referência pertencem aos testes, nunca às regras do serviço.

## Propostas de evolução — não implementadas

1. `MonthlySectorConsumption`: apenas se futuramente houver necessidade de preservar movimentações por setor; não é requisito da importação atual de preços.
2. `MonthlyConsumption.TotalConsumptionValue`: seria necessário decidir a origem e a semântica desse valor antes de acrescentar um campo. O total setorial não é fonte do consumo financeiro nesta modalidade.
3. Documentos associados à importação: tipo, nome e hash permitiriam rastrear permanentemente os dois arquivos e suas versões.

Nenhuma entidade persistida, DbContext ou migration foi alterada para esta refatoração. As propostas acima não foram implementadas.
