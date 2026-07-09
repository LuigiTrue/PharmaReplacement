# Análise da Planilha `ABASTECIMENTO FARMÁCIA 2026.xlsx`

Arquivo analisado: `/home/luigi/Downloads/ABASTECIMENTO FARMÁCIA 2026.xlsx`

Esta análise descreve a estrutura da planilha usada pela gestão do hospital, suas funções principais, como ela apresenta dados e como se compara ao RepyPharma.

## Estrutura Geral

A planilha possui 5 abas:

| Aba | Papel principal | Tamanho aproximado | Fórmulas |
| --- | --- | ---: | ---: |
| `PARÂMETROS` | Visão consolidada de consumo/saldo/projeção | 245 linhas x 10 colunas | 1466 |
| `REPOSIÇÃO DIÁRIA` | Lista operacional de itens a solicitar | 498 linhas x 15 colunas | 1524 |
| `TODOS ITENS` | Cadastro mestre e classificação dos itens | 2465 linhas x 11 colunas | 15069 |
| `ITENS CRÍTICOS` | Itens críticos por projeção/ruptura | 1501 linhas x 41 colunas | 15525 |
| `CONSUMOS` | Base de consumo por dia, total, média e saldo | 1756 linhas x 51 colunas | 12827 |

A planilha é fortemente orientada por fórmulas dinâmicas do Excel, especialmente:

- `FILTER`
- `SORT`
- `UNIQUE`
- `VLOOKUP`
- `COUNT`
- `IFERROR`
- cálculos diretos de projeção e sugestão.

No arquivo XLSX essas fórmulas aparecem como funções dinâmicas; várias delas estão encapsuladas internamente como `__xludf.DUMMYFUNCTION`, o que é comum quando o arquivo usa funções de Excel moderno e é lido fora do Excel.

## Como a Planilha Funciona

### 1. Aba `CONSUMOS`

Esta é a base operacional de consumo.

Ela apresenta blocos com:

- código do produto;
- descrição;
- unidade;
- consumo diário em um intervalo de 7 dias;
- total consumido;
- média;
- saldo;
- projeção.

Exemplo de cabeçalho:

```text
Produto | Descrição | Unidade | 6 | 7 | 8 | 9 | 10 | 11 | 12 | Total | Média | Saldo | Projeç.
```

Pelo conteúdo analisado, a média é calculada como total dividido pelos dias do período. Exemplo:

- `ACIDO ASCORBICO`: total 2, média 0,286.
- `ADENOSINA`: total -3, média -0,429.

A planilha mantém também blocos consolidados mais à direita. Um trecho importante usa:

```excel
UNIQUE(...)
VLOOKUP(...)
IFERROR(...)
```

Isso indica que a aba consolida itens vindos de mais de uma base/bloco, remove duplicidades por produto e monta uma lista final com média, saldo e projeção.

### 2. Aba `PARÂMETROS`

É uma visão consolidada e mais limpa dos dados de consumo.

Colunas principais:

```text
Produto | Descrição | Unidade | Média | Saldo | Projeç.
```

Também exibe parâmetros globais:

- `Dias de reposição: 4`
- `Última atualização: 13/02/2026 08:42:01`

Função principal:

- trazer uma lista filtrada da aba `CONSUMOS`;
- exibir média, saldo e projeção de cada item;
- servir como resumo geral para análise da gestão.

Observação importante:

A aba aceita médias negativas e fracionadas. Isso aparece em itens como `ADENOSINA`, que possui média `-0,429`. Para uso operacional em reposição, isso exige tratamento, porque saída negativa não representa consumo real.

### 3. Aba `TODOS ITENS`

É o cadastro mestre dos itens.

Colunas principais:

```text
Produto | Descrição | Unidade | MAT/MED | Situação
```

Exemplos de classificação:

- `MED`
- `MAT`
- `OPME`
- `EXP`

Exemplos de situação:

- `Em uso`
- `Inativo`

A aba também possui uma área de validação:

```text
ITENS COM ESPÉCIE NÃO CADASTRADA
```

Função principal:

- manter uma base de referência para classificar item como material/medicamento;
- filtrar itens inativos;
- identificar itens sem espécie/classificação cadastrada.

### 4. Aba `ITENS CRÍTICOS`

Esta aba monta a lista de itens considerados críticos.

Ela usa dados de consumo/saldo/projeção e cruza com `TODOS ITENS` para:

