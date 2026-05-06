# GrabAndGo — Cashierless Smart Retail Backend

> **A fully autonomous, IoT-integrated smart shopping system where customers walk in, pick items tracked by computer vision, and walk out — no cashier, no scanning, no friction.**

---

## The Idea

GrabAndGo reimagines in-store retail by eliminating the checkout line entirely. A customer opens the mobile app, generates a QR code, scans it at the entrance gate, and starts shopping. A network of computer vision cameras tracks every item picked up or returned in real time. When the customer walks to the exit, the system calculates their total, deducts it from their digital wallet, and opens the gate — all in under a second.

The system is modeled after the "just walk out" paradigm, built from the ground up on an event-driven IoT architecture: edge cameras publish MQTT events to a cloud broker, a .NET background service consumes those events and updates a shopping cart in SQL Server, and the cart delta is instantly pushed to the customer's phone over WebSocket (SignalR). Nothing is polled from the app side — every cart change arrives as a push notification.

---

## My Role

I was the **sole backend engineer and system architect** on this project. I designed and implemented every layer of the backend — from the database schema and stored procedure contracts all the way to the real-time IoT pipeline and hardware authentication system. The scope included:

- Designing the full layered .NET 8 architecture and enforcing its dependency rules
- Modeling the SQL Server schema (19 tables, 15+ stored procedures) and all JSON contracts
- Building the MQTT ingestion pipeline that translates vision events into cart mutations
- Wiring up SignalR to push cart state to the Flutter client with no polling
- Designing the dual authentication strategy: JWT for customers, API keys for hardware
- Implementing the cryptographic QR token system (HMAC-SHA256, one-time use)
- Building the background invoice generation pipeline using QuestPDF
- Writing the full API surface consumed by the Flutter mobile client

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          Flutter Mobile App                          │
│  (JWT Auth · WebSocket · QR Display · Real-Time Cart · Invoices)     │
└────────────────────────────┬────────────────────────────────────────┘
                             │ HTTPS / WebSocket (JWT)
┌────────────────────────────▼────────────────────────────────────────┐
│                      ASP.NET Core 8 Web API                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐   │
│  │  REST API    │  │ SignalR Hub  │  │  Background Services      │   │
│  │  Controllers │  │  /hubs/cart  │  │  MqttVisionWorker         │   │
│  └──────┬───────┘  └──────┬───────┘  │  InvoiceWorker            │   │
│         │                 │          └───────────┬──────────────┘   │
│  ┌──────▼─────────────────▼────────────────────▼──────────────┐    │
│  │                     Services Layer                           │    │
│  │  UserService · SessionService · CartService · CheckoutService│    │
│  │  WalletService · InvoiceService · VisionSystemService · ...  │    │
│  └──────────────────────────────┬──────────────────────────────┘    │
│  ┌───────────────────────────────▼──────────────────────────────┐   │
│  │               Repository Layer  (SqlExecutor)                 │   │
│  │        Raw ADO.NET  ·  FOR JSON PATH  ·  OPENJSON             │   │
│  └───────────────────────────────┬──────────────────────────────┘   │
└──────────────────────────────────┼──────────────────────────────────┘
                                   │
┌──────────────────────────────────▼──────────────────────────────────┐
│                    SQL Server — GrabAndGoDB                           │
│           19 tables  ·  15+ Stored Procedures  ·  Transactions        │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                         IoT Hardware Layer                            │
│                                                                       │
│  ┌─────────────────┐   MQTT    ┌──────────────┐  X-Api-Key  ┌──────┐│
│  │  Vision Cameras │──────────▶│  MQTT Broker │◀────────────│ Gate ││
│  │  (Edge Devices) │  publish  │              │  REST POST  │  HW  ││
│  └─────────────────┘           └──────┬───────┘             └──────┘│
│                                        │ subscribe                    │
│                                 MqttVisionWorker                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Runtime** | .NET 8, ASP.NET Core |
| **Database** | SQL Server — raw ADO.NET, no ORM |
| **Real-Time** | ASP.NET Core SignalR (WebSocket) |
| **IoT Messaging** | MQTT via MQTTnet 4.3.7 |
| **Authentication** | JWT Bearer (customers) + HMAC API Keys (hardware) |
| **Password Security** | BCrypt.Net-Next |
| **PDF Generation** | QuestPDF |
| **API Docs** | Swagger / OpenAPI |
| **Mobile Client** | Flutter (external repo) |

