## TÀI LIỆU YÊU CẦU KỸ THUẬT: COMMON MULTI-TENANT LIBRARY

### 1. Mục tiêu cốt lõi (Core Objectives)

- **Đóng gói (Encapsulation):** Che giấu toàn bộ sự phức tạp của Finbuckle.MultiTenant và logic Hybrid Database khỏi các microservices nghiệp vụ.
- **Tính Nhất quán (Consistency):** Đảm bảo mọi microservice đều xử lý định danh khách hàng, truy xuất dữ liệu và giao tiếp nội bộ theo đúng một chuẩn duy nhất.
- **Plug & Play:** Developer chỉ cần extension methods tại `Program.cs` để hệ thống tự chạy đúng logic Multi-tenant.

---

### 2. Yêu cầu Chức năng (Functional Requirements - FR)

**FR01. Định danh và Phân giải Tenant (Tenant Resolution)**

- Tự động trích xuất tenant từ URL (subdomain), JWT claim, HTTP Header (`X-Tenant-Id`), Message Headers.
- Kiểm tra khớp tenant giữa các nguồn có mặt (Header, URL, JWT).
- Ném exception rõ ràng khi thiếu hoặc xung đột tenant.

**FR02. Quản lý và Lưu trữ Cấu hình Tenant (Tenant Store & Caching)**

- `TenantModel` chuẩn: `TenantId`, `TenantCode`, `TenantName`, `Tier`, `UsesDedicatedDatabase`, `DatabaseHost`, `DatabasePort`, `CredentialsRef`.
- `ITenantResolutionService` gọi TenantService API khi cache miss.
- Cache qua `ILayeredCache` (GS.Core): Memory → Redis, keys `tenant:code:{code}` và `tenant:id:{id}`.
- Không gọi TenantService trên mỗi request khi đã cache.

**FR03. Định tuyến Cơ sở dữ liệu lai (Hybrid Database Routing & Isolation)**

- `TenantBaseDbContext` + `IConnectionStringResolver` (PostgreSQL).
- `UsesDedicatedDatabase = true` → build CS từ `DatabaseHost` + `{tenantCode}_{serviceName}` + credentials local.
- `UsesDedicatedDatabase = false` → `SharedDatabaseConnectionString` + global query filter `tenant_id`.
- Entity implement `ITenantEntity` để tự động áp filter trên shared DB.

**FR04. Lan truyền Ngữ cảnh (Context Propagation)**

- HTTP-to-HTTP: `TenantPropagationDelegatingHandler`.
- Event-Driven: MassTransit consume/publish filters.
- Worker: `TenantMessageContext`.

---

### 3. Yêu cầu Phi chức năng (Non-Functional Requirements - NFR)

- **NFR01 - Performance:** Cache in-memory trước, Redis sau — latency resolve tenant từ cache ≤ 5ms.
- **NFR02 - Data Security:** `ITenantBypassService` cho admin job; global filter không bypass tùy tiện.
- **NFR03 - Resilience:** Cache hit khi TenantService tạm sập; cold start (chưa cache) vẫn phụ thuộc TenantService.

---

### 4. Yêu cầu Trải nghiệm Lập trình viên (Developer Experience - DX)

- **DX01:** `AddMultiTenantServices(configuration)` tại `Program.cs`.
- **DX02:** `ICurrentTenantAccessor` inject vào service/handler.
- **DX03:** Developer không cần biết shared vs dedicated DB — `TenantBaseDbContext` tự route.
