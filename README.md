# Semantic Layer Manager

A mini system for creating and managing a **semantic layer** over a relational database.

A semantic layer is an abstraction between a physical database and its business users:
it translates cryptic technical names (`cust_t.eml`) into business terms ("Customer → Email"),
hides technical complexity, protects sensitive data, and lets people browse data without
knowing the schema or writing SQL.

The system reads a database's structure **dynamically**, merges it with an external
**metadata file** and with **manual edits**, and exposes the result through two UIs:
a **management** screen (curate the mapping) and a **data explorer** (consume data through
the layer, with PII masked).

> Full design rationale, architecture diagram, sync process, decisions, limitations and
> future work are in [`docs/DESIGN.md`](docs/DESIGN.md).

---

## Tech stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 10 Web API (C#), EF Core 10 (semantic store), Dapper (source DB) |
| Frontend | Angular 20 (standalone, signals) + Angular Material |
| Database | SQL Server 2022 (or LocalDB) |

---

## Prerequisites

- **.NET SDK 10** — `dotnet --version` should print `10.x`
- **Node.js 20.19+ / 22 / 24** and npm — `node --version`
- **SQL Server 2022** — a local instance reachable at `localhost` (Windows Authentication)
- **sqlcmd** or SQL Server Management Studio (to run the database scripts)

No `dotnet-ef` tool is required: the backend applies its own EF migrations on startup.

---

## Setup & run

### 1. Create the source database

The source (operational) database `EShopSource` and its sample data are created by the
scripts in [`database/`](database). Run them against your SQL Server instance:

```bash
sqlcmd -S localhost -i database/01_create_source_db.sql
sqlcmd -S localhost -i database/02_seed_source_data.sql
```

> Prefer a GUI? Open the two `.sql` files in SSMS and run them.

Connection strings live in
[`backend/src/SemanticLayerManager.Api/appsettings.json`](backend/src/SemanticLayerManager.Api/appsettings.json)
and default to `Server=localhost`. Adjust `Server=` only if your SQL Server instance is
named differently. The `SemanticLayerStore` (mapping) database is created **automatically**
on first run — no manual step, and existing data is preserved across restarts.

### 2. Run the backend

```bash
cd backend
dotnet run --project src/SemanticLayerManager.Api --urls http://localhost:5281
```

- API base: `http://localhost:5281/api`
- Interactive API reference (Scalar): `http://localhost:5281/scalar/v1`

### 3. Run the frontend

```bash
cd frontend
npm install
npm start
```

Open **http://localhost:4200**. The dev server proxies `/api` to the backend
(see `frontend/proxy.conf.json`), so no CORS setup is needed.

---

## The central scenario (end to end)

1. Open **Management**. On entry the app runs a **sync**: it introspects `EShopSource`
   and lists every table/column. New/unmapped columns are surfaced here.
2. Click **Upload metadata** and choose [`metadata/ecommerce-metadata.json`](metadata/ecommerce-metadata.json).
   Business names, descriptions, PII flags, hidden flags and categories are merged in.
3. Optionally **edit a field** inline (e.g. rename a column, toggle PII/hidden) and **Save** —
   this is a manual override (`Source = User`).
4. Open **Data Explorer**, pick a business entity (e.g. *Customer*), and browse the data
   through the layer: business column names, **hidden columns excluded**, and **email masked**.

### Optional: demonstrate schema drift

```bash
sqlcmd -S localhost -i database/03_schema_change_demo.sql   # adds cust_t.phone
```

Back in **Management**, click **Sync now** — the new `phone` column appears as **Unmapped**
and is listed in the sync report. It stays hidden from consumers until you map it
(secure-by-default).

---

## Running the tests

```bash
cd backend
dotnet test
```

19 unit tests cover the sync/reconcile engine, the metadata merge, and PII masking.

---

## Project structure

```
backend/
  src/SemanticLayerManager.Api/     .NET 10 Web API (layered by folder)
    Domain/                         entities + enums
    Application/                    Introspection, Sync, Metadata, Management, DataAccess
    Infrastructure/                 EF Core store, Dapper introspection, dynamic query
    Controllers/                    Schema, Sync, Metadata, Semantic, Data
  tests/SemanticLayerManager.Tests/ xUnit tests
frontend/
  src/app/
    core/                           API service + typed models
    features/management/            management screen
    features/data-explorer/         consumer screen
database/                           source DB create + seed + drift-demo scripts
metadata/                           example metadata file
docs/                               design document + diagram
```

## API endpoints

| Method & path | Purpose |
|---------------|---------|
| `POST /api/sync` | Reconcile the source schema into the semantic layer |
| `POST /api/metadata/import` | Upload & merge a metadata file |
| `GET /api/semantic/entities` | List entities + fields (management) |
| `PUT /api/semantic/fields/{id}` | Manually edit a field |
| `GET /api/data/entities` | List consumable entities |
| `GET /api/data/{entityId}?page&pageSize` | Page of data through the layer (masked) |
| `GET /api/schema` | Diagnostic: raw physical schema |
