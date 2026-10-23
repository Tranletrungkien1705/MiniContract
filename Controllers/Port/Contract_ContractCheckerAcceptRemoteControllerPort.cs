using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (Contract_Checker_Accept_Remote)
// Gate:  Contract_ContractController.cs:1796-1981
// API:   ContractCheckerController.cs:20-88 (WA_Contract_Checker_Accept)
// BIZ:   zTemp.cs:6046-6124 (WAS_Contract_Checker_Accept) -> 5887-6016 (Contract_Checker_Accept)
//        -> 5306-5886 (Contract_Checker_AcceptX)
//
// A.21: gate nhan `string strJson` -> o dich body chinh LA doi tuong JSON (Model = object, khong phai chuoi long).
//
// GAP — KHONG port phan goi dich vu ngoai (PORT_RULES_MiniContract §1 + LESSONS_AGENTS B.7):
//   - ESign.SignFileNew (gate:1873)
//   - RemoteSigningLogin (gate:1889, BaseController.cs:2397-2413)
//   - SignFileRemote_SingletonSigning (gate:1903, BaseController.cs:2432-2582)
//   - WA_Common_GetDTime (gate:1838) -> dung DateTime.UtcNow (A.13)
//   - WA_Contract_ContractElement_Update (gate:1937) -> goi OS_FileCenter_CreateAndMoveFSX (QContract.cs:41576)
//   - NotificationService.CreateNotifications / SendNotification qua iNOS/Firebase (zTemp.cs:5600-5881)
//
// Phan port DUOC (parity 1-1 phan noi bo):
//   (1) Guard CheckDB "ton tai va ContractStatus = 'PENDING'" (QContract.cs:9313-9378, zTemp.cs:5362-5368);
//   (2) Guard Sys_User "ton tai va FlagActive = '1'" (System.cs:3552-3610, zTemp.cs:5370-5376);
//   (3) Guard Contract_Checker "ton tai va FlagActive = '1'" (zTemp.cs:5235-5305, 5378-5385);
//   (4) Guard FlagChecker == '1' -> Contract_Checker_Accept_InvalidChecker (zTemp.cs:5392-5404);
//   (5) UPDATE Contract_Checker SET FlagCheck='1', CheckDTimeUTC, LogLUDTimeUTC, LogLUBy (zTemp.cs:5500-5535);
//   (6) Ghi Contract_Contract_HistAction FunctionActionType='CHECK', Desc='Kiểm tra hợp đồng' (zTemp.cs:5581-5596, QContract.cs:40201-40320).
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractCheckerAcceptRemoteControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractCheckerAcceptRemoteControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("contract_checker_accept_remote")]
    public async Task<IActionResult> Contract_Checker_Accept_Remote(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ContractCheckerAcceptRemoteReq? req)
    {
        if (req == null)
        {
            return PortResult.Fail("Contract_Checker_Accept", "Check.strJson", null);
        }

        var model = req.Model;
        var contractCode = StdParam(model?.ContractCode) ?? StdParam(req.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            return PortResult.Fail("Contract_Checker_Accept", "Check.strJson", null);
        }

        // A.14: khong co phien dang nhap o dich -> UserCode/WAUserCode TUY CHON (body hoac header); KHONG mac dinh tai khoan admin.
        var userCode = StdParam(req.UserCode)
            ?? StdParam(req.WAUserCode)
            ?? StdParam(HttpContext?.Request?.Headers["WAUserCode"].FirstOrDefault());

        var waUserCode = StdParam(req.WAUserCode)
            ?? StdParam(HttpContext?.Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? userCode
            ?? "PORT";

        var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Contract_Contract_CheckDB: ton tai va ContractStatus = 'PENDING' (zTemp.cs:5362-5368, QContract.cs:9313-9378)
            var contract = await _db.Set<Contract_Contract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));
            if (contract == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_CheckDB_OrganNotFound",
                    "Check.ContractCode", contractCode);
            }

            var status = (contract.ContractStatus ?? "").Trim().ToUpperInvariant();
            if (status != "PENDING")
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched",
                    "Check.ContractCode", contractCode,
                    "Check.strContractListToCheck", "PENDING",
                    "DB.ContractStatus", contract.ContractStatus);
            }

            // 2. Sys_User_CheckDB: ton tai va FlagActive = '1' (zTemp.cs:5370-5376, System.cs:3552-3610)
            if (string.IsNullOrEmpty(userCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_CheckDB_UserCodeNotFound",
                    "Check.UserCodeNotFound", userCode);
            }

            var sysUser = await _db.Set<Sys_User>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), PortQuery.Equal(userCode), PortQuery.Esc));
            if (sysUser == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_CheckDB_UserCodeNotFound",
                    "Check.UserCodeNotFound", userCode);
            }

            if ((sysUser.FlagActive ?? "").Trim() != "1")
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_CheckDB_FlagActiveNotMatched",
                    "Check.UserCodeError", userCode,
                    "Check.FlagActiveListToCheck", "1",
                    "Check.FlagActiveCurrent", sysUser.FlagActive);
            }

            // 3. Contract_Checker_CheckDB: ton tai va FlagActive = '1' (zTemp.cs:5378-5385, 5235-5305)
            var checker = await _db.Set<Contract_Checker>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                                       && EF.Functions.Like((x.UserCode ?? "").ToLower(), PortQuery.Equal(userCode), PortQuery.Esc));
            if (checker == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Checker_CheckDB_CheckerNotFound",
                    "Check.ContractCode", contractCode,
                    "Check.UserCode", userCode);
            }

            if ((checker.FlagActive ?? "").Trim() != "1")
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Checker_CheckDB_CheckDB_FlagActiveNotMatched",
                    "Check.ContractCode", contractCode,
                    "Check.UserCode", userCode,
                    "Check.strContractListToCheck", "1",
                    "DB.FlagActive", checker.FlagActive);
            }

            // 4. Guard FlagChecker == '1' (zTemp.cs:5392-5404)
            if ((checker.FlagChecker ?? "").Trim() != "1")
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Checker_Accept_InvalidChecker",
                    "Check.ContractCode", contractCode,
                    "Check.UserCode", checker.UserCode,
                    "Check.FlagChecker", checker.FlagChecker);
            }

            // 5. Update Contract_Checker (zTemp.cs:5500-5535)
            checker.FlagCheck = "1";
            checker.CheckDTimeUTC = nowStr;
            checker.LogLUDTimeUTC = nowStr;
            checker.LogLUBy = waUserCode;
            await _db.SaveChangesAsync();

            // 6. Ghi Contract_Contract_HistAction (zTemp.cs:5581-5596, QContract.cs:40201-40320)
            var maxVersion = await _db.Set<Contract_Contract_HistAction>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc))
                .MaxAsync(x => (int?)x.Version) ?? 0;

            var hist = new Contract_Contract_HistAction
            {
                ContractCode = contractCode,
                Version = maxVersion + 1,
                UserCode = waUserCode,
                UserName = sysUser.UserName,
                Description = "Kiểm tra hợp đồng",
                LogLUDTimeUTC = nowStr,
                LogLUBy = waUserCode,
                FunctionName = "Contract_Checker_Accept",
                FunctionActionType = "CHECK",
                Remark = "",
                FunctionRemark = ""
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                Success = true,
                Messages = new[] { "Xác nhận hợp đồng thành công" },
                RedirectUrl = $"/Contract_Contract/Detail?contractcode={contractCode}",
                Data = new
                {
                    ContractCode = contractCode,
                    UserCode = userCode,
                    FlagCheck = checker.FlagCheck,
                    CheckDTimeUTC = checker.CheckDTimeUTC,
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Contract_Checker_Accept", "Exception", ex.Message);
        }
    }

    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }
}

/// <summary>Tham số của gate Contract_ContractController.cs:1796 (Contract_Checker_Accept_Remote).</summary>
public class Contract_Contract_ContractCheckerAcceptRemoteReq
{
    public Contract_Contract_ContractCheckerAcceptRemoteModel? Model { get; set; }
    public string? ContractElement { get; set; }
    public string? StrSigRec { get; set; }
    public string? WAUserCode { get; set; }
    public string? UserCode { get; set; }
    public string? ContractCode { get; set; }
}

public class Contract_Contract_ContractCheckerAcceptRemoteModel
{
    public List<Contract_Contract_ContractCheckerAcceptRemoteFile>? FileList { get; set; }
    public string? ContractCode { get; set; }
    public string? ContractFileName { get; set; }
    public string? SignedFileContent { get; set; }
    public string? FileExtension { get; set; }
}

public class Contract_Contract_ContractCheckerAcceptRemoteFile
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
}
