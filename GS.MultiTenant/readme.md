# GS.MultiTenant

Thư viện common multi-tenant cho hệ sinh thái GS microservices. Đóng gói [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant) và logic **Hybrid Database** (Shared DB + Dedicated DB) để các service nghiệp vụ chỉ cần cấu hình plug-and-play.

## Mục tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **Encapsulation** | Che giấu sự phức tạp của Finbuckle và routing DB khỏi business code |
| **Consistency** | Mọi microservice dùng chung chuẩn định danh, cache, propagation |
| **Plug & Play** | Khởi tạo qua extension methods ngắn gọn tại `Program.cs` |

## Phụ thuộc

- **GS.Core** — primitives dùng chung: `HttpStatusException`, `AmbientContext<T>`, `StaleWhileRevalidateCache<T>`
- **Finbuckle.MultiTenant** — tenant resolution engine
- **EF Core** — hybrid `TenantBaseDbContext`
- **MassTransit** (tùy chọn) — lan truyền tenant qua message bus

---

## Phân biệt TenantCode vs TenantId

Đây là hai lớp định danh khác nhau trong luồng multi-tenant:

| Thuộc tính | Ví dụ | Nguồn | Vai trò |
|------------|-------|-------|---------|
| **TenantCode** (`Identifier`) | `acme` | Subdomain `acme.domain.com`, header, JWT claim | Định danh **bên ngoài**, dùng để **resolve** tenant |
| **TenantId** (`Id`) | `3fa85f64-...` | Tenant Service trả về | Khóa **nội bộ** (GUID), dùng cho DB filter, FK, log |

**Luồng xử lý:**

```
URL acme.domain.com
    → Finbuckle HostStrategy trích "acme" (tenantCode)
    → CachedTenantStore lookup theo tenantCode
    → HttpTenantConfigurationClient gọi Tenant Service
    → Nhận TenantModel đầy đủ (TenantId, Tier, ConnectionString...)
    → Cache lại, dùng cho request hiện tại
```

Trong code, dùng alias cho dễ đọc:

```csharp
tenant.TenantCode  // == Identifier  — mã từ URL
tenant.TenantId    // == Id          — GUID nội bộ
```

Inject qua `ICurrentTenantAccessor`:

```csharp
accessor.TenantCode  // "acme"
accessor.TenantId    // "3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## TenantServiceBaseUrl dùng để làm gì?

`TenantServiceBaseUrl` là **địa chỉ gốc** của **Tenant Service / Identity Service** nội bộ — service quản lý master data tenant.

Microservice **không** tự lưu connection string, tier hay cấu hình tenant. Khi request đến, lib chỉ biết `tenantCode` (từ URL/header/JWT). Lib sẽ:

1. Gọi `GET {TenantServiceBaseUrl}{TenantServiceEndpointTemplate}`  
   Ví dụ: `GET https://tenant-service.internal/api/tenants/acme`
2. Nhận JSON → map vào `TenantModel` (TenantId, TenantName, Tier, ConnectionString...)
3. **Cache** kết quả (Memory/Redis) — không gọi lại mỗi request (FR02, NFR01)

Nếu **để trống** `TenantServiceBaseUrl` (dev/local): lib tạo `TenantModel` tạm với `TenantId = TenantCode = mã vừa resolve`, tier Basic, không connection string riêng.

Xem project **[GS.TenantService](../GS.TenantService/readme.md)** — service mẫu chạy tại `http://localhost:5100`.

```json
{
  "MultiTenant": {
    "TenantServiceBaseUrl": "https://tenant-service.internal",
    "TenantServiceEndpointTemplate": "/api/tenants/{tenantCode}"
  }
}
```

---

## Yêu cầu chức năng

### FR01 — Tenant Resolution

Tự động trích xuất tenant từ:

| Nguồn | Giá trị resolve | Cơ chế |
|-------|-----------------|--------|
| HTTP Header | tenantCode hoặc tenantId | `X-Tenant-Id` (API Gateway) |
| URL | **tenantCode** | Subdomain `tenantCode.domain.com` |
| JWT | tenantId (thường là GUID) | Claim `tenant_id` |
| Message Broker | tenantId / tenantCode | Header `X-Tenant-Id` khi consume |

Middleware `TenantConsistencyMiddleware` kiểm tra **khớp tenant** giữa Header, URL và JWT. Ném exception rõ ràng khi thiếu hoặc xung đột:

| Exception | HTTP Status |
|-----------|-------------|
| `TenantNotResolvedException` | 400 |
| `TenantMismatchException` | 401 |
| `TenantNotFoundException` | 404 |

### FR02 — Tenant Store & Caching

- Model chuẩn: `TenantModel` (`TenantId`, `TenantCode`, `TenantName`, `Tier`, `ConnectionString`)
- `HttpTenantConfigurationClient` gọi Tenant Service / Identity Service
- `CachedTenantStore` cache qua **Memory + Redis** (tùy chọn), dùng `StaleWhileRevalidateCache` từ GS.Core — **không gọi Tenant Service mỗi request**

### FR03 — Hybrid Database Routing

`TenantBaseDbContext` (kế thừa bởi mọi service):

