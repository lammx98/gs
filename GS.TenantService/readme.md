# GS.TenantService



Master data service quản lý cấu hình tenant. Các microservice khác lấy thông tin tenant qua `TenantServiceBaseUrl` trong GS.MultiTenant.



## Database — PostgreSQL



Cấu hình connection string trong `appsettings.json` hoặc biến môi trường:



```json

{

  "ConnectionStrings": {

    "TenantDb": "Host=localhost;Port=5432;Database=gs-tenant;Username=postgres;Password=your_password"

  }

}

```



Hoặc override qua env:



```bash

ConnectionStrings__TenantDb="Host=...;Port=5432;Database=gs-tenant;Username=...;Password=..."

```



### Migration



Service tự chạy `MigrateAsync()` khi khởi động. Bạn cũng có thể apply thủ công:



```bash

dotnet ef database update --project GS.TenantService

```



Tạo migration mới (khi thay đổi model):



```bash

dotnet ef migrations add <MigrationName> --project GS.TenantService

```



Lần đầu chạy sẽ tạo bảng `Tenants` và seed 3 tenant mẫu (`acme`, `beta`, `vipcare`) nếu bảng trống.



## Chạy

```bash
dotnet run --project GS.TenantService
```

Service lắng nghe tại `http://localhost:5100` (xem `Properties/launchSettings.json`).

## Observability (Serilog + OpenTelemetry)

TenantService dùng observability từ **GS.Core** — cấu hình qua `appsettings.json`, không hard-code trong code.

### `Program.cs`

```csharp
builder.AddGsObservability(configureTracing: tracing => tracing.AddNpgsql());

app.UseHttpStatusExceptionHandling();
app.UseGsObservability();

app.RunWithObservability();
```

| Extension | Vai trò |
|-----------|---------|
| `AddGsObservability()` | Serilog + OpenTelemetry (ASP.NET Core, HttpClient, EF Core, OTLP export) |
| `AddNpgsql()` | Trace PostgreSQL queries (TenantService-specific) |
| `UseGsObservability()` | Serilog request logging |
| `RunWithObservability()` | Flush log khi shutdown |

### Cấu hình `appsettings.json`

```json
{
  "Observability": {
    "ServiceName": "GS.TenantService",
    "OpenTelemetry": {
      "Enabled": true,
      "OtlpEndpoint": "http://localhost:4317",
      "ExportTraces": true,
      "ExportMetrics": true,
      "InstrumentEntityFrameworkCore": true
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Application} {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [ "FromLogContext" ]
  }
}
```

Override OTLP endpoint qua env: `Observability__OpenTelemetry__OtlpEndpoint=http://collector:4317`

Chi tiết đầy đủ: [GS.Core/readme.md](../GS.Core/readme.md#observability-serilog--opentelemetry).

## API



| Method | Route | Mô tả |

|--------|-------|-------|

| `GET` | `/api/tenants/{tenantCode}` | **Endpoint chính** — GS.MultiTenant gọi khi resolve tenant |

| `GET` | `/api/tenants` | Danh sách tất cả tenant |

| `GET` | `/api/tenants/id/{tenantId}` | Tra cứu theo GUID nội bộ |

| `POST` | `/api/tenants` | Tạo tenant mới |

| `PUT` | `/api/tenants/{tenantCode}` | Cập nhật tenant |

| `DELETE` | `/api/tenants/{tenantCode}` | Soft delete (IsActive = false) |



### Response mẫu (`GET /api/tenants/acme`)



```json

{

  "tenantId": "11111111-1111-1111-1111-111111111111",

  "tenantCode": "acme",

  "tenantName": "Acme Clinic",

  "tier": 0,

  "connectionString": null

}

```



## Kết nối với microservice khác



Trong `appsettings.json` của service nghiệp vụ:



```json

{

  "MultiTenant": {

    "TenantServiceBaseUrl": "http://localhost:5100",

    "TenantServiceEndpointTemplate": "/api/tenants/{tenantCode}"

  }

}

```



## Seed data



| TenantCode | Tier | ConnectionString |

|------------|------|------------------|

| `acme` | Basic | null (shared DB) |

| `beta` | Standard | null (shared DB) |

| `vipcare` | VIP | dedicated connection string |



## Quy tắc nghiệp vụ



- `TenantCode` unique, normalize lowercase

- Tier `VIP` bắt buộc có `ConnectionString`

- Xóa là soft delete — tenant inactive không trả về qua GET resolve