---

## Project Structure

```
GrabAndGo/
├── GrabAndGo.Api/               # Entry point: controllers, hubs, workers, DI wiring
│   ├── Controllers/             # 8 REST controllers
│   ├── Hubs/CartHub.cs          # SignalR hub for real-time cart updates
│   ├── BackgroundServices/      # MqttVisionWorker + InvoiceWorker
│   └── Security/                # Hardware API key middleware
│
├── GrabAndGo.Services/          # Business logic — injected via interfaces only
│
├── GrabAndGo.DataAccess/        # Repository pattern + SqlExecutor core
│   └── Core/SqlExecutor.cs      # 3-method ADO.NET abstraction over stored procs
│
├── GrabAndGo.Models/            # Request/Response DTOs shared across all layers
│
├── DBScript.sql                 # Schema creation + seed data
├── StoredProcedures.sql         # All 15+ stored procedures
└── GrabAndGo.http               # Manual endpoint tests
```

Dependency direction is strictly enforced: `Api → Services → DataAccess → Models`. No layer skips a tier.

---

## IoT & Real-Time Pipeline

This is the core of the system. The full path from a physical item pick to a UI update on the customer's phone:

```
Customer picks item off shelf
        │
        ▼
Vision camera detects pick event
  { TrackId, AiLabel: "coca-cola", Action: "Pick", Confidence: 0.94 }
        │
        ▼  MQTT publish
  Topic: grabandgo/{tenantId}/store/{storeId}/vision/events/{cameraCode}
        │
        ▼  MqttVisionWorker (background service, always-on)
  Deserializes JSON → VisionEventRequestDto
  Creates DI scope → ICartService.ProcessVisionEventAsync()
        │
        ▼  CartService → CartRepository → SP_ProcessVisionEvent
  Looks up product by AiLabel
  Resolves active session via SessionTrackBinding
  Upserts CartItems (quantity +1 or -1)
  Increments CartVersion
  Writes CartItemEvent audit row
        │
        ▼  SignalR broadcast
  IHubContext<CartHub>.Clients.Group("Session_42")
       .SendAsync("ReceiveCartUpdate", cartDto)
        │
        ▼  Flutter WebSocket subscriber
  Cart item list updates live on screen — zero polling
```

No polling anywhere in this path. Latency from pick-event to screen update is bounded only by MQTT round-trip plus a single stored procedure execution.

### MQTT Topic Structure

```
grabandgo/{tenantId}/store/{storeId}/vision/events/{cameraCode}
```

The worker subscribes to the wildcard `grabandgo/+/store/+/vision/events/#`, so it handles any store and any camera without reconfiguration.

### SignalR Groups

Each shopping session gets its own group: `Session_{sessionId}`. The Flutter client calls `SubscribeToSession(sessionId)` after connecting; the server verifies JWT ownership before adding the connection to the group. This prevents cross-session data leakage even if two users connect to the same hub instance simultaneously.

---

## Authentication Design

The system has two entirely separate authentication planes — one for end customers, one for hardware devices.

### Customer Authentication (JWT Bearer)

```
POST /api/users/login
→ Returns HS256-signed JWT
  Claims: sub (UserId), email, FirstName, LastName, jti
  Expiry: 7 days
  Key source: GRABANDGO_JWT_KEY env var → appsettings.json fallback
```

**SignalR token workaround:** WebSocket upgrade requests cannot carry custom headers in browser/mobile environments. The Flutter client passes the JWT as a query parameter (`?access_token=...`) on the WebSocket connection, and an `OnMessageReceived` event handler in `Program.cs` extracts it and forwards it into the standard Bearer pipeline before the hub processes the request.

### Hardware Authentication (API Key)

Gate controllers and vision edge devices are not user sessions — they authenticate with static API keys validated on every request. The comparison uses `CryptographicOperations.FixedTimeEquals` to prevent timing-based key enumeration. If the server's own key is missing from configuration, the endpoint returns `503 Service Unavailable` rather than `401` — failing closed so hardware is never left in an undefined state.

```
X-Api-Key: {configured key}  →  [RequireApiKey("Gate")] or [RequireApiKey("Vision")]
```

### QR Token System (Store Entry)

Customers cannot call the gate endpoint with just their user ID — the gate requires a cryptographically unforgeable proof that this user intends to enter right now.

