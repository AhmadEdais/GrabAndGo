# GrabAndGo — Cashierless Smart Retail System

> A cashierless smart-retail platform where customers enter a store, pick up or return products tracked by a vision system, receive live cart updates, and complete checkout without a traditional cashier.

---

## Overview

GrabAndGo is an event-driven smart-retail system built with ASP.NET Core, SQL Server, MQTT, SignalR, and a separate Vanilla JavaScript frontend.

The system coordinates four main areas:

- customer authentication and wallet management
- QR-based store entry and checkout
- vision-system events delivered through MQTT
- real-time cart and invoice updates delivered through SignalR

The current implementation includes a browser-based demonstration frontend and a demo-control workflow used to validate the complete backend while the dedicated production computer-vision system is unavailable.

---

## The Idea

GrabAndGo removes the traditional checkout line.

A customer enters a store through a QR-based gate flow and receives an active shopping session with an empty digital cart.

Cameras, edge devices, or the demonstration controller publish product events through MQTT whenever an item is picked up or returned.

The ASP.NET Core backend:

1. receives the MQTT event
2. identifies the active shopping session
3. maps the AI label to a product
4. updates the cart through SQL Server stored procedures
5. broadcasts the updated cart through SignalR

At checkout, the system:

1. calculates the final cart total
2. validates the customer's wallet balance
3. deducts funds atomically
4. creates the transaction and transaction items
5. completes the shopping session
6. creates an invoice record
7. generates the PDF asynchronously

Cart changes are pushed to the frontend in real time. The frontend does not poll the backend for cart updates.

---

## My Role

I served as the backend engineer and system architect for this project.

My work included:

- designing the layered .NET 8 architecture
- modeling the SQL Server database
- designing stored-procedure and JSON contracts
- implementing JWT authentication for customers
- implementing API-key authentication for gate and vision devices
- building the MQTT ingestion pipeline
- building SignalR cart, gate, and invoice notifications
- implementing wallet, transaction, checkout, and invoice workflows
- creating the QR-token and store-entry flow
- building asynchronous invoice generation using QuestPDF
- implementing the Vanilla JavaScript demonstration frontend
- creating a demo controller to replace the unavailable production vision system during testing
- preparing reproducible database deployment scripts
- separating Development and Production configuration
- validating the complete application against a newly recreated database

---

## System Architecture

```text
┌──────────────────────────────────────────────────────────────────────┐
│                    Vanilla JavaScript Frontend                       │
│                                                                      │
│        HTML · CSS · REST API · SignalR · QR · Wallet · Cart         │
└──────────────────────────────┬───────────────────────────────────────┘
                               │ HTTP/HTTPS + SignalR
                               ▼
┌──────────────────────────────────────────────────────────────────────┐
│                     ASP.NET Core 8 Web API                           │
│                                                                      │
│  ┌─────────────────┐  ┌──────────────────┐  ┌────────────────────┐  │
│  │ REST Controllers│  │   SignalR Hubs   │  │ Background Workers │  │
│  │                 │  │                  │  │                    │  │
│  │ Users           │  │ CartHub          │  │ MqttVisionWorker   │  │
│  │ Wallets         │  │ GateHub          │  │ InvoiceWorker      │  │
│  │ Sessions        │  │ InvoiceHub       │  │                    │  │
│  │ Gate            │  └──────────────────┘  └────────────────────┘  │
│  │ Vision System   │                                                 │
│  │ Products        │                                                 │
│  │ Transactions    │                                                 │
│  │ Invoices        │                                                 │
│  └─────────────────┘                                                 │
│                                                                      │
│                       Business Services                              │
│                              │                                       │
│                       Repository Layer                               │
│                    Raw ADO.NET + SqlExecutor                         │
└──────────────────────────────┬───────────────────────────────────────┘
                               │ Stored procedures + JSON contracts
                               ▼
┌──────────────────────────────────────────────────────────────────────┐
│                            SQL Server                                │
│                                                                      │
│             24 application tables · 29 stored procedures            │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│                         IoT / Demo Layer                             │
│                                                                      │
│   Vision Cameras or Demo Controller                                 │
│                  │                                                   │
│                  │ MQTT/TLS                                         │
│                  ▼                                                   │
│             HiveMQ Cloud                                             │
│                  │                                                   │
│                  ▼                                                   │
│          MqttVisionWorker                                            │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| Backend | ASP.NET Core Web API |
| Language | C# |
| Database | SQL Server |
| Database Access | Raw ADO.NET |
| Data Access Pattern | Repository pattern |
| ORM | None |
| Real-Time Communication | ASP.NET Core SignalR |
| IoT Messaging | MQTT using MQTTnet |
| MQTT Broker | HiveMQ Cloud |
| Customer Authentication | JWT Bearer |
| Hardware Authentication | API keys |
| Password Hashing | BCrypt.Net-Next |
| PDF Generation | QuestPDF |
| API Documentation | Swagger / OpenAPI |
| Frontend | Vanilla JavaScript, HTML and CSS |
| Planned Database Hosting | Azure SQL Database |
| Planned Backend Hosting | Azure container hosting |
| Planned Container Registry | Azure Container Registry |

---

## Project Structure

```text
GrabAndGo/
│
├── GrabAndGo/                     # ASP.NET Core API entry project
│   ├── Controllers/               # REST API controllers
│   ├── Hubs/                      # SignalR hubs
│   ├── BackgroundServices/        # MQTT and invoice workers
│   ├── Security/                  # Hardware API-key authentication
│   ├── Program.cs                 # Dependency injection and middleware
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── appsettings.Production.json
│
├── GrabAndGo.Services/            # Business logic
│   ├── Interfaces/
│   └── Implementations/
│
├── GrabAndGo.DataAccess/          # Stored-procedure repositories
│   ├── Interfaces/
│   ├── Repositories/
│   └── Core/
│       └── SqlExecutor.cs
│
├── GrabAndGo.Models/              # Request and response DTOs
│
├── database/
│   ├── 001_schema.sql
│   ├── 002_seed_reference_data.sql
│   ├── 003_stored_procedures.sql
│   └── 004_seed_demo_data.sql
│
├── GrabAndGo.sln
└── README.md
```

The main dependency direction is:

```text
API
 ↓