- remover itens inativos;
- classificar como material ou medicamento;
- separar materiais hospitalares e medicamentos;
- montar listas auxiliares de itens zerados.

Parâmetros relevantes localizados:

```text
Dias de Reposição: 4
Última atualização: 13/02/2026 08:42:01
```

Há uma lista lateral de itens zerados, usada pela aba `REPOSIÇÃO DIÁRIA`.

Exemplo:

```text
ITENS ZERADOS: 12
```

Funções observadas:

- filtros por projeção;
- busca de situação do item em `TODOS ITENS`;
- separação por `MAT/MED`;
- geração de lista de zerados.

### 5. Aba `REPOSIÇÃO DIÁRIA`

É a tela mais operacional da planilha.

Ela apresenta:

- itens zerados no topo;
- seção de `MATERIAL HOSPITALAR`;
- seção de `MEDICAMENTOS`;
- total de itens;
- colunas de cálculo para solicitação.

Colunas principais nas seções:

```text
Produto | Descrição | Unidade | Média | Saldo | Projeç. | Solicitar
```

Fórmula central de solicitação:

```excel
(Média * Dias de Reposição) - Saldo
```

Na planilha, `Dias de Reposição` está em `ITENS CRÍTICOS!AN2`, atualmente com valor `4`.

Exemplo de fórmula:

```excel
IF(A9<>"",(D9*'ITENS CRÍTICOS'!$AN$2)-E9,"")
```

Isso significa que a planilha calcula quanto pedir para cobrir X dias, descontando o saldo atual.

## Pontos Fortes da Planilha

1. Visão operacional direta.

A aba `REPOSIÇÃO DIÁRIA` é objetiva para quem precisa separar ou solicitar itens. Ela já entrega uma lista pronta de material e medicamento.

2. Parâmetro simples de cobertura.

O campo `Dias de Reposição` permite alterar rapidamente o horizonte de reposição sem mexer nas fórmulas principais.

3. Separação entre material e medicamento.

A planilha usa `MAT/MED` para segmentar a operação, o que ajuda no abastecimento por tipo de item.

4. Controle de inativos.

Itens marcados como `Inativo` em `TODOS ITENS` são filtrados nas listas operacionais.

5. Lista de itens zerados.

A planilha destaca rupturas, o que é útil para priorização imediata.

6. Validação de cadastro.

A área `ITENS COM ESPÉCIE NÃO CADASTRADA` ajuda a identificar falhas de classificação.

## Fragilidades da Planilha

1. Fórmulas complexas e frágeis.

Há milhares de fórmulas espalhadas pelas abas. Alterações manuais podem quebrar filtros, buscas ou intervalos.

2. Valores negativos entram nos cálculos.

Itens com consumo negativo aparecem nas bases, como `ADENOSINA` com média `-0,429`. Isso pode gerar projeções negativas ou comportamento operacional confuso.

3. Médias fracionadas são apresentadas diretamente.

Exemplos como `0,286` podem aparecer como consumo médio, mas reposição real costuma trabalhar com unidade inteira. O sistema precisa decidir se arredonda, trunca ou usa total consumido.

4. Cadastro e operação estão misturados.

A planilha concentra cadastro, parâmetros, consumo, regras e saída operacional no mesmo arquivo.

5. Baixa rastreabilidade.

É difícil auditar quem alterou parâmetro, classificação ou fórmula.

6. Sem validação forte de entrada.

A planilha depende de o usuário colar/importar dados no formato correto.

## Comparação com o RepyPharma

### Similaridades

| Planilha | RepyPharma |
| --- | --- |
| Usa código, descrição e unidade do item | `Item.Code`, `Item.Name`, `Item.Unit` |
| Calcula média de saída | `ItemConsumptionAverage` e tela de fracionamento |
| Usa saldo de estoque | `StockBalance` por lote/localização |
| Trabalha com projeção/cobertura | `CoverageDays`, `RequiredQuantity`, dashboards |
| Separa material/medicamento | `ItemType` e regras de prioridade |
| Filtra itens inativos | `Item.IsActive` |
| Gera listas de reposição | `ReplenishmentService` e `FractionationSupplyService` |
| Usa horizonte de dias | controle de dias de cobertura no fracionamento |

### Diferenças

