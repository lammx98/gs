# GS.MultiTenant

Thư viện common multi-tenant cho hệ sinh thái GS microservices. Đóng gói [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant) và logic **Hybrid Database** (Shared DB + Dedicated DB) để các service nghiệp vụ chỉ cần cấu hình plug-and-play.

## Mục tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **Encapsulation** | Che giấu Finbuckle và routing DB khỏi business code |
| **Consistency** | Mọi microservice dùng chung chuẩn tenant, cache, propagation |
| **Plug & Play** | Khởi tạo qua extension methods tại `Program.cs` |

## Phụ thuộc

- **GS.Core** — `ILayeredCache`, `HttpStatusException`, `AmbientContext`
- **Finbuckle.MultiTenant** — tenant resolution engine
- **EF Core** — hybrid `TenantBaseDbContext`
- **MassTransit** (tùy chọn) — lan truyền tenant qua message bus

---

## Phân biệt TenantCode vs TenantId

| Thuộc tính | Ví dụ | Nguồn | Vai trò |
|------------|-------|-------|---------|
| **TenantCode** | `acme` | Subdomain, header `X-Tenant-Id`, JWT | Định danh **bên ngoài**, dùng để **resolve** |
| **TenantId** | `3fa85f64-...` | TenantService trả về | Khóa **nội bộ** (GUID), DB filter, FK |

**Luồng resolve tenant:**

```
Request (header / subdomain / JWT claim)
    → Finbuckle strategy trích tenantCode
    → ITenantResolutionService.GetByTenantCodeAsync
        → ILayeredCache (Memory → Redis)
        → miss: GET TenantService /api/tenants/{tenantCode}
        → cache tenant:code:{code} + tenant:id:{id}
    → IConnectionStringResolver build PostgreSQL connection string
    → TenantBaseDbContext route DB
```

---

## TenantModel

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

| Field | Mô tả |
|-------|-------|
| `tier` | `Basic` (0), `Standard` (1), `Premium` (2) — gói cước |
| `usesDedicatedDatabase` | `true` → dedicated DB; `false` → shared DB + filter |
| `databaseHost` | Host PostgreSQL (bắt buộc khi `usesDedicatedDatabase = true`) |
| `credentialsRef` | Key tra `DatabaseCredentials:{ref}` trong appsettings service |

**Lưu ý:** Service **không** nhận connection string từ TenantService. Service tự build CS từ `databaseHost` + naming template + credentials local.

---

## Hybrid Database Routing

`TenantBaseDbContext` + `IConnectionStringResolver` (`PostgreSqlConnectionStringResolver`):

| `usesDedicatedDatabase` | Routing |
|-------------------------|---------|
| `false` | `SharedDatabaseConnectionString` + global filter `tenant_id` |
| `true` | Build CS: `Host` + `{tenantCode}_{serviceName}` + `DatabaseCredentials` |

Entity implement `ITenantEntity` để tự động áp global query filter trên shared DB.

### Config service nghiệp vụ

```json
{
  "MultiTenant": {
    "ServiceDatabaseName": "identity",
    "DatabaseNamingTemplate": "{tenantCode}_{serviceName}",
    "SharedDatabaseConnectionString": "Host=localhost;Port=5432;Database=hms_identity_shared;Username=postgres;Password=...",
    "TenantServiceGrpcAddress": "http://localhost:5001",
    "UseRedisCache": true,
    "RedisConnectionString": "localhost:6379",
    "CacheAbsoluteExpiration": "00:30:00",
    "RequireTenant": true
  },
  "DatabaseCredentials": {
    "default": { "Username": "postgres", "Password": "..." }
  }
}
```

| Config | Bắt buộc | Ghi chú |
|--------|----------|---------|
| `ServiceDatabaseName` | Có (nếu có dedicated tenant) | Tên DB: `acme_identity` |
| `SharedDatabaseConnectionString` | Có | CS shared DB |
| `TenantServiceGrpcAddress` | Khuyến nghị | Để trống = tenant giả (dev only). gRPC port mặc định `5001` |
| `TenantHeaderName` | Không | Default `X-Tenant-Id` |
| `JwtTenantClaimType` | Không | Default `tenant_id` |
| `HostTemplate` | Không | Default `__tenant__.*` (subdomain) |
| `UseRedisCache` | Không | Bật Redis distributed cache |

### Subdomain (`acme.hms.com`)

```json
"HostTemplate": "__tenant__.*"
```

Dev local: thêm `127.0.0.1 acme.localhost` vào hosts, gọi `http://acme.localhost:5193/...`

