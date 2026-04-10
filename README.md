# Finzlab Assessment

The service exposes a single POST /webhooks/transactions endpoint built with .NET 10 Web API controllers and Entity Framework Core on PostgreSQL. When a payload arrives, the TransactionsController first checks whether a row with the same ExternalId already exists, if so it returns 200 Already processed without touching the database again, making every retry safe. Otherwise it persists the raw transaction and immediately computes two derived values: the amount converted to USD using a fixed in-process FX table, and a risk classification (HIGH for amounts over $10,000 USD, LOW otherwise). Both the raw record and the derived TransactionSummary are saved in a single SaveChangesAsync call, keeping the writes atomic, no partial state is ever stored. The response returns the essential fields including the derived data, so callers get structured output immediately. A UNIQUE constraint on ExternalId acts as a second-layer idempotency guard at the database level, catching any race condition that slips past the application check.
## Features

- **Webhook Ingestion**: Accepts transaction payloads via `/webhooks/transactions` endpoint
- **Idempotency**: Prevents duplicate processing using external transaction IDs
- **Currency Conversion**: Converts transaction amounts to USD using predefined exchange rates
- **Risk Classification**: Automatically classifies transactions as HIGH or LOW risk based on USD amount (>10,000 USD = HIGH)
- **PostgreSQL Storage**: Persists transactions and derived summaries using Entity Framework Core
- **OpenAPI/Swagger**: Interactive API documentation available at `/swagger`
- **Comprehensive Testing**: Unit tests with xUnit and in-memory database

## Architecture

The application follows a clean architecture with the following components:

- **Controllers**: Handle HTTP requests and responses
- **Entities**: Domain models (Transaction, TransactionSummary)
- **Models**: Request/response DTOs (TransactionPayload)
- **Persistence**: Database context and configuration using Entity Framework Core

### Data Flow

1. Webhook receives transaction payload
2. Check for existing transaction by external ID (idempotency)
3. Convert amount to USD and classify risk
4. Persist transaction and summary to database
5. Return created response with derived fields

## Decisions

1. **App check + DB UNIQUE**  
   Application reads before writing (avoids duplicate work), backed by a database UNIQUE constraint on ExternalId as a hard safety net for races.

2. **Derived in a sibling table**  
   Keeps raw ingested data separate from computed data. Easy to re-derive (drop summaries and recompute), clear ownership, atomic with the parent via single SaveChangesAsync.

### Rejected Alternative
**Outbox / background queue for derivation**  
Computing the summary asynchronously (enqueue a job, process later) adds durability for slow derivations. Rejected here because the derivation is instantaneous arithmetic — adding a queue would be over-engineering for this scope, violating the "keep it minimal" constraint.

### Failure Scenario
Two requests with the same externalId arrive simultaneously. Both pass the application AnyAsync check (neither has been inserted yet). Both proceed to SaveChangesAsync — one succeeds, the other hits the PostgreSQL UNIQUE constraint and throws a DbUpdateException. Mitigation: catch DbUpdateException with a uniqueness violation code and return 200 (already processed) instead of 500. The DB constraint is the true last line of defence.

## Prerequisites

- .NET 10.0 SDK
- PostgreSQL database
- (Optional) Visual Studio 2022 or VS Code with C# extensions

## Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd FinzlabAssesment
   ```

2. **Database Configuration**
   
   Create a PostgreSQL database and update the connection string. You can set it via:
   
   - Environment variable: `ConnectionStrings__Default`
   - User secrets (recommended for development):
     ```bash
     dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=finzlab;Username=youruser;Password=yourpassword"
     ```
   - Or add to `appsettings.Development.json`:
     ```json
     {
       "ConnectionStrings": {
         "Default": "Host=localhost;Database=finzlab;Username=youruser;Password=yourpassword"
       }
     }
     ```

3. **Install Dependencies**
   ```bash
   dotnet restore
   ```

4. **Database Migration**
   ```bash
   dotnet ef database update
   ```

## Running the Application

1. **Development Mode**
   ```bash
   dotnet run --project FinzlabAssesment
   ```

2. **Access the API**
   - Swagger UI: https://localhost:5001/swagger
   - API Base URL: https://localhost:5001

## API Documentation

### POST /webhooks/transactions

Ingests a new transaction.

**Request Body:**
```json
{
  "externalId": "string",
  "amount": 0.0,
  "currency": "string",
  "status": "string",
  "occurredAt": "2024-06-01T10:00:00Z"
}
```

**Supported Currencies:** USD, EUR, GBP, NGN

**Response (201 Created):**
```json
{
  "id": "guid",
  "externalId": "string",
  "currency": "string",
  "amount": 0.0,
  "amountUsd": 0.0,
  "riskLevel": "HIGH" | "LOW"
}
```

**Response (200 OK - Idempotent):** For duplicate externalId
```json
{
  "message": "Already processed",
  "externalId": "string"
}
```

## Testing

Run the test suite:

```bash
dotnet test
```

The tests include:
- Successful transaction ingestion with derived fields
- Idempotency handling for duplicate transactions
- Integration tests using WebApplicationFactory with in-memory database

## Currency Conversion Rates

Hardcoded rates (as of implementation):
- USD: 1.0
- EUR: 1.08
- GBP: 1.27
- NGN: 0.00065

## Risk Classification Rules

- **LOW**: Amount USD ≤ 10,000
- **HIGH**: Amount USD > 10,000

## Project Structure

```
FinzlabAssesment/
├── Controllers/
│   ├── TransactionsController.cs
│   └── WeatherForecastController.cs (sample)
├── Entities/
│   └── Transaction.cs
├── Models/
│   └── TransactionRequestModel.cs
├── Persistence/
│   └── AppDbContext.cs
├── Program.cs
├── appsettings.json
└── FinzlabAssesment.csproj

FinzlabAssesmentTests/
├── UnitTest1.cs
└── FinzlabAssesmentTests.csproj
```

## Technologies Used

- **Framework**: ASP.NET Core 10.0
- **Database**: PostgreSQL with Npgsql provider
- **ORM**: Entity Framework Core 10.0
- **API Documentation**: Swashbuckle/Swagger
- **Testing**: xUnit, Microsoft.AspNetCore.Mvc.Testing
- **Serialization**: System.Text.Json

## Development Notes

- Uses nullable reference types
- Implicit usings enabled
- OpenAPI specification generation
- In-memory database for testing
- Unique constraint on ExternalId for idempotency enforcement</content>