Services
 ↓
DataAccess
 ↓
Models
```

---

## Database Reproducibility

The database is versioned through four deployment scripts.

They must be executed in this order:

```text
database/001_schema.sql
database/002_seed_reference_data.sql
database/003_stored_procedures.sql
database/004_seed_demo_data.sql
```

### `001_schema.sql`

Creates the database structure, including:

- 24 application tables
- primary keys
- foreign keys
- constraints
- indexes
- identity columns

### `002_seed_reference_data.sql`

Seeds required lookup and status rows, including:

- session statuses
- payment statuses
- processing statuses
- wallet-ledger entry types

### `003_stored_procedures.sql`

Creates the 29 application stored procedures.

SQL Server database-diagram helper procedures are intentionally excluded because they are SSMS tooling and are not part of the GrabAndGo application.

### `004_seed_demo_data.sql`

Seeds the operational data required by the demonstration flow:

- one active store
- three store zones
- four camera records
- 11 active products
- product AI labels
- product-zone mapping

The seed scripts are designed to be safely rerunnable.

### Validation

The complete application was tested against a new empty database recreated using only these four scripts.

The following workflows passed:

- user registration
- login
- automatic wallet creation
- wallet top-up
- wallet-ledger retrieval
- store retrieval
- gate-token generation
- session creation
- cart creation
- track binding
- MQTT vision events
- SignalR cart updates
- checkout
- wallet deduction
- transaction creation
- transaction-item creation
- session completion
- invoice creation
- PDF invoice generation

---

## Database Access Design

The application does not use Entity Framework or another ORM.

All database operations pass through `SqlExecutor` and stored procedures.

The main patterns are:

```csharp
ExecuteReaderAsync<T>()
ExecuteNonQueryAsync<T>()
ExecuteScalarAsync<T>()
```

Stored procedures commonly exchange data through JSON.

### Stored-procedure conventions

- lists use `FOR JSON PATH`
- single objects use `FOR JSON PATH, WITHOUT_ARRAY_WRAPPER`
- nullable response properties use `INCLUDE_NULL_VALUES`
- writes commonly accept `@P_JSON_REQUEST`
- JSON input is parsed with `OPENJSON`
- multi-step workflows use explicit SQL transactions
- wallet deductions use locking to prevent concurrent double-spending

Important workflows such as registration, store entry, and checkout are handled atomically inside SQL transactions.

---

## MQTT and Real-Time Cart Pipeline

The core shopping pipeline is:

```text
Customer picks up or returns an item
                │
                ▼
Vision system or demo controller detects the action
                │
                ▼
MQTT event published to HiveMQ Cloud
                │
                ▼
MqttVisionWorker receives the message
                │
                ▼