- **VIP** (`ConnectionString` có giá trị) → kết nối Dedicated DB
- **Basic** (`ConnectionString` null/rỗng) → Shared DB + global query filter `WHERE tenant_id = current_tenant`

Entity implement `ITenantEntity` để tự động áp filter.

### FR04 — Context Propagation

| Kênh | API |
|------|-----|
| HTTP → HTTP | `AddTenantPropagation()` trên `HttpClient` |
| Event Bus | `UseTenantPropagation()` + `UseTenantPublishPropagation()` (MassTransit) |
| Worker thủ công | `TenantMessageContext.SetTenant(id)` / `TenantMessageContextInitializer` |

---

## Yêu cầu phi chức năng

| ID | Mô tả | Triển khai |
|----|-------|------------|
| NFR01 | Latency phân giải tenant & cache ≤ 5ms | Cache in-memory trước, Redis sau, SWR background refresh |
| NFR02 | Không bypass filter tenant tùy tiện | `ITenantBypassService` + `ApplyTenantPolicy()` |
| NFR03 | Chịu lỗi khi Tenant Service sập | Stale-while-revalidate: dùng cache cũ khi refresh thất bại |

---

## Cài đặt nhanh

### 1. `appsettings.json`

```json
{
  "MultiTenant": {
    "TenantHeaderName": "X-Tenant-Id",
    "JwtTenantClaimType": "tenant_id",
    "HostTemplate": "__tenant__.*",
    "TenantServiceBaseUrl": "https://tenant-service.internal",
    "TenantServiceEndpointTemplate": "/api/tenants/{tenantCode}",
    "SharedDatabaseConnectionString": "Server=...;Database=shared;",
    "UseRedisCache": true,
    "RedisConnectionString": "localhost:6379",
    "RequireTenant": true
  }
}
```

### 2. `Program.cs`

```csharp
using GS.MultiTenant.Extensions;

builder.Services.AddMultiTenantServices(builder.Configuration);
// hoặc alias: builder.Services.AddClinicMultiTenant(builder.Configuration);

builder.Services.AddHttpClient("InternalApi")
    .AddTenantPropagation();

builder.Services.AddMassTransit(x =>
{
    x.UseTenantPropagation();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseTenantPublishPropagation(context);
    });
});

var app = builder.Build();
app.UseTenantResolution();
```

### 3. DbContext

```csharp
public class AppDbContext : TenantBaseDbContext
{
    public AppDbContext(
        IMultiTenantContextAccessor accessor,
        IOptions<MultiTenantOptions> options,
        DbContextOptions<AppDbContext> dbOptions)
        : base(accessor, options, dbOptions) { }

    protected override void ConfigureProvider(DbContextOptionsBuilder options, string connectionString)
        => options.UseSqlServer(connectionString);
}
```

### 4. Business logic

```csharp
public class OrderService(ICurrentTenantAccessor tenant)
{
    public void ValidateFeature()
    {
        if (tenant.Tier == TenantTier.Basic)
            throw new InvalidOperationException("Feature not available on Basic plan.");
    }
}
```

### 5. Admin bypass (job tổng hợp)

```csharp
using (bypassService.EnableBypass())
{
    var all = db.Orders.ApplyTenantPolicy(bypassService).ToList();
}
```

---

## Cấu trúc project

```
GS.MultiTenant/
├── Abstractions/       ICurrentTenantAccessor, ITenantBypassService, ITenantEntity
├── Configuration/      MultiTenantOptions
├── Data/               TenantBaseDbContext, TenantQueryableExtensions
├── Exceptions/         TenantNotResolved, TenantMismatch, TenantNotFound
├── Extensions/         DI, middleware, DbContext, MassTransit
├── Http/               TenantPropagationDelegatingHandler
├── Messaging/          TenantMessageContext, MassTransit filters
├── Middleware/         TenantConsistencyMiddleware
├── Models/             TenantModel, TenantTier
├── Resolution/         TenantIdentifierExtractor
├── Services/           CurrentTenantAccessor, TenantBypassService
└── Stores/             CachedTenantStore, HttpTenantConfigurationClient

GS.Core/ (dùng chung)
├── Ambient/            AmbientContext<T>
├── Caching/            StaleWhileRevalidateCache<T>
├── Exceptions/         HttpStatusException
└── Middleware/         HttpStatusExceptionMiddleware
```

---

## API chính

| Method | Mô tả |
|--------|-------|
| `AddMultiTenantServices()` | Đăng ký toàn bộ DI |
| `AddClinicMultiTenant()` | Alias theo tài liệu DX01 |
| `UseTenantResolution()` | Pipeline middleware + Finbuckle |
| `AddTenantDbContext<T>()` | Đăng ký DbContext hybrid |
| `AddTenantPropagation()` | Gắn tenant header vào HttpClient |
| `UseTenantPropagation()` | MassTransit consume filter |
| `UseTenantPublishPropagation()` | MassTransit publish filter |

---

## CAP (DotNetCore.CAP)

Chưa tích hợp sẵn. Trong CAP subscriber, gọi:

```csharp
using var scope = TenantMessageContextInitializer.InitializeFromHeaders(headers);
```

để khôi phục ngữ cảnh tenant từ message headers.

---

## Tài liệu gốc

Xem `docs.md` để biết đầy đủ functional / non-functional requirements.