```
1. App calls  POST /api/sessions/generate-qr
2. Server generates: {TokenId}|{HMAC-SHA256(TokenId + UserId + StoreId + Nonce)}
3. Flutter encodes the QrCodeData string into a scannable QR image
4. Gate hardware scans → POST /api/gate/scan
5. Server: verifies HMAC · checks 30-min expiry · marks ConsumedAt (one-time use)
6. On success: atomically creates Session + Cart in a single stored procedure
```

---

## REST API Surface

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/api/users/register` | Public | Create account |
| `POST` | `/api/users/login` | Public | Get JWT token |
| `GET` | `/api/users/{id}` | JWT | Get own profile |
| `POST` | `/api/sessions/generate-qr` | JWT | Issue entry QR token |
| `GET` | `/api/sessions/active` | JWT | Check for active session |
| `GET` | `/api/products` | JWT | Browse catalog (paginated + search) |
| `POST` | `/api/wallets/top-up` | JWT | Add funds |
| `GET` | `/api/wallets/balance` | JWT | Current balance |
| `GET` | `/api/wallets/ledger` | JWT | Transaction ledger |
| `GET` | `/api/transactions` | JWT | Purchase history |
| `POST` | `/api/gate/scan` | API Key | Gate: validate QR + open |
| `POST` | `/api/gate/checkout` | API Key | Gate: checkout + open/hold |
| `POST` | `/api/vision-system/bind-track` | API Key | Vision: bind camera track to session |
| `GET` | `/api/invoices` | JWT | Invoice list |
| `GET` | `/api/invoices/{id}` | JWT | Poll invoice status |
| `GET` | `/api/invoices/{id}/pdf` | JWT | Download PDF |

**Gate safety contract:** `POST /api/gate/checkout` always returns HTTP 200 regardless of the business outcome. Hardware reads the `GateAction` field in the response body (`"OpenGate"` or `"KeepClosed"`). This ensures the physical gate is never left waiting on an HTTP error response.

---

## Database Design

All database access goes through `SqlExecutor` — a three-method ADO.NET abstraction that calls stored procedures exclusively. There is no ORM.

```csharp
ExecuteReaderAsync<T>(spName, parameters)     // SELECT → FOR JSON PATH → List<T>
ExecuteNonQueryAsync<T>(spName, requestBody)  // INSERT/UPDATE via @P_JSON_REQUEST
ExecuteScalarAsync<T>(spName, parameters)     // Scalar: counts, IDs, flags
```

Every stored procedure follows a strict JSON contract:
- **Lists:** `FOR JSON PATH, INCLUDE_NULL_VALUES`
- **Single objects:** `FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES`
- **Inserts/Updates:** Accept a single `@P_JSON_REQUEST` parameter, parsed with `OPENJSON`
- `INCLUDE_NULL_VALUES` is mandatory — the Flutter client requires all keys present even when null

Multi-step operations (user registration, store entry, checkout) are wrapped in explicit `BEGIN TRAN / COMMIT / ROLLBACK`. Wallet deductions use `UPDLOCK` to prevent race conditions under concurrent checkout requests.

### Core Tables

| Table | Purpose |
|---|---|
| `Users` / `Wallets` | Customer accounts and digital payment balances |
| `Stores` / `Zones` / `Cameras` | Physical store topology |
| `Products` / `ProductAiLabels` | Catalog items mapped to vision model class names |
| `Sessions` / `Carts` / `CartItems` | Active shopping trip state |
| `SessionTrackBindings` | Maps a vision TrackId (person silhouette) to a Session |
| `EntryQrTokens` | One-time use HMAC-signed entry tokens |
| `VisionEventsRaw` | Raw event audit log from MQTT |
| `CartItemEvent` | Immutable log of every Pick / Return action |
| `Transactions` / `TransactionItems` | Completed purchases with line items |
| `WalletLedgerEntries` | Full audit trail of every balance change |
| `Invoices` | PDF generation state and file path |

---

## Background Services

### MqttVisionWorker

An always-running `IHostedService` that holds a persistent connection to the MQTT broker. On each incoming message:

1. Deserializes the payload into `VisionEventRequestDto` (TrackId, AiLabel, Action, Confidence, CameraCode)
2. Creates a DI scope (repositories are scoped; the worker is a singleton)
3. Delegates to `ICartService.ProcessVisionEventAsync()`
4. Broadcasts the updated cart snapshot to the session's SignalR group
5. Logs the result; swallows exceptions to prevent the worker from crashing on a malformed message
6. Auto-reconnects if the broker drops the connection

### InvoiceWorker

Decouples PDF generation from the checkout flow. The checkout endpoint creates an invoice stub synchronously so the transaction completes fast, and this worker renders the PDF asynchronously:

1. Polls `SP_GetPendingInvoices` every N seconds (configurable via `Invoice:PollSeconds`)
2. Fetches transaction line items per invoice via `SP_GetInvoiceData`
3. Renders an A4 PDF using QuestPDF
4. Writes the file to `{Invoice:StorageFolder}/invoice-{transactionId}.pdf`
5. Updates the invoice row with the file path via `SP_UpdateInvoicePath`

---

## Customer Journey (End-to-End)

```
1. Register / Login         POST /api/users/login  →  JWT token stored on device

