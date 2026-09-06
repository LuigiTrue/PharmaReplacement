# RepyPharma

Sistema web para **gestão e apoio à reposição de estoque em farmácia hospitalar**, desenvolvido em .NET 8.

O projeto transforma dados de estoque e consumo provenientes de relatórios externos em informações operacionais para auxiliar decisões como **quais itens precisam ser repostos, de onde devem ser abastecidos e quais lotes devem ser priorizados**.

## Sobre o projeto

O RepyPharma surgiu a partir de um problema real de operação: informações importantes para reposição de medicamentos e materiais estavam distribuídas entre relatórios e planilhas, exigindo cálculos, filtros e conferências manuais.

A aplicação centraliza esse processo em um sistema web, utilizando banco de dados relacional e regras de negócio implementadas em C#.

O fluxo principal é:

```text
Relatórios PDF / JSON
        ↓
Validação e parsing
        ↓
Importação e normalização
        ↓
PostgreSQL
        ↓
Regras de estoque e reposição
        ↓
Dashboard / Grades / Relatórios
```

O objetivo não é apenas registrar estoque, mas **transformar dados operacionais em decisões de abastecimento mais previsíveis e rastreáveis**.

---

## Principais funcionalidades

* Importação e validação de dados provenientes de relatórios PDF e arquivos JSON.
* Controle de estoque por **item, lote e localização**.
* Registro e utilização da validade dos lotes.
* Configuração individual de estoque mínimo e prioridade dos itens.
* Identificação automática de itens críticos ou próximos do estoque mínimo.
* Geração de listas de reposição para a Farmácia Central.
* Identificação da origem disponível para abastecimento.
* Priorização de lotes considerando disponibilidade e menor validade.
* Dashboard com indicadores de itens críticos, em alerta e acima do estoque esperado.
* Importação de médias de consumo diretamente de relatórios PDF.
* Fluxo específico para abastecimento de fracionamento.
* Geração de relatórios operacionais em HTML.
* Autenticação, autorização por roles e gerenciamento de usuários.

---

## Tecnologias

| Tecnologia                | Utilização                                              |
| ------------------------- | ------------------------------------------------------- |
| **C# / .NET 8**           | Backend e regras de negócio                             |
| **ASP.NET Core / Blazor** | Aplicação web com componentes interativos no servidor   |
| **Entity Framework Core** | ORM e persistência                                      |
| **PostgreSQL**            | Banco de dados relacional                               |
| **Npgsql**                | Provider PostgreSQL para EF Core                        |
| **ASP.NET Core Identity** | Autenticação, autorização e gerenciamento de usuários   |
| **Microsoft Fluent UI**   | Componentes de interface                                |
| **ApexCharts**            | Visualizações e dashboards                              |
| **PdfPig**                | Extração e processamento de relatórios PDF              |
| **Docker**                | Build e execução da aplicação em containers             |
| **Render / Aiven**        | Infraestrutura utilizada para deploy e banco PostgreSQL |

---

## Arquitetura

O projeto está organizado por responsabilidades, mantendo interface, regras de negócio, domínio e infraestrutura separados.

```text
PharmaReplacement/
│
├── Components/          # Páginas e componentes Blazor
├── Data/                # DbContext e configuração do PostgreSQL
├── Domain/
│   └── Entities/        # Entidades e tipos do domínio
├── Infrastructure/
│   ├── Identity/        # Autenticação e seed de usuários/roles
│   ├── Json/            # Infraestrutura relacionada a arquivos JSON
│   └── Repositories/    # Persistência e acesso a dados
├── Migrations/          # Migrations do Entity Framework Core
├── Models/              # Modelos utilizados pela aplicação
├── Services/
│   ├── Abstractions/
│   ├── Implementations/
│   ├── Import/
│   ├── Inventory/
│   └── Replenishment/
├── ViewModels/
├── docs/                # Documentação técnica
├── wwwroot/
│
├── Program.cs
├── Dockerfile
└── RepyPharma.csproj
```

Essa separação permite que as regras de reposição sejam evoluídas sem concentrar lógica de domínio nos componentes da interface.

---

## Modelo de dados

O estoque foi modelado de forma normalizada.

As principais entidades são:

```text
Item
 ├── Batches
 ├── StockBalances
 ├── DailyConsumptions
 ├── ConsumptionAverages
 └── ReplenishmentRule

Batch
 └── StockBalances

Location
 └── StockBalances
```

### Exemplo

Um medicamento pode possuir:

```text
Dipirona 500 mg
│
├── Lote A
│   ├── Farmácia Central: 120 un.
│   └── Almoxarifado: 500 un.
│
└── Lote B
    ├── Farmácia Central: 40 un.
    └── CAF: 300 un.
```

Dessa forma, o sistema consegue determinar não apenas **quanto existe do produto**, mas também:

* onde ele está;
* em qual lote;
* qual a validade;
* qual origem pode abastecer a Farmácia Central.

---

## Lógica de reposição

A regra de reposição cruza informações de estoque com parâmetros configurados para cada item.

De forma simplificada:

```text
Estoque atual da Farmácia Central
            +
Estoque mínimo configurado
            +
Prioridade do item
            +
Estoques disponíveis em outras localizações
            +
Lotes e respectivas validades
            ↓
       ReplenishmentService
            ↓
Lista de itens que precisam de reposição
```

Os itens podem ser classificados, por exemplo, como:

```text
Critical
Warning
Ok
```

Itens que não necessitam de reposição são removidos da lista operacional.

Quando existe estoque em outra localização, o sistema também procura um lote disponível e prioriza os lotes com menor validade.

---

## Importação de relatórios

Uma parte importante do projeto é a integração com dados produzidos por sistemas externos sem depender de uma API.

O RepyPharma possui serviços responsáveis por:

```text
Upload
  ↓
Validação do documento
  ↓
Extração das páginas
  ↓
Parsing das linhas
  ↓
Conversão para objetos do domínio
  ↓
Validação dos dados
  ↓
Persistência no PostgreSQL
```

Relatórios PDF de consumo são processados com **PdfPig**, permitindo extrair informações como:

* código do item;
* descrição;
* período do relatório;
* saída total;
* média de consumo;
* saldo;
* cobertura projetada.

As importações utilizam identificadores e restrições de unicidade para reduzir duplicidades em processamentos repetidos.

---

## Decisões técnicas

Algumas decisões de arquitetura tomadas durante o desenvolvimento:

### Estoque separado de produto

O saldo não é armazenado diretamente no cadastro do item.

Ele pertence à combinação:

```text
Item + Lote + Localização
```

Isso permite representar corretamente estoques distribuídos em diferentes setores.

### Regra de reposição separada do item

Parâmetros como:

* estoque mínimo;
* prioridade;
* estoque de segurança;
* lead time;
* cobertura desejada;

pertencem a uma entidade específica de reposição, evitando misturar cadastro do produto com regras operacionais.

### Validação antes da persistência

Os arquivos importados passam por etapas de validação e parsing antes da gravação no banco, reduzindo o risco de persistir relatórios incompatíveis ou dados incompletos.

### Regras centralizadas em serviços

Cálculos e decisões de reposição ficam em serviços C#, em vez de serem implementados diretamente na interface ou em fórmulas de planilhas.

Isso facilita manutenção, testes e evolução das regras de negócio.

---

## Autenticação e segurança

A aplicação utiliza **ASP.NET Core Identity**.

Entre os recursos implementados estão:

* autenticação por usuário e senha;
* armazenamento seguro de hashes de senha pelo Identity;
* roles;
* bloqueio após tentativas de login inválidas;
* controle de usuários ativos/inativos;
* cookies HTTP-only;
* política de cookies segura em produção;
* gerenciamento de perfil;
* fluxo de solicitação de alteração de senha.

---

## Executando localmente

### Pré-requisitos

* .NET SDK 8
* PostgreSQL
* Entity Framework Core CLI

Clone o repositório:

```bash
git clone https://github.com/LuigiTrue/PharmaReplacement.git
cd PharmaReplacement
```

Configure a conexão com PostgreSQL:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=repypharm;Username=SEU_USUARIO;Password=SUA_SENHA"
```

Opcionalmente, configure o usuário administrativo inicial:

```bash
export IdentitySeed__AdminUserName="admin"
export IdentitySeed__AdminEmail="admin@example.com"
export IdentitySeed__AdminPassword="SUA_SENHA_FORTE"
```

Restaure as dependências:

```bash
dotnet restore
```

Aplique as migrations:

```bash
dotnet ef database update
```

Execute:

```bash
dotnet run
```

---

## Docker

O projeto possui build multi-stage para .NET 8.

```bash
docker build -t repypharma .
```

Exemplo de execução:

```bash
docker run --rm \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="SUA_CONNECTION_STRING" \
  repypharma
```

---

## Documentação técnica

Documentações adicionais estão disponíveis em [`/docs`](docs):

* [`banco-de-dados.md`](docs/banco-de-dados.md) — estrutura, relacionamentos e decisões do modelo relacional.
* [`servicos.md`](docs/servicos.md) — responsabilidades e funcionamento dos principais serviços.
* [`analise-planilha-abastecimento.md`](docs/analise-planilha-abastecimento.md) — análise do processo operacional utilizado como referência para evolução do sistema.
* [`diagrama-banco.svg`](docs/diagrama-banco.svg) — diagrama do banco de dados.

---

## Contexto técnico do projeto

Além da implementação da aplicação, o desenvolvimento envolveu decisões relacionadas a:

* modelagem relacional;
* migração de armazenamento baseado em arquivos para PostgreSQL;
* processamento de documentos PDF;
* normalização de dados;
* implementação de regras de negócio;
* autenticação e autorização;
* dependency injection;
* Repository Pattern;
* Entity Framework Core migrations;
* configuração de ambientes;
* containerização;
* deploy de aplicação .NET;
* banco PostgreSQL em ambiente cloud.

O projeto continua em evolução conforme novas necessidades do fluxo real de farmácia hospitalar são identificadas.

---

