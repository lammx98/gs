# GS.Core

Thư viện **primitives dùng chung** cho toàn bộ hệ sinh thái GS microservices. Cung cấp các building block nhất quán: xử lý lỗi, JWT, cache, MediatR pipeline, observability — để mọi service dùng cùng một chuẩn.

## Mục tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **Consistency** | Một chuẩn logging, tracing, error handling, JWT trên mọi service |
| **Safety** | Result Pattern giúp luồng nghiệp vụ rõ ràng, không throw tung tóe |
| **Plug & Play** | Khởi tạo qua extension methods ngắn gọn tại `Program.cs` |
| **Resilience** | Layered cache (Memory + Redis) và SWR cache khi cần |

## Phụ thuộc chính

| Package | Vai trò |
|---------|---------|
| `Microsoft.AspNetCore.App` | Middleware, MVC extensions |
| `Serilog.AspNetCore` | Structured logging |
| `OpenTelemetry.*` | Traces, metrics, OTLP export |
| `MediatR` + `FluentValidation` | VSA pipeline |
| `FastEndpoints` | `SendResultAsync` mapping |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT validation |

---

## Cấu trúc project

```
GS.Core/
├── Auth/               JwtOptions, IJwtTokenService, GsJwtClaimTypes
├── Ambient/            AmbientContext<T>
├── Caching/            ILayeredCache, LayeredCache, StaleWhileRevalidateCache<T>
├── Configuration/      ObservabilityOptions, JwtOptions, LayeredCacheOptions
├── Exceptions/         HttpStatusException
├── Extensions/         DI & pipeline extensions
├── Mediation/          ValidationBehavior (MediatR + FluentValidation)
├── Middleware/         HttpStatusExceptionMiddleware
├── Results/            Result, Result<T>, Error
└── Security/           IPasswordHasherService
```

---

## Cài đặt nhanh

### 1. Tham chiếu project

```xml
<ProjectReference Include="..\GS.Core\GS.Core.csproj" />
```

### 2. `Program.cs` (service nghiệp vụ)

```csharp
using GS.Core.Extensions;
using GS.MultiTenant.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddGsObservability();

builder.Services.AddGsJwtAuthentication(builder.Configuration);       // validate JWT
builder.Services.AddGsMediatR(typeof(Program).Assembly);              // VSA pipeline
builder.Services.AddGsLayeredCache(builder.Configuration);          // optional, MultiTenant tự gọi
builder.Services.AddMultiTenantServices(builder.Configuration);
builder.Services.AddFastEndpoints();

var app = builder.Build();

app.UseGsObservability();
app.UseAuthentication();        // JWT trước
app.UseTenantResolution();      // MultiTenant sau khi JWT đã parse
app.UseAuthorization();
app.UseFastEndpoints(c => c.Endpoints.RoutePrefix = "api");

app.RunWithObservability();
```

### 3. `Program.cs` (service phát hành token — HMS.Identity)

```csharp
builder.Services.AddGsJwtAuthentication(builder.Configuration, issueTokens: true);
```

---

## JWT Authentication

Tất cả service dùng **cùng** `Jwt` config để validate token. Chỉ service Identity (hoặc auth service) bật `issueTokens: true`.

### Config

```json
{
  "Jwt": {
    "Issuer": "HMS.Identity",
    "Audience": "HMS",
    "SigningKey": "SAME_KEY_ACROSS_ALL_SERVICES",
    "ExpiresMinutes": 60,
    "TenantClaimType": "tenant_id"
  }
}
```

`TenantClaimType` phải khớp `MultiTenant:JwtTenantClaimType`.

### Luồng cross-service

```
Client → HMS.Identity: login → nhận JWT (sub, email, tenant_id)
Client → HMS.Clinical:  Authorization: Bearer {token}
       → AddGsJwtAuthentication validate signature/issuer/audience
       → GS.MultiTenant đọc claim tenant_id, so khớp header/subdomain
```

### API

| Method | Mô tả |
|--------|-------|
| `AddGsJwtAuthentication(config)` | Đăng ký JWT Bearer validation |
| `AddGsJwtAuthentication(config, issueTokens: true)` | Thêm `IJwtTokenService` để phát token |
| `IJwtTokenService.CreateToken(JwtTokenRequest)` | Tạo access token |

---

## MediatR + FluentValidation (VSA)

```csharp
builder.Services.AddGsMediatR(typeof(Program).Assembly);
```

Tự động đăng ký:
- MediatR handlers từ assembly
- FluentValidation validators
- `ValidationBehavior` — validation fail → `Result.Fail(Error.Validation(...))`

---

## FastEndpoints + Result

