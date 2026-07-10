# GS.TenantService

Master data service quản lý cấu hình tenant. Các microservice khác lấy thông tin tenant qua **gRPC** (`TenantServiceGrpcAddress` trong GS.MultiTenant).

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

Service lắng nghe tại:
- REST: `http://localhost:5000`
- gRPC (internal): `http://localhost:5001`

## Docker

Build image (từ thư mục gốc repo `GS`):

```bash
docker build -f GS.TenantService/Dockerfile -t gs-tenant-service .
```

Chạy cùng stack local (dùng PostgreSQL container `postgres` trên network `local-shared-network`):

```bash
# Đảm bảo network external đã tồn tại
docker network create local-shared-network   # bỏ qua nếu đã có

# Từ thư mục GS
docker compose up -d --build

# Migration lần đầu
docker compose --profile migrate run --rm tenant-service-migrate
```

| Service | URL |
|---------|-----|
| TenantService REST | `http://localhost:5100` |
| TenantService gRPC | `http://localhost:5101` (microservices dùng address này) |
| PostgreSQL | Container `postgres` trên `local-shared-network` |

Chạy container đơn lẻ (join shared network):

```bash
docker run --rm -p 5100:5000 -p 5101:5001 \
  --network local-shared-network \
  -e ConnectionStrings__TenantDb="Host=postgres;Port=5432;Database=gs-tenant;Username=postgres;Password=123456a@" \
  gs-tenant-service
```

## Observability

```csharp
builder.AddObservability(configureTracing: tracing => tracing.AddNpgsql());

app.UseHttpStatusExceptionHandling();
app.UseObservability();
app.RunWithObservability();
```

Chi tiết: [GS.Core/readme.md](../GS.Core/readme.md#observability-serilog--opentelemetry).

## API

### gRPC (internal — GS.MultiTenant)

Contract: `GS.MultiTenant/Protos/gs/tenant/v1/tenant.proto`

| RPC | Mô tả |
|-----|-------|
| `GetByTenantCode` | Resolve tenant theo code |
| `GetByTenantId` | Resolve tenant theo GUID |

### REST (management / external)

| Method | Route | Mô tả |
|--------|-------|-------|
| `GET` | `/api/tenants/{tenantCode}` | Tra cứu theo code |
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
    "TenantServiceGrpcAddress": "http://localhost:5001"
  }
}
```

Docker (service trong cùng `gs-internal` network): `http://gs-tenant-service:5001`  
Docker (consumer chạy host): `http://localhost:5101`

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