| Tema | Planilha | RepyPharma |
| --- | --- | --- |
| Persistência | Arquivo Excel | Banco PostgreSQL |
| Cálculo | Fórmulas em células | Serviços C# centralizados |
| Estoque | Saldo consolidado por item | Saldo por item, lote e localização |
| Lotes | Não aparece como foco principal | Lotes e validade são parte central |
| Auditoria | Limitada | Pode evoluir com banco/logs |
| Importação | Depende da estrutura da planilha | Importa PDF/JSON para banco |
| Validação | Fraca e manual | Pode validar antes de salvar |
| UI | Abas e tabelas Excel | Componentes Blazor com grids e cards |
| Separação de regras | Misturada nas fórmulas | Serviços separados por responsabilidade |

## Lacunas do RepyPharma em Relação à Planilha

1. Tela equivalente à `REPOSIÇÃO DIÁRIA`.

O sistema já possui reposição e fracionamento, mas a planilha tem uma visão diária muito específica: medicamentos e materiais lado a lado, com campo `Solicitar`.

2. Destaque explícito de itens zerados.

A planilha dá visibilidade imediata a rupturas. O sistema pode ter um bloco ou filtro específico para estoque zero.

3. Cadastro de classificação incompleta.

A planilha mostra `ITENS COM ESPÉCIE NÃO CADASTRADA`. O RepyPharma poderia ter um painel de saneamento cadastral para itens sem tipo confiável.

4. Campo de cobertura global para reposição diária.

O fracionamento já tem dias de cobertura. A reposição geral poderia ter um parâmetro semelhante para calcular `Solicitar = média * dias - saldo`.

5. Tratamento explícito de consumo negativo.

A planilha deixa valores negativos aparecerem; o sistema deve tratar isso de forma mais segura, ignorando consumo negativo ou tratando como ajuste.

## Onde o RepyPharma Já Supera a Planilha

1. Modelo de estoque mais fiel.

O sistema separa item, lote, localização e saldo. Isso permite decisões por origem de reposição, validade e disponibilidade real.

2. Menos risco de quebra por fórmula.

As regras ficam em serviços versionados, não em células editáveis.

3. Melhor base para auditoria.

Como os dados estão em banco, é possível evoluir para histórico de importações, usuário responsável e logs.

4. Validação antes de persistir.

O sistema já foi ajustado para validar PDFs antes de salvar no banco.

5. Interface mais controlada.

O usuário não precisa manipular intervalos, fórmulas ou filtros diretamente.

## Recomendações Para Aproximar o Sistema do Uso Real da Gestão

1. Criar uma visão de `Reposição Diária`.

Essa tela deveria conter:

- itens zerados;
- materiais;
- medicamentos;
- média semanal;
- saldo atual;
- projeção;
- quantidade a solicitar.

2. Reproduzir a fórmula operacional da planilha como serviço.

Regra:

```text
Solicitar = max(0, floor(Média semanal) * Dias de reposição - Saldo)
```

O uso de `floor` evita sugerir quantidades fracionadas.

3. Tratar consumo negativo como zero.

Valores negativos provavelmente representam ajuste/devolução, não consumo. Para reposição, devem ser ignorados ou exibidos em alerta separado.

4. Criar painel de cadastro incompleto.

Equivalente ao `ITENS COM ESPÉCIE NÃO CADASTRADA`, mas usando `ItemType`.

5. Criar marcador de estoque zerado.

Pode ser um card no dashboard ou uma seção antes da lista de reposição.

6. Guardar parâmetros de gestão.

O valor de `Dias de Reposição` deveria ser configurável no sistema, não fixo no código.

7. Exibir data da última atualização.

A planilha destaca `Última atualização`. O sistema poderia mostrar a data do último PDF/estoque importado.

## Conclusão

A planilha é uma ferramenta operacional madura para a rotina da gestão: ela consolida consumo, saldo, projeção, itens críticos, zerados e sugestão de reposição diária.

O RepyPharma já tem uma arquitetura mais robusta, com banco de dados, entidades normalizadas, serviços e validação. A principal diferença é que a planilha ainda possui uma visão operacional direta que o sistema deve replicar: a lista diária com `Solicitar`, separada por material e medicamento, baseada em média semanal, saldo e dias de reposição.

O caminho mais adequado não é copiar a planilha por completo, mas transformar suas regras úteis em serviços do sistema, preservando o que o RepyPharma já faz melhor: rastreabilidade, validação, controle por lote/localização e menor risco de erro manual.