2. Generate QR              POST /api/sessions/generate-qr
                            App renders QR image from returned token string

3. Scan at gate             Gate hardware  →  POST /api/gate/scan
                            HMAC verified, token burned, Session + Cart created
                            Gate opens

4. Vision bind              Edge device  →  POST /api/vision-system/bind-track
                            Camera TrackId linked to this customer's session

5. Shop                     Customer picks / returns items
                            MQTT  →  MqttVisionWorker  →  SP  →  SignalR push
                            Cart updates appear live on phone with no polling

6. Walk to exit             Exit sensor  →  POST /api/gate/checkout
                            Total calculated, wallet deducted, transaction created
                            GateStatusUpdate pushed to Flutter over SignalR
                            Gate opens (or holds if wallet balance is insufficient)

7. Invoice                  InvoiceWorker renders PDF in background
                            App polls  GET /api/invoices/{id}  until IsReady = true
                            Downloads PDF via  GET /api/invoices/{id}/pdf
```

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (local or remote)
- An MQTT broker (default config targets the HiveMQ public sandbox)

### Setup

```bash
# 1. Clone and restore
git clone https://github.com/AhmadEdais/GrabAndGo.git
cd GrabAndGo

# 2. Set the connection string in GrabAndGo/appsettings.json
# "ConnectionStrings": { "DefaultConnection": "Server=.;Database=GrabAndGoDB;..." }

# 3. Run database setup against your SQL Server instance
#    Execute DBScript.sql, then StoredProcedures.sql

# 4. Override the JWT signing key via environment variable (recommended)
$env:GRABANDGO_JWT_KEY = "YourCryptographicallyRandomKeyAtLeast32Chars"

# 5. Build and run
dotnet build
dotnet run --project GrabAndGo/GrabAndGo.Api.csproj
```

Swagger UI is available at `https://localhost:{port}/swagger`.  
Use `GrabAndGo/GrabAndGo.http` for manual endpoint testing in Visual Studio or VS Code.

### Configuration Reference

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "..."             // SQL Server connection string
  },
  "Jwt": {
    "Key": "...",                          // Min 32 chars; override with GRABANDGO_JWT_KEY env var
    "Issuer": "GrabAndGoApi",
    "Audience": "GrabAndGoFlutterApp"
  },
  "HardwareAuth": {
    "GateApiKey": "...",                   // Sent by gate hardware in X-Api-Key header
    "VisionApiKey": "..."                  // Sent by vision edge device in X-Api-Key header
  },
  "Invoice": {
    "StorageFolder": "invoices",           // Local path for generated PDFs
    "PollSeconds": 5,
    "BatchSize": 10
  }
}
```

---

## Security Notes

- JWT signing key is loaded from the `GRABANDGO_JWT_KEY` environment variable first, falling back to appsettings
- Hardware API key comparisons use `CryptographicOperations.FixedTimeEquals` to prevent timing attacks
- QR entry tokens are single-use and HMAC-SHA256 signed — they cannot be replayed or forged
- Wallet deductions use `UPDLOCK` pessimistic locking to prevent double-spend under concurrency
- Replace the public MQTT sandbox broker with a private, authenticated broker before deploying to production
- Move API keys and secrets from appsettings.json to a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.) in production

---
> **Ahmad Edais**
> *Software Developer | Amman, Jordan*
>
> [LinkedIn](https://linkedin.com/in/ahmad-edais) • [Email](mailto:ahmad.edais.jo@gmail.com)
