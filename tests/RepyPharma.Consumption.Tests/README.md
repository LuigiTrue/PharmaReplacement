# Importação de consumo

Execute `dotnet build` e `dotnet test` na raiz do repositório. Os testes de leitura e interface não consultam banco.

Para testar a persistência, defina `CONSUMPTION_TEST_CONNECTION` com uma conexão PostgreSQL de desenvolvimento que permita criar bancos. A suíte cria um banco exclusivo com nome aleatório, cria o esquema existente com EnsureCreatedAsync (sem executar migrations), testa a persistência e remove somente esse banco ao terminar. Nenhum teste limpa o banco indicado na conexão. Sem essa variável, os testes de persistência são explicitamente ignorados.

```bash
CONSUMPTION_TEST_CONNECTION='Host=127.0.0.1;Port=55439;Database=postgres;Username=postgres' dotnet test --no-build
```

As validações cobrem meses de 28/29/30/31 dias, ano bissexto, cobertura exata de 1 até hoje no mês atual em Brasília, lacunas por produto, dias extras, cabeçalhos e blocos variáveis, aspas, vírgulas, zeros, negativos, campos inválidos, precisão decimal e duplicidades. A persistência cobre atualização sem soma nem duplicação, extensão do mês atual, preço desconhecido ou previamente informado, produtos não cadastrados, unidades divergentes, concorrência e rollback integral após falha de gravação. A interface utiliza a competência do card e mantém atualização disponível.

Não há modo mock, contadores de referência, competência digitada manualmente nem listagem dos consumos no console. O código de teste permanece exclusivamente nesta suíte de regressão.

A integração da aba de estoque de DataUpdates reutiliza `InputFileComponent` e os serviços PDF existentes. Os testes de estoque verificam a indicação de PDF na interface, a rejeição de documentos inválidos, a gravação de itens/lotes/locais/saldos e a ausência de callback de sucesso quando a transação falha. Um PDF mínimo é gerado somente como fixture de teste; o parser e a persistência exercitados são os reais.


A importação conjunta ignora preços zero, copia o único preço positivo ou calcula Valor Total / Qtd Moviment. somente quando há preços positivos distintos. Os testes verificam os sinais de devoluções, exclusão das linhas de preço zero, campos condicionais de ponderação e exceções quando não existe custo válido. Não há comparação entre as quantidades dos relatórios. As médias são verificadas na prévia e no banco com três casas, inclusive arredondamento de valores negativos.

Para validar arquivos reais sem gravá-los, defina também CONSUMPTION_DAILY_REFERENCE e CONSUMPTION_SECTOR_REFERENCE. O teste monta somente um catálogo temporário a partir do diário e confere o preço único ou ponderado e os erros de processamento, sem confirmar os documentos reais.