---

## ITenantResolutionService

Service trung tâm lấy cấu hình tenant, dùng `ILayeredCache` strategy **MemoryThenRedis**:

| Method | Mô tả |
|--------|-------|
| `GetByTenantCodeAsync(code)` | Cache → TenantService gRPC → cache cả code + id key |
| `GetByTenantIdAsync(id)` | Cache → TenantService gRPC → cache |
| `SetAsync(tenant)` | Ghi cache thủ công |
| `ClearAsync(code, id?)` | Xóa cache |

Cache keys:
- `tenant:code:{tenantCode}`
- `tenant:id:{tenantId}`

`CachedTenantStore` (Finbuckle) delegate sang `ITenantResolutionService`.

### TenantService dependency

| Tình huống | Kết quả |
|------------|---------|
| Cache hit | Không gọi TenantService |
| Cache miss | Gọi TenantService 1 lần, cache lại |
| TenantService down + đã cache | Vẫn chạy |
| TenantService down + chưa cache | Tenant not found |
| `TenantServiceGrpcAddress` trống | Tenant giả (dev), không gọi gRPC |

Khuyến nghị production: bật `UseRedisCache` để cache sống qua pod restart.

---

## Tenant Resolution Strategies (Finbuckle)

Tất cả strategy được đăng ký; chỉ active khi request có dữ liệu tương ứng:

| Nguồn | Cơ chế |
|-------|--------|
| HTTP Header | `X-Tenant-Id` (configurable) |
| Subdomain | `HostTemplate`, default `__tenant__.*` |
| JWT | Claim `tenant_id` (sau `UseAuthentication`) |
| Message | `X-Tenant-Id` header khi consume |

`TenantConsistencyMiddleware` so khớp các nguồn **có mặt** trong request. Chỉ cần 1 nguồn hợp lệ là đủ; nhiều nguồn khác nhau → `401 TenantMismatch`.

---

## Cài đặt nhanh

### `Program.cs`

```csharp
using GS.Core.Extensions;
using GS.MultiTenant.Extensions;

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMultiTenantServices(builder.Configuration);

builder.Services.AddTenantDbContext<AppDbContext>((options, cs) =>
    options.UseNpgsql(cs));

var app = builder.Build();

app.UseAuthentication();       // JWT trước
app.UseTenantResolution();     // MultiTenant + consistency check
app.UseAuthorization();
```

### DbContext

```csharp
public class AppDbContext : TenantBaseDbContext
{
    public AppDbContext(
        IMultiTenantContextAccessor accessor,
        IConnectionStringResolver resolver,
        DbContextOptions<AppDbContext> options)
        : base(accessor, resolver, options) { }

    protected override void ConfigureProvider(DbContextOptionsBuilder options, string cs)
        => options.UseNpgsql(cs);
}
```

### Entity

```csharp
public class Order : ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    // ...
}
```

---

## Context Propagation

| Kênh | API |
|------|-----|
| HTTP → HTTP | `AddTenantPropagation()` trên `HttpClient` |
| Event Bus | `UseTenantPropagation()` + `UseTenantPublishPropagation()` (MassTransit) |
| Worker | `TenantMessageContext.SetTenant(id)` |

---

## Cấu trúc project

```
GS.MultiTenant/
├── Abstractions/       ICurrentTenantAccessor, ITenantResolutionService, IConnectionStringResolver
├── Configuration/      MultiTenantOptions
├── Data/               TenantBaseDbContext
├── Services/           TenantResolutionService, PostgreSqlConnectionStringResolver
├── Stores/             CachedTenantStore, GrpcTenantConfigurationClient
├── Protos/             gs/tenant/v1/tenant.proto (gRPC contract)
├── Middleware/         TenantConsistencyMiddleware
├── Models/             TenantModel, TenantTier
└── Extensions/         DI, middleware, DbContext, MassTransit
```

---

## API chính

| Method | Mô tả |
|--------|-------|
| `AddMultiTenantServices()` | Đăng ký toàn bộ DI (cache, resolver, Finbuckle) |
| `UseTenantResolution()` | Pipeline middleware + Finbuckle |
| `AddTenantDbContext<T>()` | DbContext hybrid với connection string đã resolve |
| `AddTenantPropagation()` | Gắn tenant header vào HttpClient |
| `ITenantResolutionService` | Lấy/cache tenant config |
| `IConnectionStringResolver` | Build PostgreSQL CS từ TenantModel |

---

## Tài liệu gốc

Xem `docs.md` để biết đầy đủ functional / non-functional requirements.