```csharp
using GS.Core.Extensions;

public override async Task HandleAsync(Request req, CancellationToken ct)
{
    var result = await _mediator.Send(new MyCommand(...), ct);
    await this.SendResultAsync(result, ct);
}
```

---

## LayeredCache (`ILayeredCache`)

Cache hai tầng với **hai chiến lược lookup** rõ ràng:

| `CacheLookupStrategy` | Thứ tự đọc |
|-----------------------|------------|
| `MemoryThenRedis` | Memory → Redis → null/fallback |
| `RedisOnly` | Redis → null/fallback |

| `CacheStorageTarget` | Ghi/xóa |
|----------------------|---------|
| `Memory` | In-Memory only |
| `Redis` | Redis only (bỏ qua nếu chưa cấu hình) |
| `MemoryAndRedis` | Cả hai |

```csharp
services.AddGsLayeredCache(configuration);

// Đọc
var value = await cache.GetAsync<T>(key, CacheLookupStrategy.MemoryThenRedis);

// Đọc + fallback (tự cache kết quả fallback)
var value = await cache.GetAsync(
    key,
    CacheLookupStrategy.MemoryThenRedis,
    async ct => await FetchAsync(ct),
    CacheStorageTarget.MemoryAndRedis);

// Ghi / xóa
await cache.SetAsync(key, value, CacheStorageTarget.MemoryAndRedis);
await cache.ClearAsync(key);
```

Config (tùy chọn):

```json
{
  "LayeredCache": {
    "DefaultExpiration": "00:30:00"
  }
}
```

Redis: đăng ký `IDistributedCache` trước (vd. `AddStackExchangeRedisCache` trong `GS.MultiTenant`).

---

## StaleWhileRevalidateCache\<T\>

Cache SWR cho use case cần **refresh nền** khi entry stale (khác `ILayeredCache` dùng get/set/clear tường minh):

1. Trả cache ngay
2. Entry stale → refresh background
3. Refresh fail → giữ cache cũ

| Option | Mặc định |
|--------|----------|
| `AbsoluteExpiration` | 30 phút |
| `StaleThreshold` | 5 phút |

---

## Result Pattern

Dùng `Result` / `Result<T>` cho **lỗi nghiệp vụ** thay vì throw exception.

| Loại lỗi | Cách xử lý |
|----------|------------|
| Nghiệp vụ | `Result.Fail(...)` |
| Kỹ thuật | Throw exception |
| API (MVC) | `.ToActionResult()` |
| API (FastEndpoints) | `.SendResultAsync()` |

### Error helpers

| Factory | HTTP Status |
|---------|-------------|
| `Error.Validation(message)` | 400 |
| `Error.NotFound(message)` | 404 |
| `Error.Conflict(message)` | 409 |
| `Error.Unauthorized(message)` | 401 |

---

## HttpStatusException

```csharp
throw new HttpStatusException("Tenant code already exists.", 409);
```

Middleware `UseHttpStatusExceptionHandling()` bắt và trả JSON chuẩn. Dùng ở infrastructure/edge; business logic ưu tiên `Result`.

---

## Observability (Serilog + OpenTelemetry)

```csharp
builder.AddGsObservability(configureTracing: static t =>
    Npgsql.TracerProviderBuilderExtensions.AddNpgsql(t));

app.UseGsObservability();
app.RunWithObservability();
```

| Thuộc tính `Observability` | Mô tả |
|----------------------------|-------|
| `ServiceName` | Tên service trong trace/log |
| `OpenTelemetry.OtlpEndpoint` | Collector, vd. `http://localhost:4317` |
| `OpenTelemetry.ExportTraces` | Bật export traces |
| `OpenTelemetry.InstrumentEntityFrameworkCore` | Trace EF queries |

---

## API chính

| Method / Type | Mô tả |
|---------------|-------|
| `AddGsObservability()` | Serilog + OpenTelemetry |
| `AddGsJwtAuthentication()` | JWT Bearer validation |
| `AddGsMediatR(assembly)` | MediatR + FluentValidation pipeline |
| `AddGsLayeredCache()` | `ILayeredCache` |
| `SendResultAsync()` | FastEndpoints → JSON response |
| `ToActionResult()` | MVC → JSON response |
| `Result` / `Result<T>` | Functional error handling |
| `ILayeredCache` | Memory/Redis layered cache |
| `IPasswordHasherService` | Hash/verify password |

---

## Thư viện sử dụng GS.Core

| Project | Dùng gì từ Core |
|---------|-----------------|
| **GS.MultiTenant** | `ILayeredCache`, `HttpStatusException`, `AmbientContext` |
| **GS.TenantService** | `AddGsObservability`, `HttpStatusException` |
| **HMS.Identity** | JWT, MediatR, FastEndpoints, Result, PasswordHasher |
