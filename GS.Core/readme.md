# GS.Core

Thư viện **primitives dùng chung** cho toàn bộ hệ sinh thái GS microservices. Cung cấp các building block nhất quán: xử lý lỗi nghiệp vụ, exception HTTP, observability, cache và ngữ cảnh ambient — để mọi service/service lib (như `GS.MultiTenant`) dùng cùng một chuẩn.

## Mục tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **Consistency** | Một chuẩn logging, tracing, error handling trên mọi service |
| **Safety** | Result Pattern giúp luồng nghiệp vụ rõ ràng, không throw tung tóe |
| **Plug & Play** | Khởi tạo qua extension methods ngắn gọn tại `Program.cs` |
| **Resilience** | Cache stale-while-revalidate giữ service hoạt động khi dependency sập |

## Phụ thuộc

| Package | Vai trò |
|---------|---------|
| `Microsoft.AspNetCore.App` | Middleware, MVC extensions |
| `Serilog.AspNetCore` | Structured logging |
| `OpenTelemetry.*` | Traces, metrics, OTLP export |

---

## Cấu trúc project

```
GS.Core/
├── Ambient/            AmbientContext<T>
├── Caching/            StaleWhileRevalidateCache<T>
├── Configuration/      ObservabilityOptions
├── Exceptions/         HttpStatusException
├── Extensions/         DI & pipeline extensions
├── Middleware/         HttpStatusExceptionMiddleware
└── Results/            Result, Result<T>, Error
```

---

## Cài đặt nhanh

### 1. Tham chiếu project

```xml
<ProjectReference Include="..\GS.Core\GS.Core.csproj" />
```

### 2. `appsettings.json`

```json
{
  "Observability": {
    "ServiceName": "GS.MyService",
    "OpenTelemetry": {
      "Enabled": true,
      "OtlpEndpoint": "http://localhost:4317",
      "ExportTraces": true,
      "ExportMetrics": true
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Application} {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### 3. `Program.cs`

```csharp
using GS.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddGsObservability();

// ... đăng ký services

var app = builder.Build();

app.UseGsObservability();
app.UseHttpStatusExceptionHandling();

app.RunWithObservability();
```

---

## Result Pattern (Functional Error Handling)

Dùng `Result` / `Result<T>` cho **lỗi nghiệp vụ** thay vì throw exception. Giúp luồng code an toàn, dễ đọc và compose qua `Bind` / `Map`.

### Nguyên tắc

| Loại lỗi | Cách xử lý |
|----------|------------|
| **Nghiệp vụ** (chưa thanh toán, không đủ quyền, không tìm thấy) | Trả `Result.Fail(...)` |
| **Kỹ thuật** (DB down, bug, timeout) | Throw exception |
| **API boundary** (controller) | `.ToActionResult()` hoặc `.ValueOrThrow()` |

### Ví dụ nghiệp vụ

```csharp
using GS.Core.Results;

public Result<DispenseOrder> DispenseMedicine(Patient patient)
{
    if (!patient.HasPaid)
    {
        return Result<DispenseOrder>.Fail(
            "PaymentRequired",
            "Bệnh nhân chưa thanh toán, không thể nhận thuốc.",
            statusCode: 402);
    }

    if (patient.Prescription is null)
    {
        return Result<DispenseOrder>.Fail(Error.NotFound("Không tìm thấy đơn thuốc"));
    }

    return Result<DispenseOrder>.Success(CreateOrder(patient));
}
```

### Railway-oriented (Bind chain)

```csharp
return await GetPatient(id)
    .Bind(p => ValidatePayment(p))
    .Bind(p => CreateDispenseOrder(p));
```

### Controller

```csharp
[HttpPost("{id}/dispense")]
public async Task<ActionResult<DispenseOrder>> Dispense(Guid id)
{
    return (await _service.DispenseAsync(id)).ToActionResult();
}
```

Response lỗi (HTTP 402):

```json
{
  "error": "PaymentRequired",
  "message": "Bệnh nhân chưa thanh toán, không thể nhận thuốc.",
  "errors": [
    { "code": "PaymentRequired", "message": "Bệnh nhân chưa thanh toán, không thể nhận thuốc." }
  ]
}
```

### Error helpers

| Factory | HTTP Status |
|---------|-------------|
| `Error.Validation(message)` | 400 |
| `Error.NotFound(message)` | 404 |
| `Error.Conflict(message)` | 409 |
| `Error.Forbidden(message)` | 403 |
| `Error.Unauthorized(message)` | 401 |
| `Error.Create(code, message, statusCode?)` | Tùy chỉnh |

### Combinators

| Method | Mô tả |
|--------|-------|
| `Map` | Biến đổi giá trị success |
| `Bind` | Nối chuỗi operation trả `Result` |
| `Tap` | Side-effect trên success, giữ nguyên result |
| `Ensure` | Thêm điều kiện, fail nếu không thỏa |
| `Match` | Pattern match success / failure |
| `Combine` | Gộp nhiều `Result`, trả tất cả lỗi |

---

## HttpStatusException

Exception map trực tiếp sang HTTP status code. Middleware `HttpStatusExceptionMiddleware` bắt và trả JSON chuẩn.

```csharp
using GS.Core.Exceptions;

