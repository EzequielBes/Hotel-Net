# Reserva Web — Design

## Contexto

Hoje o sistema só suporta check-in/check-out presencial (`Hospitality`): um funcionário
autenticado registra a chegada/saída de um hóspede num quarto específico já sabendo o
número do quarto (`CheckInUseCase`/`CheckOutUseCase`, `HospitalityController`).

A "Reserva Web" é um fluxo novo e independente: um cliente final (sem necessariamente
estar logado como funcionário) navega quartos disponíveis como um catálogo de e-commerce,
escolhe um plano de tarifa e período de estadia, e faz um pedido. O pedido precisa ser
processado de forma consistente (nunca alugar o mesmo quarto duas vezes no mesmo período)
e de forma idempotente (reenvio da mesma requisição nunca duplica o pedido).

Este design cobre apenas a Reserva Web. O fluxo presencial de Hospitality não é alterado.

## Modelo de domínio

### `RoomCategory` (nova entidade)
Agrupa quartos do mesmo tipo/tamanho para fins de precificação e disponibilidade.

- `Id`
- `Name` (ex: "Standard", "Suíte")
- `MaxCapacity` (máximo de pessoas)
- `MinStayDays` / `MaxStayDays` (estadia mínima/máxima em diárias)

`Room` ganha `RoomCategoryId` (FK). Um quarto pertence a exatamente uma categoria.

### `RatePlan` (nova entidade)
Perfil de horário/preço cadastrado por funcionário para uma categoria. Ex: categoria
"Standard" pode ter plano "12h" a R$Y/diária e plano "8h" a R$Z/diária.

- `Id`
- `RoomCategoryId` (FK)
- `Name` (ex: "12h", "8h")
- `HoursPackage` (int — informativo, quantas horas o pacote cobre por diária)
- `PricePerDay` (decimal)

Preço total do pedido = `PricePerDay × número de diárias` (diárias = dias entre
`CheckInDate` e `CheckOutDate`, validado contra `MinStayDays`/`MaxStayDays` da categoria).

### `BookingOrder` (nova entidade)
Representa o pedido de reserva web, do estado inicial até confirmado/rejeitado.

- `Id`
- `IdempotencyKey` (string, **unique index** no banco — garante idempotência real mesmo
  sob concorrência, não apenas dedupe em memória)
- `Cpf`
- `GuestName`
- `GuestCount`
- `RoomCategoryId` (FK — escolhido pelo cliente)
- `RatePlanId` (FK — escolhido pelo cliente)
- `RoomId` (nullable — atribuído pelo consumer no processamento, não pelo cliente)
- `CheckInDate` / `CheckOutDate` (datas, sem horário — diárias)
- `Status` (enum: `Pending`, `Confirmed`, `Rejected`)
- `TotalPrice`
- `CreatedAt`

## Fluxo

1. **`GET /api/rooms/available?checkIn=&checkOut=&guestCount=`**
   Sem autenticação. Lista `RoomCategory` que têm ao menos 1 `Room` sem overlap de
   `BookingOrder` Confirmed no período informado, com `MaxCapacity >= guestCount`.
   Retorna, por categoria: capacidade máxima, `MinStayDays`/`MaxStayDays`, e lista de
   `RatePlan` (nome, horas do pacote, preço/diária).

2. **`POST /api/bookings`**
   Sem autenticação. Header `Idempotency-Key` obrigatório.
   - Valida `GuestCount <= RoomCategory.MaxCapacity`.
   - Valida diárias dentro de `MinStayDays`/`MaxStayDays`.
   - Insere `BookingOrder` com `Status=Pending` e o `IdempotencyKey` recebido.
     - Se já existe uma linha com essa `IdempotencyKey` (unique constraint), **não cria
       nova** — retorna o pedido já existente (200), sem publicar mensagem de novo.
   - Publica mensagem `ProcessBookingMessage { BookingOrderId }` no RabbitMQ via
     MassTransit, com partition key = `RoomCategoryId` (garante que pedidos da mesma
     categoria nunca processam em paralelo entre si).
   - Retorna 202 com o `BookingOrder` (Status=Pending) e seu Id para consulta.

