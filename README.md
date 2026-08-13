# Korp - Teste Técnico - André Ferreira

## Sobre o projeto

Sistema de emissão de notas fiscais desenvolvido para o desafio técnico da KORP. A aplicação permite cadastrar produtos e seus saldos, criar notas com múltiplos itens, fechar e imprimir notas, atualizar o estoque e apresentar feedback quando a comunicação entre os microsserviços está indisponível.

## Arquitetura

```text
                         Angular
                        /       \
                       v         v
               StockService   BillingService
                    ^              |
                    |              |
                    +-----HTTP-----+

                    |              |
                    v              v
                stock.db       billing.db
```

- **StockService** é responsável pelos produtos, saldos e dedução transacional do estoque.
- **BillingService** é responsável pelas notas fiscais, itens preservados e estados `Open`/`Closed`.
- O frontend consulta os dois serviços; o BillingService consulta e solicita deduções ao StockService por HTTP.
- Cada serviço possui seu próprio banco SQLite e não compartilha persistência com o outro.

## Tecnologias utilizadas

### Backend

- C# e ASP.NET Core Web API sobre .NET 10
- Entity Framework Core 10 e SQLite
- LINQ
- `HttpClient` para comunicação BillingService → StockService

### Frontend

- Angular 21 e TypeScript
- Reactive Forms e `FormArray`
- `HttpClient` e RxJS
- CSS próprio, sem biblioteca visual ou de componentes

## Como executar

### Pré-requisitos

- .NET SDK 10.0.x
- Node.js compatível com Angular 21 — validado com Node.js 24.12.0
- npm 11.x — o projeto fixa o gerenciador em npm 11.6.2

### Restaurar o backend

Na raiz do repositório:

```bash
dotnet restore KorpTeste.sln
```

O manifesto local fixa `dotnet-ef` 10.0.10. Ele não é necessário para iniciar a aplicação, mas pode ser restaurado quando for preciso operar as migrations:

```bash
dotnet tool restore
```

### Iniciar os serviços

Abra dois terminais na raiz do repositório.

Terminal 1 — estoque:

```bash
dotnet run --project backend/StockService/StockService.csproj
```

Terminal 2 — faturamento:

```bash
dotnet run --project backend/BillingService/BillingService.csproj
```

Endereços de desenvolvimento:

- StockService: `http://localhost:5101`
- BillingService: `http://localhost:5102`

### Iniciar o frontend

Em um terceiro terminal:

```bash
cd frontend
npm ci
npm start
```

Acesse `http://localhost:4200`.

## Banco de dados

Cada serviço mantém seu próprio banco SQLite em `App_Data`. Os arquivos `.db` são criados localmente e não são versionados. As migrations do EF Core são versionadas e aplicadas automaticamente quando cada serviço inicia, portanto não é necessário criar ou popular os bancos manualmente.

## Funcionalidades

- Cadastro de produtos com código, descrição e saldo.
- Validação de código único e saldo não negativo.
- Criação de notas fiscais com múltiplos produtos e quantidades.
- Numeração sequencial e estados exibidos como **Aberta** e **Fechada**.
- Preservação do código e da descrição dos produtos nos itens da nota.
- Fechamento antes da impressão, com dedução do estoque em uma transação local.
- Proteção contra novo processamento de uma nota já fechada.
- Feedback em português para validações, conflitos e indisponibilidade do estoque.

## Decisões técnicas

### Angular

A aplicação usa componentes standalone e rotas para Produtos, Notas fiscais e Detalhe da nota. Os formulários são reativos; a nota utiliza `FormArray` para adicionar ou remover itens e um validador para impedir produtos duplicados.

O hook `ngOnInit` é usado somente onde necessário para carregar a lista de produtos, os dados de emissão/listagem de notas e o detalhe de uma nota. Como Angular 21 opera em modo zoneless, as conclusões assíncronas notificam a renderização com `ChangeDetectorRef.markForCheck()`.

### RxJS

As chamadas do `HttpClient` retornam `Observable`. O operador `finalize` limpa os estados de carregamento e processamento tanto em sucesso quanto em erro. `forkJoin` carrega produtos e notas em conjunto na tela de emissão.

### C# e LINQ

Os dois microsserviços usam controllers, serviços de aplicação, DTOs, injeção de dependência e EF Core. O cliente HTTP tipado do BillingService encapsula a comunicação com o estoque.

LINQ é usado em consultas e regras de negócio com operações presentes no código, incluindo `Where`, `Select`, `OrderBy`, `OrderByDescending`, `Any`, `GroupBy`, `Contains` e `AsNoTracking`. A dedução valida todos os itens antes de alterar os saldos e confirma as alterações dentro de uma transação SQLite local.

### Tratamento de erros

As APIs retornam ASP.NET Core Problem Details com status HTTP, descrição segura e um código estável. Entre os casos tratados estão dados inválidos, produto ou nota inexistente, código de produto duplicado, estoque insuficiente, nota já fechada e StockService indisponível. Stack traces, detalhes de banco e exceções internas não são enviados ao Angular.

## Demonstração de falha e recuperação

1. Inicie as três aplicações e crie um produto e uma nota Aberta.
2. Interrompa fisicamente o StockService.
3. Na nota, selecione **Imprimir nota fiscal**.
4. O BillingService informa indisponibilidade, o Angular apresenta o erro e a nota permanece Aberta; a impressão não é chamada.
5. Reinicie o StockService e tente novamente.
6. O estoque é deduzido, a nota passa para Fechada e a impressão é aberta.

Não existe simulador de falha nem repetição automática da dedução. O cenário é demonstrado pela parada real do serviço e pela nova tentativa feita pelo usuário.

## Limitação arquitetural conhecida

Não há transação distribuída entre os bancos independentes. Existe uma janela estreita de confirmação parcial caso o estoque seja persistido e o BillingService falhe antes de gravar o estado `Closed`. Transação distribuída e infraestrutura de idempotência não foram adicionadas por estarem fora do escopo solicitado.
