# XUNG ĐỘT #100 - ESignController/Sign

## Hạng mục
port 1-1 ESignController/Sign từ 2020.6.QContract → MiniContract

## Hai nguồn mâu thuẫn

### 1. Source Excerpt (đề bài): Không có API controller
- File: `C:\Users\Public\Pictures\kientran_private\repos\MiniContract\.parity-run\logs\100_ESignController_Sign\source_excerpt.txt`
- Dòng 514: "API: khong thay controller ESignController.cs trong D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers"
- Dòng 501-502: Gate gọi các service NGOÀI: `WA_Common_GetDTime`, `WA_Contract_Contract_Get`, `WA_Contract_ContractElement_Update`, `WA_Contract_ContractUser_ConfirmOrgCreate`, `WA_Contract_ContractUser_ConfirmParty`, `WA_MstSv_Sys_User_Login`
- **Không có hàm `WA_ESign_*` ở tầng Biz.Web để port**

### 2. PORT_RULES § 6.1 (Nguồn sự thật)
- Chỉ dẫn: "Nguồn sự thật theo thứ tự: **mã nguồn gốc** (`source_excerpt.txt`) → **đề bài** → **luật này**"
- Luật § 1: "Khi toàn bộ tính toán/dữ liệu nằm ở dịch vụ ngoài ... PHẢI ghi NO_BACKEND.flag + conflicts\<số>_conflict.md, TUYỆT ĐỐI KHÔNG tự chế"

### 3. Thực tế gate ESignController.Sign (dòng 2603-2823)
- Gate **MVC** (không REST), nhận `ContractModel`, `accessToken`, `networkId`
- **Trực tiếp gọi dịch vụ ngoài:**
  - iNOS.AccountService → GetCurrentUser()
  - iNOS.OrgService → GetMyOrgUserList()
  - CommonService.WA_Common_GetDTime() (service)
  - Contract_ContractService.WA_Contract_Contract_Get() (service)
  - **ESign.SignFileNew()** ← Thư viện ký số bên ngoài (không có mã nguồn ở excerpt)
  - Contract_ContractUserService.WA_Contract_ContractUser_ConfirmOrgCreate/ConfirmParty() (service)
- **Không có hàm Biz tương ứng**: Gate không gọi `ClientService.WA_ESign_*`, mà gọi trực tiếp các service/library → không có `Biz.Web\Controllers\ESignController.cs` → **KHÔNG CÓ API TO PORT**

## Điều đã thử
1. Grep trong source_excerpt: không tìm thấy `ESignController.cs` ở `Biz.Web\Controllers` (dòng 514)
2. Grep repo đích: thấy `ESignControllerPort.cs` đã có, nhưng chỉ port các action `Contract_ContractParty_Cancel`, `ShowModalPopupPartySign_QRCode`, `ReturnResultPage` — **không có action `Sign`**
3. Kiểm tra gate: mã logic chủ yếu là gọi service → bước xử lý ký số (`DoSigningServer()`) gọi `ESign.SignFileNew()` là **NGOÀI repo Biz**

## Tình huống
- **Gate có**: ESignController.Sign (MVC, dòng 2603-2823)
- **API/Biz không có**: không tìm thấy hàm `WA_ESign_Sign` hay tương tự → **THIẾU SOURCE BACKEND**
- **Luật**: Khi API/Biz thiếu → không port, ghi GAP, `TRANG_THAI: CAN_XEM_TAY`
- **Yêu cầu**: Port 1-1 → phải có backend, nếu không là trường hợp khác (proxy-only, skip, v.v.)

## Đề xuất
1. **KIẾN NGHỊ 1** (thực hiện ngay): ghi `NO_BACKEND.flag`, kết thúc với `TRANG_THAI: CAN_XEM_TAY`
   - Gate ESignController.Sign là MVC action gọi trực tiếp các service, không có API controller Biz tương ứng
   - ESign.SignFileNew() là thư viện bên ngoài không có code ở excerpt
   - Cần quyết định: bỏ qua, hoặc port thành "proxy" mà không có logic ký số ở backend

2. **KIẾN NGHỊ 2**: Xác minh với Lead/PO:
   - Hạng mục này có phải là "gateway → service" mà thiếu core logic ở backend không?
   - Có nên bỏ qua (#100) hay port thành action nhận request → forward to external ESign service (mà không tái tạo logic)?

## Tham khảo
- PORT_RULES § 1: "Phải đọc TRỌN cả chuỗi [gate → API → biz → SQL]"
- PORT_RULES § 3, "GAP": "Ghi GAP + TRANG_THAI: CAN_XEM_TAY" khi backend không có
- LESSONS_AGENTS.md § B.7: "Thiếu nguồn backend = không port"