3. **Consumer (`ProcessBookingConsumer`)**
   `IConsumer<ProcessBookingMessage>` do MassTransit, particionado por `RoomCategoryId`
   (`UsePartitioner`, concorrência 1 por partição). Dentro de uma transação:
   - Busca `BookingOrder` pelo Id (ainda Pending).
   - Busca o primeiro `Room` da categoria sem overlap de datas com nenhum outro
     `BookingOrder` Confirmed (`CheckInDate < outro.CheckOutDate && CheckOutDate > outro.CheckIn`).
   - Se achou: atribui `RoomId`, calcula `TotalPrice`, marca `Confirmed`.
   - Se não achou (concorrência levou o último quarto): marca `Rejected`.
   - Dispara webhook de notificação com o resultado.

4. **Webhook**
   `IWebhookSender` (Domain/Ports) implementado por `HttpWebhookSender` (Infrastructure).
   POST para URL configurável (`appsettings.json:Webhooks:BookingConfirmed`) com payload
   `{ BookingOrderId, Status, RoomNumber?, TotalPrice, Cpf }`. Falha no webhook é logada
   mas não desfaz a confirmação da reserva (webhook é notificação, não fonte de verdade).

5. **`GET /api/bookings/{id}`**
   Sem autenticação (cliente consulta status do próprio pedido pelo Id retornado no
   passo 2). Retorna `BookingOrder` atual (Pending/Confirmed/Rejected).

## Infraestrutura

- **RabbitMQ real via Docker**: `docker-compose.yml` na raiz do projeto sobe
  `rabbitmq:3-management` para desenvolvimento local.
- **MassTransit** (`MassTransit` + `MassTransit.RabbitMQ`) configurado em `Program.cs`,
  usando a mesma composition root já existente (Program.cs é o único lugar que conhece
  Infrastructure).
- **Persistência**: novos `DbSet<RoomCategory>`, `DbSet<RatePlan>`, `DbSet<BookingOrder>`
  em `AppDbContext`. Unique index em `BookingOrder.IdempotencyKey` via Fluent API. Nova
  migration EF Core.
- **Novos repositórios** (Domain/Ports + Infrastructure/Persistence/Repositories),
  seguindo o padrão atual (`IRoomCategoryRepository`, `IRatePlanRepository`,
  `IBookingOrderRepository`), registrados em `Program.cs`.

## Estrutura de código (Clean Architecture, mesmo padrão do projeto)

```
Domain/
  Entities/RoomCategory.cs, RatePlan.cs, BookingOrder.cs
  Enums/BookingStatus.cs
  Ports/IRoomCategoryRepository.cs, IRatePlanRepository.cs, IBookingOrderRepository.cs, IWebhookSender.cs
Application/
  UseCases/Booking/
    IListAvailableRoomsUseCase.cs / ListAvailableRoomsUseCase.cs
    ICreateBookingUseCase.cs / CreateBookingUseCase.cs
    IGetBookingUseCase.cs / GetBookingUseCase.cs
    BookingCommands.cs (records: CreateBookingCommand, etc.)
    ProcessBookingMessage.cs (contrato da mensagem MassTransit)
  DTOs/BookingDtos.cs
Infrastructure/
  Persistence/Repositories/RoomCategoryRepository.cs, RatePlanRepository.cs, BookingOrderRepository.cs
  Messaging/ProcessBookingConsumer.cs
  Webhooks/HttpWebhookSender.cs
Api/
  Controllers/BookingController.cs (rooms/available, bookings, bookings/{id})
docker-compose.yml (RabbitMQ)
```

Segue as convenções já existentes: interface `IXUseCase` com método `Execute(Command)`,
records para commands/DTOs, `Program.cs` como composition root único, testes xUnit com
Moq + FluentAssertions em `CheckInApp.Tests/`.

## Testes

- `CreateBookingUseCaseTests`: idempotência (mesma key não duplica), validação de
  capacidade, validação de min/max diárias, publicação de mensagem.
- `ProcessBookingConsumerTests` (ou use case equivalente chamado pelo consumer):
  atribuição de quarto livre, rejeição quando não há quarto livre (overlap), cálculo de
  preço por diárias × RatePlan.
- `ListAvailableRoomsUseCaseTests`: filtra corretamente por capacidade e overlap de datas.

## Fora de escopo

- Pagamento real (webhook é apenas notificação de saída, não gateway de entrada).
- Cancelamento/alteração de reserva web já confirmada.
- Autenticação de cliente final (endpoints de reserva web são públicos).
