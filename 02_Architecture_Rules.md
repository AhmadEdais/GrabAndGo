## 1. Database & ORM Rules (STRICT)
* **NO ENTITY FRAMEWORK CORE:** We are strictly using raw ADO.NET with a custom `SqlExecutor` class to achieve sub-200ms API latency. Do not generate `DbContext` or LINQ-to-SQL queries under any circumstances.
* **JSON Stored Procedures (The Core Engine):** All database communication happens via Stored Procedures. You must act as an expert SQL Server Architect and perfectly align with our JSON-based framework:
    * **List Returns (`Task<List<DTO>>`):** Must use `FOR JSON PATH, INCLUDE_NULL_VALUES`. (Reason: C# expects a JSON array).
    * **Single Object Returns (`Task<DTO?>`):** Must use `FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES`. (Reason: C# expects a single naked JSON object; wrappers crash the deserializer).
    * **Scalars (`ExecuteScalarAsync`):** Use standard raw SQL `SELECT` (no JSON clauses).
    * **The Null Constraint:** Front-end clients (Flutter) require keys to be present even if null. `INCLUDE_NULL_VALUES` is mandatory on all JSON returns.
    * **Parameter Naming:** Our framework uses Reflection. Every `@Parameter` name MUST exactly match the casing and spelling of the C# property (1:1 match).
    * **Nested Hierarchy (One-to-Many):** If returning a parent with children, use a correlated subquery with `FOR JSON PATH`. The alias of the subquery MUST exactly match the name of the C# `List<T>` property.
    * **JSON Ingestion (Deep Writes):** Use `OPENJSON` on a single `@P_JSON_REQUEST` parameter for flat inserts/updates. Avoid massive `CROSS APPLY OPENJSON` scripts for deeply nested inserts; instead, break them apart into flatter, atomic API calls for maintainability.
* **INT IDs Only:** The old GP report mentions GUIDs/UniqueIdentifiers. IGNORE THAT. We exclusively use `INT IDENTITY(1,1)` for all primary and foreign keys.

## 2. The 3-Tier Architecture & Folder Structure
We strictly follow a separated 4-project structure to enforce separation of concerns.

### Project Tree
📦 GrabAndGo (Solution)
 ┣ 📂 GrabAndGo.Api (The "Door")
 ┃ ┣ 📂 Controllers (Thin endpoints, HTTP routing, Status Codes. NEVER talks to DB.)
 ┃ ┗ 📄 Program.cs (Where DI container is configured)
 ┣ 📂 GrabAndGo.DataAccess (The "Plumbing")
 ┃ ┣ 📂 Core (Contains the custom `SqlExecutor.cs`)
 ┃ ┣ 📂 Interfaces (e.g., `IUserRepository.cs`)
 ┃ ┗ 📂 Repositories (Executes SQL. e.g., `UserRepository.cs`)
 ┣ 📂 GrabAndGo.Models (The "Contracts")
 ┃ ┣ 📂 Requests (Data coming IN. Must have Data Annotations.)
 ┃ ┗ 📂 Responses (Data going OUT. Must hide sensitive data.)
 ┗ 📂 GrabAndGo.Services (The "Brain")
   ┣ 📂 Implementations (Business logic, password hashing via BCrypt)
   ┗ 📂 Interfaces (e.g., `IUserService.cs`)

### Dependency Injection (DI) & Interface Rules
* **No Static Classes/Methods:** Never use `static` methods for database access or business logic. 
* **Interface Segregation:** Every Service and every Repository MUST have a corresponding Interface. The implementation file must inherit from this interface.
* **Constructor Injection Only:** * Controllers may ONLY inject Service interfaces (e.g., `IUserService`).
    * Services may ONLY inject Repository interfaces (e.g., `IUserRepository`).
    * Implementations are never injected directly.
* **DI Registration (`Program.cs`):** * `SqlExecutor` is registered as a `Singleton`.
    * All Repositories and Services are registered as `Scoped` (e.g., `builder.Services.AddScoped<IUserService, UserService>();`).

## 3. Data Transfer Objects (DTOs)
* **Domain models/Entities are NEVER exposed to the API.** * **Every request** must have a `[Feature]RequestDto` with Data Annotations (`[Required]`, etc.).
* **Every response** must use a `[Feature]ResponseDto` that hides sensitive data (like PasswordHashes).
* **Exact Property Matching (JSON Serialization):** Because our `SqlExecutor` automatically serializes DTOs into JSON strings (`@P_JSON_REQUEST`) instead of using manual `cmd.Parameters.AddWithValue()`, the property names in your C# DTOs MUST exactly match the column names in the Database tables and the expected JSON keys in the Stored Procedure's `OPENJSON` clause (case-sensitive). Do not use manual parameter mapping for object properties.