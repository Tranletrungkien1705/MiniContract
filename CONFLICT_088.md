# XUNG DOT #088 - DashboardController/DashboardSummary

## Hang muc
`DashboardController/DashboardSummary` — port 1-1 MiniContract

## Phat hien xung dot

### Khuong nguon + De bai vs Su that
- **De bai**: "port 1-1 MiniContract #88 DashboardController/DashboardSummary"
- **Chuan tren PORT_RULES**: Endpoint port phai la API (route `/api/<controller>/<action>`, response `PortResult/ServiceResult`)
- **Su that trai chinh (source_excerpt.txt:4-82)**:
  - Gate (DashboardController.DashboardSummary, line 81-144) la **MVC Action, tra View HTML** (`return View(...)`)
  - KHONG co API Controller trong nguon (API: `khong thay controller DashboardController.cs trong Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers`)
  - Gate goi **dich vu ngoai** `ReportService.Instance.WA_Rpt_ContractForDashboard()` (line 57) KHONG co implementation backend trong excerpt
  - Models `Rpt_ContractForDashboard` va `Invoice_license` KHONG co trong `12.Dev.Common` (source_excerpt.txt:75-81)

### Xung dot chinh tiet
1. **Loai action**: MVC (Server-side render) khong phai API REST
   - De bai: Port API endpoint ("/api/...")
   - Nguon gate: MVC action (ViewBag, View render, hien thi du lieu tren giao dien)
   - Quy tac §2 PORT_RULES: "endpoint port ... route `/api/<tên>` ... POST" — MVC action khong match

2. **Tieu huong dich vu ngoai**: Khong co backend trien khai
   - Gate goi dich vu bao cao: `ReportService.Instance.WA_Rpt_ContractForDashboard(...)`
   - Khong co file Biz trong excerpt chua ham nay
   - Khong co DDL, DDL tự tạo để định
   - → PORT_RULES §1 / §3 cấm "bịa giá trị" + A.16/B.7 LESSONS: "thiếu nguồn backend = không port"

3. **Khai bao gap qua tran da thoat` từ gate**
   - UserState, Session, GwUserCode/Password (tài khoản viết cứng), AddressAPIs
   - LESSONS A.20: hàm phụ của gate/BaseController không được "giả lập"
   - PORT_RULES §3 + §4 cấm chép/bịa tài khoản

### Nhan xet chi tiet
- Nguon: DashboardController.cs:81-144 luu `UserState` vao Session, extract GwUserCode/Password/AddressAPIs
- A.20 LESSONS: dữ liệu cấp từ BaseController (UserState, Session...) không có ở đích
- PORT_RULES: tài khoản cố định (GwUserCode, GwPassword) cấm chép ngay cả trong comment
- Kết quả: KHÔNG thể port mà tuân thủ luật

## Da thử
1. Đọc source_excerpt.txt: KHÔNG có API controller, KHÔNG có biz, KHÔNG có models
2. Grep comment trong nguồn: xác nhận gate gọi dịch vụ ngoài, không gọi hàm biz nội bộ

## De xuat
**BỎ HẠNG MỤC NÀY** (ghi vào `skipped.json`):
- Gate là MVC action, không phải API endpoint ⇒ không trong phạm vi "port 1-1 API"
- Dịch vụ báo cáo (WA_Rpt_ContractForDashboard) không có backend ⇒ A.16/B.7: "thiếu nguồn backend = không port"
- Tài khoản viết cứng + Session state ⇒ không tuân thủ PORT_RULES §3 + LESSONS A.24

**Hoặc** nếu yêu cầu phải port:
- Lead phải cung cấp định nghĩa hàm `WA_Rpt_ContractForDashboard`, DDL bảng Rpt_ContractForDashboard, mô tả chi tiết giá trị `AddressAPIs`, cách truyền session/auth ở đích
- Xác nhận port MVC action (HTML render) hay chỉ API (JSON return)

## Pham vi work hop le
Các hạng mục khác trên nhánh parity/w-agy (nếu có) không bị ảnh hưởng: vẫn có thể hoàn thành độc lập.
