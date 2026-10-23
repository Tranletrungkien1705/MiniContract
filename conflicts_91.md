# XUNG ĐỘT #91 - DashboardRptController/DashboardLicense

## Hạng mục
Port 1-1 MiniContract: DashboardRptController.DashboardLicense (gate) → không có API/biz tương ứng

## Mâu thuẫn
Theo source_excerpt.txt:
- **Gate tồn tại**: `D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\DashboardRptController.cs:95-162`
- **Service được gọi**: `WA_Rpt_ContractForDashboard` (line 143)
- **API controller không tồn tại**: Grep `DashboardRptController.cs` trong `D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers` → không có
- **Hàm biz không tồn tại**: Grep `WA_Rpt_ContractForDashboard`, `Rpt_ContractForDashboard`, `WAS_Rpt_ContractForDashboard` → không có
- **Model không tồn tại**: `Rpt_ContractForDashboard`, `Invoice_license` không tìm thấy trong `D:\idocNet\2020.6.QContract\12.Dev.Common`

## Luật áp dụng
PORT_RULES_MiniContract.md mục 1 (Luật chung):
> "Lệnh gọi **dịch vụ ngoài** (ký số ESign, e-mail, SMS, iNOS, VNPay, `RestClient`…) mà đích không có ⇒ KHÔNG bịa: ghi `GAP` + `TRANG_THAI: CAN_XEM_TAY`"

LESSONS_AGENTS.md § B.7:
> "Thiếu nguồn backend = không port. Nếu action ở gate chỉ gọi `Service.WA_X_*` sang dịch vụ ngoài và trong `idn.Loyalty.Biz`/`Biz.Web` KHÔNG có hàm `WA_X_*`/`X_*` tương ứng (grep toàn nguồn) thì KHÔNG đoán quy tắc/mã lỗi: ghi `conflicts\<số>_conflict.md` (như #159) và `TRANG_THAI: CAN_XEM_TAY`."

## Đã thử
1. Grep `Rpt_ContractForDashboard` trong `D:\idocNet\2020.6.QContract` → không có
2. Grep `Invoice_license` → không có  
3. Grep `WA_Rpt_ContractForDashboard` trong toàn `idn.Skycic.QContract.Biz` → không có
4. Grep `DashboardRpt` trong `Biz.Web\Controllers` → không có

## Đề xuất (chỉ là đề xuất)
- **Nếu bỏ qua (#159–#161 đã được Claude quyết định BO)**: Đánh dấu `TRANG_THAI: BO`, không code
- **Nếu port với GAP**: Tạo endpoint GET `/api/dashboard_rpt/license` (chỉ trả cấu trúc DTO rỗng hoặc mock), ghi `GAP: Toàn bộ logic biz WA_Rpt_ContractForDashboard không có ở source backend`

## Quyết định chớp nhoáng: Chờ Claude hoặc lead xác nhận (BO hay GAP+CAN_XEM_TAY)

---

**Tham khảo**:
- #159–#161 (GovIDType Create/Update/Delete) cũng gặp tình huống tương tự, Claude quyết: BO
- Nếu hạng mục này thuộc chuỗi báo cáo đặc biệt của dự án cũ (QContract), có thể service `WA_Rpt_*` nằm ở các module phụ ngoài excerpt (idn.Skycic.QContract.ReportModule…) — cần xác minh thêm
