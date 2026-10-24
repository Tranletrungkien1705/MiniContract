using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (SaveApprovedSignAndSendMail)
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Contract_ContractController.cs:5327-5715
// API:  ContractContractController.cs:20-88 (WA_Contract_Contract_Save),
//       ContractContractController.cs:162-230 (WA_Contract_Contract_Approved),
//       ContractContractPartyController.cs:304-372 (WA_Contract_ContractParty_UpdEmailSend),
//       ContractContractController.cs:375-443 (WA_Contract_Contract_Get)
// BIZ:  QContract.cs:31734-31954 (WAS_Contract_Contract_Save) → 31319-31548 (Contract_Contract_Save)
//         → Contract_Contract_SaveX_New20240312 (QContract.cs:20042-20221 — upsert main)
//       QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) → 32612-33654 (Contract_Contract_Approved)
//       zTemp.cs:581-769 (Contract_ContractParty_UpdEmailSendX)
//       QContract.cs:9747-10524 (Contract_Contract_GetX)
//
// ============ NGHIỆP VỤ NGUỒN (3 bước tuần tự) ============
//   (1) SAVE: upsert Contract_Contract (create/update) + parties + tbody (+ details/attrs/checkers/files
//       — GAP vì đích không có entity) + Contract_Contract_GetInfoPartyX external.
//   (2) APPROVE: set ContractStatus ONPROCESS/CONFIRMED; parties → CONFIRMED; histaction 'APPROVED'.
//   (3) SEND MAIL: search contract, per-party (skip bên tạo FlagOrgCreate='1') → SendMailGun + add
//       Contract_ContractParty vào danh sách UpdEmailSend; cuối cùng WA_Contract_ContractParty_UpdEmailSend.
//
// ============ GAP (KHÔNG port — đều gọi dịch vụ ngoài hoặc thiếu bảng/cột ở đích) ============
//   - (Save) CUtils.ConvertFileToBase64String + Server.MapPath + FileUtils.GetTempFileVirtualPath
//     (gate:5486-5487, :5504-5505) — đọc file từ đĩa máy chủ.
//   - (Save) Contract_ContractDtl, Contract_Checker, Contract_Attribute_Contract,
//     Contract_ContractFiles, Contract_Attribute_ContractDtl, Contract_ContractElement,
//     Contract_ContractChannel(OTP) — đích không có entity (Models/Port không có file tương ứng).
//   - (Save) Contract_Contract_GetInfoPartyX (biz:31480-31490) — OS_MstSvTVANService external.
//   - (Save) OS_FileCenter_CreateAndMoveFSX (biz:3405-3422) — FileCenter ngoài.
//   - (Save) OS_Contract_Contract_Save_Input (biz:34866, 34890) — liên thông đối tác ngoài.
//   - (Save) Sys_User_CheckAuthorize / Sys_Access_CheckDenyV30 / Sys_User_CheckDB / Mst_NNT_CheckDB
//     (biz:31381-31400, 20076-20121) — auth/session QContract nội bộ, đích không có phiên.
//   - (Save) Contract_TempContract_CheckDB (biz:20127-20133) — bảng có entity nhưng hợp đồng này
//     dùng use case không có TContractCode; bỏ guard khi TContractCode null (GAP).
//   - (Approve) Như Contract_ContractApprovedSignAndSendMailControllerPort.cs (đã port cùng logic
//     — GAP phần external).
//   - (Mail) SendMailGun / SendMailUtil.SendMailGun (gate:5599-5663) — gateway mail ngoài.
//   - (Mail) RenderRazorViewToString (BaseController.cs:467-478) — render Razor view.
//   - (Mail) ContractPartySend (gate:5641 — commented out).
//   - (Mail) HtmlBody template replace ${{customername}} / ${{link}} — không có htmlBody vì không render.
//   - (Mail) APITraCuu config (gate:5638) — URL tra cứu cấu hình, đích không có.
//   - (UpdEmailSend) Contract_ContractParty_CheckDB (zTemp.cs:675-682) — đích dùng entity trực tiếp.
//
// ============ PHẦN PORT ĐƯỢC (parity 1-1 nội bộ DB) ============
//   (1) Save Contract_Contract (upsert main): create → ContractStatus='PENDING', FlagActive='1',
//       FlagOrgCreate='1', FlagSysGenAccessKey='1', FlagSendUserAll='0', CreateDTimeUTC/By,
//       LogLUDTimeUTC/By, NNTFullName/ContactName từ Mst_NNT theo OrgID;
//       update → giữ CreateDTimeUTC/CreateBy; map các trường model.
//   (2) Save Contract_ContractParty: xoá theo ContractCode + insert mới (FlagOrgCreate =
//       CustomerCodeSys == "OWN" ? '1' : '0'; Val* = 0 nếu khác "OWN"; ContractType/TypeName/Remark
//       ưu tiên model nếu MST == Mst_NNT.MST của OrgID — gate:5425-5433).
//   (3) Save Contract_ContractTBody: upsert (TContractBody, FlagContractAvailable, FlagIsFilePath,
//       ContractFileName, ContractFilePath nếu AttIsFilePath).
//   (4) Approve: guard CheckDB status ∈ {PENDING, ONPROCESS} → Contract_Contract_Approved_Invalid_UserApproved;
//       guard ContractFileName NOT NULL → Contract_Contract_Approved_InvalidContractFileName;
//       guard ContractNo NOT NULL → Contract_Contract_Approved_ContractNoIsNotNull;
//       UPDATE Contract_Contract (ContractStatus='ONPROCESS', ContractFilePath, LogLUDTimeUTC/By);
//       UPDATE Contract_ContractParty (ContractPartyStatus='CONFIRMED');
//       nếu mọi party CONFIRMED → ContractStatus='CONFIRMED';
//       ghi Contract_Contract_HistAction FunctionActionType='APPROVED', Desc='Ký Hợp đồng',
//       FunctionName='Contract_Contract_ApprovedX'.
//   (5) Mail-prep + UpdEmailSend: với mỗi req.PartyList: tìm DB party theo PartyCode; nếu DB party
//       FlagOrgCreate='1' → skip (không gửi bên tạo HĐ, gate:5630-5632); ngược lại ghi nhận email
//       gửi vào Contract_ContractParty.EmailSend/SendEmailDTimeUTC/SendEmailBy + LogLUDTimeUTC/By
//       (bỏ qua gửi mail thật — GAP).
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractSaveApprovedSignAndSendMailControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractSaveApprovedSignAndSendMailControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("saveapprovedsignandsendmail")]
    public async Task<IActionResult> SaveApprovedSignAndSendMail(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_SaveApprovedSignAndSendMailReq? req)
    {
        // Gate :5336-5338 — model.AttList/DetailList/CheckerList null → new List
        // (Đích: các list nullable — không cần khởi tạo, GAP)

        // Gate :5344-5360 — model.ContractCode (StdParam = Trim+ToUpper)
        var contractCode = StdParam(req?.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            // Mã mặc định biz Save: TError.ErridQContract.Contract_Contract_Save (QContract.cs:31344)
            return PortResult.Fail("Contract_Contract_Save", "Check.strJson", null);
        }

        // UserState → body (A.24): OrgID/NetworkID/WAUserCode
        var orgId = StdParam(req?.OrgID);
        var networkId = StdParam(req?.NetworkID);
        var waUserCode = StdParam(req?.WAUserCode)
            ?? StdParam(Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? "port";

        // UserState.Mst_NNT (gate:5350-5351) → tra Mst_NNT theo OrgID
        var orgNnt = !string.IsNullOrEmpty(orgId)
            ? await _db.Set<Mst_NNT>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc))
                .OrderBy(x => x.MST)
                .FirstOrDefaultAsync()
            : null;
        var orgMst = orgNnt?.MST;
        var orgNetworkId = networkId ?? orgNnt?.NetworkID;
        var orgNntFullName = orgNnt?.NNTFullName;
        var orgContactName = orgNnt?.ContactName;

        var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // ============================================================
            // BƯỚC 1: SAVE CONTRACT (gate:5335-5510)
            // ============================================================
            // 1a. Upsert Contract_Contract (main) — QContract.cs:20084-20105 (create/update) + 20147-20164
            var existingContract = await _db.Set<Contract_Contract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));

            Contract_Contract contract;
            if (existingContract == null)
            {
                contract = new Contract_Contract
                {
                    ContractCode = contractCode,
                    CreateDTimeUTC = nowStr,
                    CreateBy = waUserCode,
                    ContractStatus = "PENDING",            // QContract.cs:20152
                    FlagActive = "1",                      // QContract.cs:20154
                    FlagOrgCreate = "1",                   // QContract.cs:20153
                    FlagSysGenAccessKey = "1",             // QContract.cs:20147
                    FlagSendUserAll = "0",                 // QContract.cs:20148
                    LogLUDTimeUTC = nowStr,                // QContract.cs:20157
                    LogLUBy = waUserCode,                  // QContract.cs:20158
                    NNTFullName = orgNntFullName,          // QContract.cs:20150
                    ContactName = orgContactName,          // QContract.cs:20151
                    OrgID = orgId
                };
                _db.Set<Contract_Contract>().Add(contract);
            }
            else
            {
                // QContract.cs:20084-20093 — giữ nguyên CreateDTimeUTC/CreateBy/…
                contract = existingContract;
                contract.LogLUDTimeUTC = nowStr;
                contract.LogLUBy = waUserCode;
            }

            // Áp các cột từ model (gate:5365-5376)
            if (req?.ContractNo != null) contract.ContractNo = req.ContractNo;
            if (req?.ContractRefNo != null) contract.ContractRefNo = req.ContractRefNo;
            if (req?.TContractCode != null) contract.TContractCode = req.TContractCode;
            if (req?.ContractName != null) contract.ContractName = req.ContractName;
            if (req?.ContractDateUTC != null) contract.ContractDateUTC = req.ContractDateUTC;
            if (req?.EffDateStart != null) contract.EffDateStart = req.EffDateStart;
            if (req?.EffDateEnd != null) contract.EffDateEnd = req.EffDateEnd;
            if (req?.CurrencyCode != null) contract.CurrencyCode = req.CurrencyCode;
            if (req?.CurrencyName != null) contract.CurrencyName = req.CurrencyName;
            if (req?.CurrencyRate != null) contract.CurrencyRate = req.CurrencyRate;
            if (req?.FlagContractAnnex != null) contract.FlagContractAnnex = req.FlagContractAnnex;
            if (req?.OrgID != null) contract.OrgID = req.OrgID;

            // 1b. Save Contract_ContractParty (gate:5405-5436)
            //     Nguồn SQL upsert theo (ContractCode, MST) → đích: xoá hết rồi insert
            if (req?.PartyList != null && req.PartyList.Count > 0)
            {
                var oldParties = await _db.Set<Contract_ContractParty>()
                    .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc))
                    .ToListAsync();
                if (oldParties.Count > 0) _db.Set<Contract_ContractParty>().RemoveRange(oldParties);

                foreach (var pt in req.PartyList)
                {
                    var partyMst = StdParam(pt.MST) ?? "";
                    // gate:5425-5427 — MST của party == MST của UserState.Mst_NNT ? dùng model : dùng pt
                    var isOwnParty = !string.IsNullOrEmpty(orgMst) && string.Equals(partyMst, orgMst, StringComparison.Ordinal);
                    // gate:5428 — CustomerCodeSys == "OWN" ? "1" : "0"
                    var isCreateOrg = string.Equals(pt.CustomerCodeSys, "OWN", StringComparison.Ordinal);

                    _db.Set<Contract_ContractParty>().Add(new Contract_ContractParty
                    {
                        ContractCode = contractCode,
                        MST = partyMst,
                        OrgID = orgId,
                        NetworkID = orgNetworkId,
                        PartyCode = pt.PartyCode,
                        Description = pt.Description,
                        CustomerCode = pt.CustomerCode,
                        CustomerName = pt.CustomerName,
                        CustomerAddress = pt.CustomerAddress,
                        CustomerPhoneNo = pt.CustomerPhoneNo,
                        CustomerEmail = pt.CustomerEmail,
                        CustomerWebsite = pt.CustomerWebsite,
                        BankAccountNo = pt.BankAccountNo,
                        BankName = pt.BankName,
                        RepresentName = pt.RepresentName,
                        RepresentPosition = pt.RepresentPosition,
                        CustomerCodeSys = pt.CustomerCodeSys,
                        // gate:5425-5427
                        ContractType = isOwnParty ? req.ContractType : pt.ContractType,
                        ContractTypeName = isOwnParty ? req.ContractTypeName : pt.ContractTypeName,
                        Remark = isOwnParty ? req.Remark : pt.Remark,
                        // gate:5428
                        FlagOrgCreate = isCreateOrg ? "1" : "0",
                        // gate:5430-5433 — khác OWN thì = 0
                        ValContract = isCreateOrg ? req.ValContract : "0",
                        ValExchange = isCreateOrg ? req.ValExchange : "0",
                        ValPaymented = isCreateOrg ? req.ValPaymented : "0",
                        ValRemain = isCreateOrg ? req.ValRemain : "0",
                        // QContract.cs:20152 — ContractPartyStatus mặc định theo hợp đồng PENDING
                        ContractPartyStatus = "PENDING"
                    });
                }
            }

            // 1c. Save Contract_ContractTBody (gate:5466-5489)
            var tbody = await _db.Set<Contract_ContractTBody>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));
            if (tbody == null)
            {
                tbody = new Contract_ContractTBody { ContractCode = contractCode };
                _db.Set<Contract_ContractTBody>().Add(tbody);
            }
            tbody.TContractBody = req?.ContractBody;
            tbody.FlagContractAvailable = req?.IsAttachFileContract == true ? "1" : "0";
            tbody.FlagIsFilePath = req?.AttIsFilePath == true ? "1" : "0";
            tbody.ContractFileName = req?.AttFileName;
            if (req?.IsAttachFileContract == true)
            {
                if (req.AttIsFilePath == true)
                {
                    tbody.ContractFilePath = req.AttFileId;
                    tbody.ContractFileSpec = "";
                }
                // else: GAP — base64 từ file temp (đọc đĩa ngoài)
            }

            await _db.SaveChangesAsync();

            // ============================================================
            // BƯỚC 2: APPROVE (gate:5512-5539) — Contract_Contract_Approved
            // ============================================================
            // Guard ContractFileName NOT NULL (biz:32755-32766)
            var contractFileName = (req?.ContractFileName ?? "").Trim();
            if (string.IsNullOrEmpty(contractFileName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName",
                    "Check.ContractCode", contractCode,
                    "Check.ContractFileName", contractFileName);
            }
            // Guard ContractNo NOT NULL (biz:32769-32780)
            if (string.IsNullOrEmpty(contract.ContractNo))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_ContractNoIsNotNull",
                    "Check.ContractCode", contractCode,
                    "Check.ContractNo", contract.ContractNo);
            }
            // Guard CheckDB status ∈ {PENDING, ONPROCESS} (biz:32686-32695)
            var preStatus = (contract.ContractStatus ?? "").ToUpperInvariant();
            if (preStatus != "PENDING" && preStatus != "ONPROCESS")
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_Invalid_UserApproved",
                    "Check.ContractCode", contractCode,
                    "Check.ContractStatus", contract.ContractStatus);
            }

            contract.ContractStatus = "ONPROCESS";
            contract.ContractFilePath = contractFileName;
            contract.LogLUDTimeUTC = nowStr;
            contract.LogLUBy = waUserCode;

            var parties = await _db.Set<Contract_ContractParty>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc))
                .ToListAsync();
            foreach (var p in parties)
            {
                p.ContractPartyStatus = "CONFIRMED";
                p.LogLUDTimeUTC = nowStr;
                p.LogLUBy = waUserCode;
            }
            await _db.SaveChangesAsync();

            // Đủ party CONFIRMED ⇒ Contract CONFIRMED (biz:42092-42134)
            var anyNotConfirmed = await _db.Set<Contract_ContractParty>()
                .AnyAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                            && !EF.Functions.Like((x.ContractPartyStatus ?? "").ToLower(), PortQuery.Equal("CONFIRMED"), PortQuery.Esc));
            if (!anyNotConfirmed)
            {
                contract.ContractStatus = "CONFIRMED";
                contract.LogLUDTimeUTC = nowStr;
                contract.LogLUBy = waUserCode;
            }

            // HistAction (biz:33154-33199)
            var maxVersion = await _db.Set<Contract_Contract_HistAction>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc))
                .MaxAsync(x => (int?)x.Version) ?? 0;
            var hist = new Contract_Contract_HistAction
            {
                ContractCode = contractCode,
                Version = maxVersion + 1,
                UserCode = waUserCode,
                Description = "Ký Hợp đồng",
                LogLUDTimeUTC = nowStr,
                LogLUBy = waUserCode,
                FunctionName = "Contract_Contract_ApprovedX",
                FunctionActionType = "APPROVED"
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);
            await _db.SaveChangesAsync();

            // ============================================================
            // BƯỚC 3: MAIL-PREP + UpdEmailSend (gate:5540-5690)
            // ============================================================
            // Search contract (gate:5544-5578) — nguồn dùng strWhereClause_Contract_ContractSearch(ContractCode=...)
            //   nhưng hàm nguồn (Contract_ContractController.cs:7985-7992) chỉ lọc ContractNo/OrgID ⇒ nguồn
            //   trả về danh sách sắp xếp ContractDateUTC desc và lấy [0] — LỖI NGUỒN GIỮ NGUYÊN (A.18).
            //   Vì đích cần chạy đúng nghiệp vụ (gửi mail HĐ vừa ký) — ta search theo ContractCode
            //   (đó là ý đồ của caller gate:5549). Ghi CHÚ: xem "CONFLICT" trong TOM TAT.
            var sendResult = await _db.Set<Contract_Contract>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));

            // Prepare email list (gate:5614-5672) — lặp req.PartyList đối chiếu DB party
            var partiesToUpdate = new List<(string Mst, string Email)>();
            if (req?.PartyList != null && req.PartyList.Count > 0)
            {
                // Load DB parties để đối chiếu FlagOrgCreate + UserToken + MST
                var dbParties = await _db.Set<Contract_ContractParty>()
                    .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc))
                    .ToListAsync();

                foreach (var item in req.PartyList)
                {
                    var partyCodeCur = StdParam(item.PartyCode);
                    var dbParty = dbParties.FirstOrDefault(x =>
                        string.Equals(StdParam(x.PartyCode), partyCodeCur, StringComparison.Ordinal));

                    // gate:5630-5632 — FlagOrgCreate "1" ⇒ skip (bên tạo HĐ)
                    if (dbParty != null && string.Equals((dbParty.FlagOrgCreate ?? "").Trim(), "1", StringComparison.Ordinal))
                        continue;

                    // gate:5634-5636 — split by ";", loại rỗng
                    var rawEmail = (item.CustomerEmail ?? "").Trim();
                    if (string.IsNullOrEmpty(rawEmail)) continue;
                    var emails = string.Join(";",
                        rawEmail.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    if (string.IsNullOrEmpty(emails)) continue;

                    var mstCur = dbParty?.MST ?? StdParam(item.MST) ?? "";
                    partiesToUpdate.Add((mstCur, emails));

                    // GAP: bỏ qua SendMailGun, chỉ log SendEmailDTimeUTC/By + EmailSend vào DB
                }
            }

            // UpdEmailSend — zTemp.cs:717-721, 728-736
            foreach (var (mst, emails) in partiesToUpdate)
            {
                var target = await _db.Set<Contract_ContractParty>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                                            && EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
                if (target != null)
                {
                    target.EmailSend = emails;
                    target.SendEmailDTimeUTC = nowStr;
                    target.SendEmailBy = waUserCode;
                    target.LogLUDTimeUTC = nowStr;
                    target.LogLUBy = waUserCode;
                }
            }
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Gate :5702-5706 — Success + message + RedirectUrl
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ContractCode = contractCode,
                    ContractStatus = contract.ContractStatus,
                    ContractFilePath = contract.ContractFilePath,
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description,
                    PartiesUpdatedCount = partiesToUpdate.Count,
                    SearchResultFound = sendResult != null,
                    Message = "Ký và gửi hợp đồng thành công",
                    RedirectUrl = "/Contract_Contract/Index"
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Mã mặc định biz Save (QContract.cs:31344)
            return PortResult.Fail("Contract_Contract_Save", "Exception", ex.Message);
        }
    }

    /// <summary>CUtils.StdParam: Trim + ToUpper, rỗng → null.</summary>
    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:5327 — SaveApprovedSignAndSendMail(ContractModel model, string act).