CartService processes the event
                │
                ▼
Stored procedure updates the cart
                │
                ▼
SignalR broadcasts the updated cart
                │
                ▼
Vanilla JavaScript frontend updates immediately
```

A representative event contains values such as:

```json
{
  "trackId": "track-1001",
  "aiLabel": "Kewpie_Mayonnaise",
  "action": "Pick",
  "confidence": 0.97,
  "cameraCode": "CAM_DEMO"
}
```

The worker:

1. deserializes the message
2. creates a dependency-injection scope
3. resolves the active track binding
4. maps the AI label to a product
5. updates the cart
6. broadcasts the resulting cart state
7. logs malformed or rejected events
8. reconnects when the MQTT connection is interrupted

---

## SignalR

The backend includes multiple SignalR hubs:

```text
/hubs/cart
/hubs/gate
/hubs/invoice
```

### Cart updates

A customer subscribes to a group associated with their shopping session.

```text
Session_{sessionId}
```

Cart changes are broadcast only to connections associated with that session.

### Authentication

The frontend passes its JWT to SignalR during connection setup.

The backend extracts the token from the `access_token` query parameter for hub requests and passes it through the normal JWT validation pipeline.

---

## Authentication

The system uses separate authentication mechanisms for customers and hardware.

### Customer Authentication

Customers authenticate using JWT Bearer tokens.

After login, the backend issues an HS256-signed JWT containing claims such as:

- user ID
- email
- first name
- last name
- token ID

The signing key is read using **Option A**:

```text
GRABANDGO_JWT_KEY
```

This name matches the current implementation in `Program.cs`.

Example in PowerShell:

```powershell
$env:GRABANDGO_JWT_KEY = "use-a-cryptographically-random-secret-at-least-32-characters"
```

Example in Git Bash:

```bash
export GRABANDGO_JWT_KEY='use-a-cryptographically-random-secret-at-least-32-characters'
```

The JWT key must not be committed to Git.

### Hardware Authentication

Gate and vision endpoints use API keys supplied through:

```text
X-Api-Key
```

The configured keys are:

```text
HardwareAuth:GateApiKey
HardwareAuth:VisionApiKey
```

Environment-variable equivalents are:

```text
HardwareAuth__GateApiKey
HardwareAuth__VisionApiKey
```

Hardware API-key comparisons use fixed-time comparison to reduce timing-based attacks.

---

## QR and Store-Entry Flow

The current demonstration flow is:

```text
1. Gate or demo controller requests a gate token
2. Backend generates a short-lived token
3. Backend creates a frontend URL containing the token
4. Gate displays the URL as a QR code
5. Customer scans the QR using the browser frontend
6. Authenticated frontend submits the gate token
7. Stored procedure consumes the token atomically
8. Shopping Session and empty Cart are created
```

The QR content follows this general format:

```text
{FrontendBaseUrl}/store-detail.html?gateToken={encodedToken}
```

The gate token is:

- short-lived
- hashed before database storage
- single use
- marked as consumed when accepted

The store-entry stored procedure also prevents a customer from creating multiple active shopping sessions.

The repository also contains hardware-oriented gate endpoints used to model the physical gate workflow.

---

## Checkout Flow

At the exit, the system:

1. resolves the active session
2. calculates the cart total
3. checks the wallet balance
4. deducts the required amount
5. creates a transaction
6. creates transaction items
7. creates wallet-ledger entries
8. ends the session
9. creates an invoice stub
10. sends a gate-status update
11. allows the invoice worker to generate the PDF asynchronously

Wallet changes and transaction creation are protected by database transactions.

---

## Background Services

### `MqttVisionWorker`

`MqttVisionWorker` is a long-running hosted service.

It:

- maintains a connection to HiveMQ Cloud
- subscribes to vision-event topics
- receives product pick and return events
- creates a dependency-injection scope per message
- calls the cart service
- sends SignalR updates
- reconnects after connection loss

Because the worker runs inside the API process, deployment scaling requires care. Multiple API replicas could create multiple MQTT subscriptions and process the same logical workload more than once.

The initial deployment is therefore expected to use:

```text
minimum replicas: 1
maximum replicas: 1
```

until distributed worker coordination is introduced.

### `InvoiceWorker`

`InvoiceWorker` runs asynchronously after checkout.

It:

1. polls for pending invoice records
2. loads transaction and item information
3. generates an A4 PDF using QuestPDF
4. writes the file to the configured storage folder
5. updates the invoice database row with the generated path

The polling interval and batch size are configurable.

---

## Invoice Storage

Development uses a local invoice directory.

Production currently uses:

```text
/app/invoices
```

This path is designed for a Linux container.

Container-local storage is normally temporary, so a deployed version should use one of these approaches:

- an Azure-mounted persistent volume for the initial demonstration
- Azure Blob Storage for a stronger production architecture

Generated invoices should not be committed to Git.

---

## REST API Surface

Representative endpoints include:

| Method | Endpoint | Authentication | Purpose |
|---|---|---|---|
| `POST` | `/api/users/register` | Public | Register a customer |
| `POST` | `/api/users/login` | Public | Authenticate and receive a JWT |
| `GET` | `/api/users/{id}` | JWT | Retrieve customer profile |
| `GET` | `/api/products` | JWT | Retrieve active products |
| `POST` | `/api/wallets/top-up` | JWT | Add funds to a wallet |
| `GET` | `/api/wallets/balance` | JWT | Retrieve wallet balance |
| `GET` | `/api/wallets/ledger` | JWT | Retrieve wallet history |
| `GET` | `/api/transactions` | JWT | Retrieve purchase history |
| `POST` | `/api/gate/generate-qr` | Gate API key | Generate a gate QR token |
| `POST` | `/api/gate/scan` | Gate API key | Process a hardware gate scan |
| `POST` | `/api/gate/checkout` | Gate API key | Process exit and checkout |
| `POST` | `/api/vision-system/bind-track` | Vision API key | Bind a vision track to a session |
| `GET` | `/api/invoices` | JWT | Retrieve invoice list |
| `GET` | `/api/invoices/{id}` | JWT | Retrieve invoice status |
| `GET` | `/api/invoices/{id}/pdf` | JWT | Download an invoice PDF |
| `GET` | `/health` | Public | Application health check |

Refer to Swagger or the controller source code for the complete current endpoint list and request contracts.

---

## Configuration

Configuration is separated by environment.

### `appsettings.json`

Contains shared, non-secret defaults:

- logging
- JWT issuer and audience
- empty connection-string placeholder
- invoice defaults
- empty hardware API-key placeholders
- vision-system URL
- MQTT host and port
- empty MQTT credential placeholders

### `appsettings.Development.json`

Contains local machine-specific settings:

- local frontend URL
- HTTPS endpoint on port `7001`
- Windows certificate path

Example development endpoint:

```text
https://0.0.0.0:7001
```

### `appsettings.Production.json`

Contains container-friendly settings:

- HTTP endpoint on port `8080`
- Linux invoice directory

Example production endpoint:

```text
http://0.0.0.0:8080
```

Public HTTPS is expected to be terminated by the cloud hosting platform before requests are forwarded to the container.

---

## Environment Variables

| Variable | Purpose | Secret |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server or Azure SQL connection string | Yes |
| `GRABANDGO_JWT_KEY` | JWT signing key | Yes |
| `Broker__Username` | HiveMQ Cloud username | Yes |
| `Broker__Password` | HiveMQ Cloud password | Yes |
| `HardwareAuth__GateApiKey` | Gate API key | Yes |
| `HardwareAuth__VisionApiKey` | Vision-system API key | Yes |
| `FrontendBaseUrl` | Frontend URL embedded in generated QR codes | No |
| `Invoice__StorageFolder` | Invoice output directory | No |
| `VisionSystem__BaseUrl` | Vision-system or demo webhook URL | Depends |

ASP.NET Core converts double underscores into nested configuration keys.

For example:

```text
Broker__Username
```

overrides:

```json
{
  "Broker": {
    "Username": ""
  }
}
```

The JWT key is an exception because the current code directly reads:

```text
GRABANDGO_JWT_KEY
```

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server
- SQL Server Management Studio or another SQL client
- a HiveMQ Cloud account or another MQTT broker
- the separate GrabAndGo frontend
- a local HTTPS certificate for the current Development setup

### Clone the repository

```bash
git clone https://github.com/AhmadEdais/GrabAndGo.git
cd GrabAndGo
```

### Create the database

Create an empty SQL Server database.

Then execute these files in order:

```text
database/001_schema.sql
database/002_seed_reference_data.sql
database/003_stored_procedures.sql
database/004_seed_demo_data.sql
```

### Configure secrets

Use .NET User Secrets, environment variables, or another local secret mechanism.

Required values include:

```text
ConnectionStrings__DefaultConnection
GRABANDGO_JWT_KEY
Broker__Username
Broker__Password
HardwareAuth__GateApiKey
HardwareAuth__VisionApiKey
FrontendBaseUrl
```

Do not place real secrets inside committed `appsettings` files.

---

## Running in Development

The Development configuration uses:

```text
HTTPS port 7001
```

Run:

```bash
dotnet run --project GrabAndGo/GrabAndGo.Api.csproj
```

The launch profile normally selects the Development environment.

The configured development frontend origin must match the address used by the browser frontend.

---

## Running in Production Mode Locally

Set the required production environment variables first.

Then run:

```bash
ASPNETCORE_ENVIRONMENT=Production \
dotnet run --no-launch-profile --project GrabAndGo/GrabAndGo.Api.csproj
```

Expected output:

```text
Hosting environment: Production
Now listening on: http://0.0.0.0:8080
```

The `--no-launch-profile` option is important because the launch profile may force the environment back to Development.

### Health check

Open:

```text
http://localhost:8080/health
```

Expected response:

```json
{
  "status": "healthy",
  "environment": "Production"
}
```

The health endpoint confirms that the process is running. It does not by itself verify database, MQTT, or other external dependencies.

---

## Production-Mode Validation

The backend has been tested locally using Production configuration.

Validated behavior includes:

- Production environment loading
- HTTP port `8080`
- environment-based database configuration
- HiveMQ Cloud connection
- JWT authentication
- login
- wallet loading
- wallet top-up
- receipts and ledger retrieval
- store retrieval
- session and cart creation
- track binding
- MQTT product events
- SignalR cart updates
- checkout
- wallet deduction
- transaction creation
- invoice record creation
- PDF generation
- LAN access to the `/health` endpoint

The physical phone QR scan was not used during the local Production HTTP test because browser camera APIs generally require HTTPS.

The gate-entry stored procedure was executed directly to reproduce the result of a valid scan. The remainder of the complete business flow was then tested successfully.

The final physical QR scan will be validated after both the frontend and backend are deployed over HTTPS.

---

## Frontend

The browser frontend is maintained separately and is built with:

- Vanilla JavaScript
- HTML
- CSS
- SignalR JavaScript client

It provides:

- registration and login
- wallet balance and top-up
- product browsing
- QR and gate workflow
- active cart display
- live cart updates
- receipts
- invoices

The frontend must be configured with the correct backend API and SignalR URLs for each environment.

---

## CORS

The backend currently contains local Development origins for:

- localhost
- `127.0.0.1`
- the local LAN frontend address

Before cloud deployment, the CORS policy must be updated to allow the deployed frontend origin.

`FrontendBaseUrl` is currently used to construct generated QR URLs. It should not be assumed to configure CORS automatically.

---

## Swagger

Swagger/OpenAPI is registered in the API project.

Before a public production deployment, Swagger should normally be restricted to Development or protected appropriately.

Example intended pattern:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

---

## Deployment Status

Completed:

- repository cleanup
- generated-file exclusions
- reproducible database scripts
- schema validation
- reference-data seeding
- stored-procedure validation
- demo-data seeding
- full fresh-database acceptance test
- Development and Production configuration separation
- Production-mode backend validation
- MQTT Production configuration validation
- health endpoint
- invoice generation validation

In progress:

- Docker fundamentals
- backend Dockerfile
- `.dockerignore`
- local Linux-container validation
- persistent invoice storage
- Azure SQL deployment
- Azure Container Registry
- Azure container hosting
- static frontend deployment
- public HTTPS QR validation
- GitHub Actions CI/CD

The repository does not yet claim a completed Docker or Azure deployment.

---

## Planned Deployment Architecture

```text
GitHub Repository
        │
        ▼
Docker Build
        │
        ▼
Azure Container Registry
        │
        ▼
Azure Container Hosting
        │
        ├── ASP.NET Core API
        ├── MQTT Worker
        └── Invoice Worker
        │
        ├──────────────► HiveMQ Cloud
        │
        ├──────────────► Azure SQL Database
        │
        └──────────────► Persistent invoice storage

Static Frontend Hosting
        │
        └──────────────► HTTPS API + SignalR
```

The initial container deployment is expected to use one replica because the API currently contains long-running MQTT and invoice workers.

Running multiple replicas without coordination could cause duplicate subscriptions or competing background work.

---



## License

MIT

---

> **Ahmad Edais**  
> Software Developer — Amman, Jordan
>
> [LinkedIn](https://linkedin.com/in/ahmad-edais) · [Email](mailto:ahmad.edais.jo@gmail.com)