throw new HttpStatusException("Tenant code already exists.", 409);
```

Response:

```json
{
  "error": "HttpStatusException",
  "message": "Tenant code already exists."
}
```

Dùng cho **infrastructure / tenant resolution** hoặc khi cần throw tại biên API. Business logic ưu tiên `Result`.

`Result.ToHttpStatusException()` chuyển `Error` đầu tiên sang `HttpStatusException` khi cần.

---

## Observability (Serilog + OpenTelemetry)

### Serilog

- Đọc cấu hình từ section `Serilog` trong `appsettings.json`
- Enrich mặc định: `Application`, `MachineName`, `EnvironmentName`, `ThreadId`, `FromLogContext`
- Fallback Console sink nếu chưa khai báo `WriteTo`

### OpenTelemetry

- Resource: `service.name`, `service.version`, `deployment.environment`
- **Traces:** ASP.NET Core, HttpClient, EF Core (tùy chọn) → OTLP
- **Metrics:** ASP.NET Core, HttpClient, Runtime → OTLP

Thêm instrumentation riêng cho service (ví dụ Npgsql):

```csharp
builder.AddGsObservability(configureTracing: static tracing =>
    Npgsql.TracerProviderBuilderExtensions.AddNpgsql(tracing));
```

Cấu hình qua section `Observability`:

| Thuộc tính | Mô tả |
|------------|-------|
| `ServiceName` | Tên service (mặc định: `ApplicationName`) |
| `ServiceVersion` | Version (mặc định: entry assembly) |
| `OpenTelemetry.Enabled` | Bật/tắt OTel |
| `OpenTelemetry.OtlpEndpoint` | Collector endpoint, ví dụ `http://localhost:4317` |
| `OpenTelemetry.ExportTraces` | Export traces |
| `OpenTelemetry.ExportMetrics` | Export metrics |
| `OpenTelemetry.InstrumentEntityFrameworkCore` | Bật EF Core tracing |

---

## StaleWhileRevalidateCache\<T\>

Cache hai tầng (Memory + Redis tùy chọn) với chiến lược **stale-while-revalidate**:

1. Trả cache ngay lập tức (latency thấp)
2. Nếu entry đã stale → refresh nền
3. Refresh thất bại → giữ cache cũ, log warning

Được `GS.MultiTenant` dùng cho `CachedTenantStore`.

```csharp
services.AddMemoryCache();
services.AddSingleton<StaleWhileRevalidateCache<TenantModel>>();

// Trong store:
var tenant = await _cache.GetOrCreateAsync(
    cacheKey,
    ct => _client.FetchAsync(tenantCode, ct),
    cancellationToken: cancellationToken);
```

| Option | Mặc định | Mô tả |
|--------|----------|-------|
| `AbsoluteExpiration` | 30 phút | TTL tối đa của entry |
| `StaleThreshold` | 5 phút | Sau thời gian này, trigger refresh nền |

---

## AmbientContext\<T\>

Giá trị ambient theo `AsyncLocal`, restore-on-dispose — phù hợp truyền ngữ cảnh qua async call stack (message handler, middleware scope).

```csharp
using GS.Core.Ambient;

private static readonly AmbientContext<string?> TenantId = new();

// Set trong filter / middleware:
using (TenantId.Set("3fa85f64-..."))
{
    await ProcessMessageAsync();
}

// Đọc ở bất kỳ đâu trong cùng async flow:
var id = TenantId.Value;
```

---

## API chính

| Method / Type | Mô tả |
|---------------|-------|
| `AddGsObservability()` | Serilog + OpenTelemetry |
| `UseGsObservability()` | Serilog request logging |
| `RunWithObservability()` | `app.Run()` + flush log |
| `UseHttpStatusExceptionHandling()` | Middleware bắt `HttpStatusException` |
| `Result` / `Result<T>` | Functional error handling |
| `Error` | Mô tả lỗi có cấu trúc |
| `ToActionResult()` | Map `Result` → MVC response |
| `StaleWhileRevalidateCache<T>` | Cache SWR |
| `AmbientContext<T>` | Async-local ambient value |

---

## Thư viện sử dụng GS.Core

| Project | Dùng gì từ Core |
|---------|-----------------|
| **[GS.MultiTenant](../GS.MultiTenant/readme.md)** | `StaleWhileRevalidateCache`, `HttpStatusException`, `AmbientContext` |
| **[GS.TenantService](../GS.TenantService/readme.md)** | `AddGsObservability`, `UseHttpStatusExceptionHandling` |

---

## Quy ước đề xuất

```
Controller  →  Service (Result<T>)  →  Repository
     ↑                ↓
 ToActionResult    Bind / Map
     ↑
HttpStatusException (chỉ ở edge hoặc infra)
```

- Service layer **luôn** trả `Result<T>` cho logic nghiệp vụ.
- Controller **không** chứa if/else nghiệp vụ — chỉ gọi service và map result.
- Exception dành cho lỗi không lường trước được (infrastructure, bug).
- Mọi microservice gọi `AddGsObservability()` để log và trace thống nhất.
