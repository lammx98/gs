## TÀI LIỆU YÊU CẦU KỸ THUẬT: COMMON MULTI-TENANT LIBRARY

### 1. Mục tiêu cốt lõi (Core Objectives)

- **Đóng gói (Encapsulation):** Che giấu toàn bộ sự phức tạp của Finbuckle.MultiTenant và logic Hybrid Database khỏi các microservices nghiệp vụ.
- **Tính Nhất quán (Consistency):** Đảm bảo mọi microservice (Kế toán, CRM, Tồn kho...) đều xử lý định danh khách hàng, truy xuất dữ liệu và giao tiếp nội bộ theo đúng một chuẩn duy nhất.
- **Plug & Play:** Đảm bảo Developer phát triển các feature mới chỉ cần 1 dòng code để cấu hình thư viện là hệ thống tự động chạy đúng logic Multi-tenant.

---



### 2. Yêu cầu Chức năng (Functional Requirements - FR)

**FR01. Định danh và Phân giải Tenant (Tenant Resolution)**

- Thư viện phải có khả năng tự động trính xuất `TenantID` từ URL (dạng tenantCode.domain.com) hoặc từ JWT. Cần kiểm tra khớp `TenantID`  giữa JWT và URL
- Thư viện phải có khả năng tự động trích xuất `TenantID` từ HTTP Header (ví dụ: `X-Tenant-Id`) do API Gateway truyền vào.
- Thư viện phải có khả năng tự động trích xuất `TenantID` từ Message Headers của các Message Broker (RabbitMQ/Kafka) khi service đóng vai trò là Consumer (Worker xử lý ngầm).
- Thư viện phải có cơ chế ném lỗi (Throw Exception) rõ ràng (ví dụ: `400 Bad Request` hoặc `401 Unauthorized`) nếu một request yêu cầu TenantID nhưng không tìm thấy định danh.

**FR02. Quản lý và Lưu trữ Cấu hình Tenant (Tenant Store & Caching)**

- Thư viện phải định nghĩa một chuẩn `TenantModel` chung, bắt buộc chứa các thông tin: `TenantId`, `TenantName`, `Tier` (Gói cước), và `ConnectionString` (có thể null nếu dùng Shared DB).
- Thư viện phải có module gọi API nội bộ (HTTP hoặc gRPC) sang `Tenant Service / Identity Service` để lấy thông tin cấu hình của Tenant dựa trên ID nhận được.
- **Bắt buộc:** Thư viện phải tích hợp Distributed Cache (ví dụ: Redis) hoặc In-memory Cache để lưu trữ thông tin Tenant. Không được phép gọi sang `Tenant Service` trên mỗi request để tránh nghẽn mạng.

**FR03. Định tuyến Cơ sở dữ liệu lai (Hybrid Database Routing & Isolation)**

- Thư viện phải cung cấp một `BaseDbContext` mà mọi service đều phải kế thừa.
- **Logic Routing:** Tại runtime, `BaseDbContext` phải tự động đánh giá:
  - Nếu `ConnectionString` của Tenant có giá trị (Gói VIP) -> Tự động ghi đè chuỗi kết nối sang Dedicated DB.
  - Nếu `ConnectionString` rỗng/null (Gói Basic) -> Tự động sử dụng chuỗi kết nối mặc định (Shared DB).
- **Logic Isolation:** Với Shared DB, thư viện phải tự động chèn bộ lọc toàn cục (Global Query Filter: `WHERE tenant_id = current_tenant`) vào tất cả các Entity. Đảm bảo Developer không thể vô tình truy vấn chéo dữ liệu.

**FR04. Lan truyền Ngữ cảnh (Context Propagation)**

- **HTTP-to-HTTP:** Thư viện phải cung cấp một cơ chế (Delegating Handler/Interceptor) để khi Service A gọi API nội bộ sang Service B, `TenantID` hiện tại tự động được đính kèm vào Header của request gửi đi.
- **Event-Driven:** Thư viện phải cung cấp cơ chế chặn (Interceptor) trên Event Bus (MassTransit/Cap) để tự động nhét `TenantID` vào metadata của message khi Publish, và phục hồi ngữ cảnh Tenant khi Consume.

---



### 3. Yêu cầu Phi chức năng (Non-Functional Requirements - NFR)

- **NFR01 - Hiệu suất (Performance):** Quá trình phân giải Tenant và lấy thông tin cấu hình (từ cache) không được làm tăng quá `5ms` độ trễ (latency) của mỗi request.
- **NFR02 - Bảo mật dữ liệu (Data Security):** Không cho phép Developer vô hiệu hóa bộ lọc `tenant_id` ở tầng Application trừ khi được cấp quyền admin thông qua một Service/Interface chuyên biệt (ví dụ: `ITenantBypassService` dùng cho các job tổng hợp dữ liệu toàn hệ thống).
- **NFR03 - Khả năng chịu lỗi (Resilience):** Cấu hình cache phải có cơ chế Stale-while-revalidate (dùng lại cache cũ trong lúc chờ lấy dữ liệu mới) để nếu `Tenant Service` bị sập tạm thời, các request của Tenant đã được cache vẫn hoạt động bình thường.

---



### 4. Yêu cầu Trải nghiệm Lập trình viên (Developer Experience - DX)

- **DX01:** Việc khởi tạo tại `Program.cs` chỉ cần thực hiện qua các Extension methods cực ngắn gọn, ví dụ: `builder.Services.AddClinicMultiTenant(options => ...)`.
- **DX02:** Thư viện phải cung cấp một Interface (ví dụ: `ICurrentTenantAccessor`) có thể được Inject vào các Service/Controller để Developer dễ dàng lấy được thông tin `TenantID` hoặc `Tier` hiện tại để xử lý business logic (ví dụ: kiểm tra xem gói Basic có được dùng tính năng này không).
- **DX03:** Developer khi viết LINQ truy vấn DB chỉ cần quan tâm logic nghiệp vụ, hoàn toàn "mù" (agnostic) về việc dữ liệu đang nằm ở Shared DB hay Dedicated DB.

``