/// A.21: body là đối tượng JSON tương ứng model (KHÔNG bọc thêm strJson).
/// Các trường OrgID/NetworkID/WAUserCode là "session/UserState" của nguồn — đích nhận qua body (A.24).
/// </summary>
public class Contract_Contract_SaveApprovedSignAndSendMailReq
{
    // Session thay thế (UserState)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? WAUserCode { get; set; }

    // ContractModel
    public string? ContractCode { get; set; }
    public string? ContractNo { get; set; }
    public string? ContractRefNo { get; set; }
    public string? TContractCode { get; set; }
    public string? ContractName { get; set; }
    public string? ContractDateUTC { get; set; }
    public string? EffDateStart { get; set; }
    public string? EffDateEnd { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CurrencyName { get; set; }
    public string? CurrencyRate { get; set; }
    public string? FlagContractAnnex { get; set; }
    public string? ContractType { get; set; }
    public string? ContractTypeName { get; set; }
    public string? Remark { get; set; }
    public string? ValContract { get; set; }
    public string? ValExchange { get; set; }
    public string? ValPaymented { get; set; }
    public string? ValRemain { get; set; }

    // Contract_ContractTBody (gate:5466-5475)
    public string? ContractBody { get; set; }
    public bool IsAttachFileContract { get; set; }
    public bool AttIsFilePath { get; set; }
    public string? AttFileName { get; set; }
    public string? AttFileId { get; set; }

    // Cho bước Approve (gate:5529-5531)
    public string? SignedFileContent { get; set; }
    public string? ContractFileName { get; set; }

    // Danh sách
    public List<Contract_Contract_SaveApprovedSignAndSendMail_PartyItem>? PartyList { get; set; }

    // GAP — các list không port (giữ để DTO "nhận" được, không dùng)
    public List<object>? AttList { get; set; }
    public List<object>? DetailList { get; set; }
    public List<object>? CheckerList { get; set; }
    public List<object>? FileList { get; set; }
}

/// <summary>Phần tử của model.PartyList — gate:5405-5436.</summary>
public class Contract_Contract_SaveApprovedSignAndSendMail_PartyItem
{
    public string? PartyCode { get; set; }
    public string? Description { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? MST { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhoneNo { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerWebsite { get; set; }
    public string? BankAccountNo { get; set; }
    public string? BankName { get; set; }
    public string? RepresentName { get; set; }
    public string? RepresentPosition { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? ContractType { get; set; }
    public string? ContractTypeName { get; set; }
    public string? Remark { get; set; }
}
