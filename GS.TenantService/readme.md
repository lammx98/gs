# GS.TenantService

Master data service quản lý cấu hình tenant. Các microservice khác lấy thông tin tenant qua `TenantServiceBaseUrl` trong GS.MultiTenant (`ITenantResolutionService`).

## Database — PostgreSQL

```json
{
  "ConnectionStrings": {
    "TenantDb": "Host=localhost;Port=5432;Database=gs-tenant;Username=postgres;Password=your_password"
  }
}
```

Override qua env: `ConnectionStrings__TenantDb="Host=...;..."`

### Migration

**Không** tự chạy migration khi startup. Apply thủ công:

```bash
dotnet ef database update --project GS.TenantService
```

Tạo migration mới:

```bash
dotnet ef migrations add <MigrationName> --project GS.TenantService
```

## Chạy

```bash
dotnet run --project GS.TenantService
```

Service lắng nghe tại `http://localhost:5100`.

## Observability

```csharp
builder.AddGsObservability(configureTracing: tracing => tracing.AddNpgsql());

app.UseHttpStatusExceptionHandling();
app.UseGsObservability();
app.RunWithObservability();
```

Chi tiết: [GS.Core/readme.md](../GS.Core/readme.md#observability-serilog--opentelemetry).

## API

| Method | Route | Mô tả |
|--------|-------|-------|
| `GET` | `/api/tenants/{tenantCode}` | **Endpoint chính** — GS.MultiTenant gọi khi resolve tenant |
| `GET` | `/api/tenants` | Danh sách tất cả tenant |
| `GET` | `/api/tenants/id/{tenantId}` | Tra cứu theo GUID |
| `POST` | `/api/tenants` | Tạo tenant mới |
| `PUT` | `/api/tenants/{tenantCode}` | Cập nhật tenant |
| `DELETE` | `/api/tenants/{tenantCode}` | Soft delete (`IsActive = false`) |

### Response mẫu (`GET /api/tenants/acme`)

```json
{
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "tenantCode": "acme",
  "tenantName": "Acme Clinic",
  "tier": 0,
  "usesDedicatedDatabase": false,
  "databaseHost": null,
  "databasePort": null,
  "credentialsRef": null
}
```

### Tạo tenant dedicated

```json
POST /api/tenants
{
  "tenantCode": "vipcare",
  "tenantName": "VIP Care Hospital",
  "tier": 2,
  "usesDedicatedDatabase": true,
  "databaseHost": "vip-pg.internal",
  "databasePort": 5432,
  "credentialsRef": "default"
}
```

## Kết nối với microservice khác

```json
{
  "MultiTenant": {
    "TenantServiceBaseUrl": "http://localhost:5100"
  }
}
```

`TenantServiceEndpointTemplate` có default `/api/tenants/{tenantCode}` — không cần khai báo nếu dùng mặc định.

## Quy tắc nghiệp vụ

- `TenantCode` unique, normalize lowercase
- `usesDedicatedDatabase = true` → bắt buộc `DatabaseHost`
- `usesDedicatedDatabase = false` → không được có `DatabaseHost`, `DatabasePort`, `CredentialsRef`
- `Tier`: `Basic` (0), `Standard` (1), `Premium` (2) — độc lập với `usesDedicatedDatabase`
- Xóa là soft delete — tenant inactive không trả về qua GET resolve

## Schema `Tenants`

| Column | Type | Mô tả |
|--------|------|-------|
| `Id` | uuid | PK |
| `TenantCode` | varchar(64) | Unique |
| `TenantName` | varchar(256) | |
| `Tier` | int | 0/1/2 |
| `UsesDedicatedDatabase` | bool | Routing DB |
| `DatabaseHost` | varchar(256) | Nullable |
| `DatabasePort` | int | Nullable |
| `CredentialsRef` | varchar(128) | Nullable |
| `IsActive` | bool | |
| `CreatedAt` | timestamptz | |
| `UpdatedAt` | timestamptz | Nullable |
