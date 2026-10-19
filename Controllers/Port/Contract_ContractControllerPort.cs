using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Contract_ContractController.cs:415-698 (Index)
//       + 700-853 (DoSearch), 860-916 (ValidateContractNoExists)
//       + 7958-7978 (strWhereClause_Mst_ContractType), 7980-7995 (strWhereClause_Contract_ContractSearch)
//       + BaseController.cs:6012-6046 (List_Mst_ContractType_Async), 6138-6144 (RT_Contract_Contract_Get), 145-149 (GetDateTimeServerClient)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\ContractContractController.cs:375-443 (WA_Contract_Contract_Get)
//       MstContractTypeController.cs:20-88 (WA_Mst_ContractType_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:10526-10787 (WAS_Contract_Contract_Get),
//       9380-9575 (Contract_Contract_Get), 9747-10524 (Contract_Contract_GetX)
//       Master.cs:44050-44141 (WAS_Mst_ContractType_Get)
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Màn danh sách hợp đồng (Index). Trả dữ liệu mà gate đưa vào View:
    /// ListMst_ContractType (dropdown loại hợp đồng) + PageInfo&lt;Contract_Contract&gt;.
    /// Gate: Contract_ContractController.cs:415-698
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_IndexReq? req)
    {
        try
        {
            // Gate :435-436 — page = 0, recordcount = 10
            var page = req?.Page ?? 0;
            var recordcount = req?.RecordCount ?? 10;

            // Gate :528-561 — dropdown loại hợp đồng: Mst_ContractType.FlagActive = '1' (strWhereClause_Mst_ContractType :7971-7974)
            var listMst_ContractType = await _db.Set<Mst_ContractType>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .OrderBy(x => x.NetworkID)
                .ThenBy(x => x.ContractType)
                .ToListAsync();

            // Gate :562-567 — pageInfo khởi tạo rỗng, itemCount = 0, pageCount = 0
            var dataList = new List<Contract_Contract>();
            var itemCount = 0;
            var pageCount = 0;

            // Gate :568-669 — nhánh init
            var init = req?.Init ?? "";
            var contractStatus = req?.ContractStatus;
            var flagCheckContractNeedToSign = req?.FlagCheckContractNeedToSign ?? "";
            var createdTimeFrom = req?.CreatedTimeFrom ?? "";
            var createdTimeTo = req?.CreatedTimeTo ?? "";
            var contractDateFrom = req?.ContractDateFrom ?? "";
            var contractDateTo = req?.ContractDateTo ?? "";

            if (!string.IsNullOrEmpty(init))
            {
                init = init.ToUpperInvariant();

                // Gate :572-596 — init thuộc 3 trạng thái thì gán lại bộ lọc, ngược lại coi là "search"
                if (init.Equals(ContractStatus_Client.OnProcess)
                    || init.Equals(ContractStatus_Client.ContractWaitingToSign)
                    || init.Equals(ContractStatus_Client.ContractWaitingOtherSign))
                {
                    if (init.Equals(ContractStatus_Client.OnProcess))
                    {
                        contractStatus = ContractStatus_Client.OnProcess;
                        flagCheckContractNeedToSign = "";
                    }
                    else if (init.Equals(ContractStatus_Client.ContractWaitingToSign))
                    {
                        // Hợp đồng chờ tôi ký
                        flagCheckContractNeedToSign = "1";
                    }
                    else if (init.Equals(ContractStatus_Client.ContractWaitingOtherSign))
                    {
                        // Hợp đồng người khác ký
                        flagCheckContractNeedToSign = "0";
                    }
                }
                else
                {
                    flagCheckContractNeedToSign = "1";
                    init = "search";
                }

                // Gate :597-626 — strWhereClause_Contract_ContractSearch
                //   :7985-7988 Contract_Contract.ContractNo = contractno
                //   :7989-7992 Contract_Contract.OrgID = OrgID (gate KHÔNG truyền OrgID -> không lọc)
                //   Contract_ContractParty.ContractType = contracttype, Contract_ContractParty.CustomerCode = customercode
                //   Mst_Customer.CustomerCode = customercode
                // GAP: Contract_Contract.ContractNo / OrgID và Mst_Customer.CustomerCode không có trong excerpt
                //      (không có DDL/model) -> không lọc được, xem .parity-run\conflicts\10_conflict.md.
                var query = _db.Set<Contract_Contract>().AsNoTracking();

                var contractType = StdParam(req?.ContractType);
                var customerCode = StdParam(req?.CustomerCode);
                if (!string.IsNullOrEmpty(contractType) || !string.IsNullOrEmpty(customerCode))
                {
                    var partyQuery = _db.Set<Contract_ContractParty>().AsNoTracking();
                    if (!string.IsNullOrEmpty(contractType))
                    {
                        partyQuery = partyQuery.Where(x => EF.Functions.Like((x.ContractType ?? "").ToLower(), PortQuery.Equal(contractType), PortQuery.Esc));
                    }
                    if (!string.IsNullOrEmpty(customerCode))
                    {
                        partyQuery = partyQuery.Where(x => EF.Functions.Like((x.CustomerCode ?? "").ToLower(), PortQuery.Equal(customerCode), PortQuery.Esc));
                    }
                    var partyCodes = partyQuery.Select(x => x.ContractCode);
                    query = query.Where(x => partyCodes.Contains(x.ContractCode));
                }

                // Gate :639-640 — Ft_RecordStart = page * recordcount, Ft_RecordCount = recordcount
                long recordStart = (long)page * recordcount;
                long recordCount = recordcount;

                // QContract.cs:9850-9852 — order by ContractDateUTC desc, CreateDTimeUTC desc
                var ordered = query.OrderByDescending(x => x.ContractDateUTC).ThenByDescending(x => x.CreateDTimeUTC);

                // QContract.cs:9856 — MyCount = Count(0) của bảng lọc
                itemCount = await ordered.CountAsync();

                // QContract.cs:9864-9866 — MyIdxSeq >= @nFilterRecordStart and <= @nFilterRecordEnd
                // (nFilterRecordEnd = start + count - 1, QContract.cs:9784-9785)
                dataList = (recordCount <= 0 || recordStart < 0)
                    ? new List<Contract_Contract>()
                    : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                                   .Take((int)Math.Min(recordCount, int.MaxValue))
                                   .ToListAsync();

                // Gate :655-659 — chỉ tính PageCount khi có dòng
                if (dataList.Count > 0)
                {
                    pageCount = (itemCount % recordcount == 0) ? (itemCount / recordcount) : (itemCount / recordcount + 1);
                }
            }
            else
            {
                // Gate :662-669 — không có init: KHÔNG gọi API, chỉ gán giá trị mặc định cho View
                flagCheckContractNeedToSign = "1";
                var firstDayOfYear = DateTime.Now.Year + "-01-01"; // GetDateTimeServerClient (BaseController.cs:145-149)
                createdTimeFrom = firstDayOfYear;
                contractDateFrom = firstDayOfYear;
                init = "search";
            }

            // Gate :671-675
            var pageInfo = new PageInfo<Contract_Contract>(dataList, itemCount, pageCount, page, recordcount);

            // Gate :677-695 — các giá trị ViewBag (giữ nguyên tên trường của gate)
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ListMst_ContractType = listMst_ContractType,
                    PageInfo = pageInfo,
                    StartCount = ((long)page * recordcount).ToString(),
                    ContractCode = req?.ContractCode,
                    ContractName = req?.ContractName,
                    ContractNo = req?.ContractNo,
                    ContractType = req?.ContractType,
                    ContractRefNo = req?.ContractRefNo,
                    ContractDateUTCFrom = contractDateFrom,
                    ContractDateUTCTo = contractDateTo,
                    CreateDTimeUTCFrom = createdTimeFrom,
                    CreateDTimeUTCTo = createdTimeTo,
                    ContractStatus = contractStatus,
                    ContractStatus_Pending = req?.CtrStatusPending,
                    ContractStatus_Canceled = req?.CtrStatusCanceled,
                    EffStatus_Processing = req?.EffStatusProcessing,
                    EffStatus_Finished = req?.EffStatusFinished,
                    SignBy = req?.SignBy,
                    CustomerCode = req?.CustomerCode,
                    FlagCheckContractNeedToSign = flagCheckContractNeedToSign,
                    Init = init,
                    CurrentUserSigningInfo = await GetCurrentUserSigningInfoAsync(req?.OrgID)
                }
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Contract_Contract_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Tìm kiếm hợp đồng (DoSearch).
    /// Gate: Contract_ContractController.cs:700-853 + 7980-7995 (strWhereClause_Contract_ContractSearch)
    /// API:  ContractContractController.cs:375-443 (WA_Contract_Contract_Get)
    /// BIZ:  QContract.cs:10526-10787 (WAS_Contract_Contract_Get), 9380-9575 (Contract_Contract_Get),
    ///       9747-10524 (Contract_Contract_GetX)
    /// </summary>
    [HttpPost("dosearch")]
    public async Task<IActionResult> DoSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_DoSearchReq? req)
    {
        // Gate :709-715 — pageInfo rỗng, itemCount = 0, pageCount = 0 (resultEntry.Success=false chỉ dùng ở nhánh lỗi)
        var page = req?.Page ?? 0;
        var recordCount = req?.RecordCount ?? 0;
        var dataList = new List<Contract_Contract>();
        var itemCount = 0;
        var pageCount = 0;

        // Gate :718-719 — CUtils.StrValue (KHÔNG Trim/ToUpper)
        // contractStatus chỉ được truyền vào Contract_ContractSearch nhưng hàm lọc nguồn (:7980-7995) KHÔNG dùng -> không lọc
        var flagCheckContractNeedToSign = StrValue(req?.FlagCheckContractNeedTosign);

        // Gate :721 — init = CUtils.StrValue(searchModel.Init).ToUpper()
        var init = (StrValue(req?.Init) ?? "").ToUpperInvariant();

        // Gate :740-744 — hợp đồng người khác ký: flagcheckcontractneedtosign = "0"
        if (init.Equals(ContractStatus_Client.ContractWaitingOtherSign))
        {
            flagCheckContractNeedToSign = "0";
        }

        // Gate :746-777 — strWhereClause_Contract_ContractSearch
        //   :7985-7988 Contract_Contract.ContractNo = ContractNo (CUtils.StrValue, toán tử "=")
        //   :7989-7992 Contract_Contract.OrgID = OrgID — gate KHÔNG truyền OrgID (dòng :749-750 bị comment) -> không lọc
        //   Contract_ContractParty.ContractType / CustomerCode, Mst_Customer.CustomerCode: hàm lọc nguồn (excerpt :7980-7995)
        //   KHÔNG dùng -> không lọc (A.18).
        var contractNo = StrValue(req?.ContractNo);
        var query = _db.Set<Contract_Contract>().AsNoTracking();
        if (!string.IsNullOrEmpty(contractNo))
        {
            query = query.Where(x => EF.Functions.Like((x.ContractNo ?? "").ToLower(), PortQuery.Equal(contractNo), PortQuery.Esc));
        }

        // Gate :791-792 — Ft_RecordStart = Page * RecordCount, Ft_RecordCount = RecordCount
        // QContract.cs:9784-9785 — nFilterRecordEnd = start + count - 1
        long recordStart = (long)page * recordCount;
        long recordCountL = recordCount;

        // QContract.cs:9850-9852 — order by ContractDateUTC desc, CreateDTimeUTC desc
        var ordered = query.OrderByDescending(x => x.ContractDateUTC).ThenByDescending(x => x.CreateDTimeUTC);

        // QContract.cs:9856 — MyCount = Count(0) của bảng lọc
        itemCount = await ordered.CountAsync();

        // QContract.cs:9864-9866 — MyIdxSeq >= @nFilterRecordStart and <= @nFilterRecordEnd
        dataList = (recordCountL <= 0 || recordStart < 0)
            ? new List<Contract_Contract>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordCountL, int.MaxValue))
                           .ToListAsync();

        // Gate :815-819 — chỉ tính PageCount khi có dòng (RecordCount = 0 -> không chia cho 0)
        if (dataList.Count > 0)
        {
            pageCount = (itemCount % recordCount == 0) ? (itemCount / recordCount) : (itemCount / recordCount + 1);
        }

        // Gate :821-824 — PageSize = RecordCount, PageIndex = Page, ItemCount, PageCount
        var pageInfo = new PageInfo<Contract_Contract>(dataList, itemCount, pageCount, page, recordCount);

        // Gate :844 — JsonView("BindDataContract_Contract", pageInfo) -> Success = true, Data = pageInfo
        // GAP: cột FilePathActual (QContract.cs:9946-9951) và ctc_TContracName (QContract.cs:9945) không trả được:
        //      Contract_ContractTBody chỉ có ContractFilePath (các cột khác của bảng không có trong excerpt),
        //      Contract_TempContract.TContracName có nhưng cột nguồn đặt tên ctc_TContracName -> xem conflicts\11_conflict.md.
        // GAP: Contract_ContractDtl/Files/Attribute_Contract/Attribute_ContractDtl/Checker/UserInContract/Contract_Base64/
        //      Contract_Ref/HistAction/Channel/ChannelOTP (gate :796-800 yêu cầu "*") chưa có entity ở đích -> không trả.
        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }

    /// <summary>
    /// Kiểm tra số hợp đồng đã tồn tại chưa (ValidateContractNoExists).
    /// Gate: Contract_ContractController.cs:860-916
    ///       + 7980-7995 (strWhereClause_Contract_ContractSearch)
    ///       + BaseController.cs:6138-6144 (RT_Contract_Contract_Get)
    /// API:  ContractContractController.cs:375-443 (WA_Contract_Contract_Get)
    /// BIZ:  QContract.cs:10526-10787 (WAS_Contract_Contract_Get),
    ///       9380-9575 (Contract_Contract_Get), 9747-10524 (Contract_Contract_GetX)
    /// </summary>
    [HttpPost("validatecontractnoexists")]
    public async Task<IActionResult> ValidateContractNoExists([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ValidateContractNoExistsReq? req)
    {
        try
        {
            var query = _db.Set<Contract_Contract>().AsNoTracking();

            var contractNo = StrValue(req?.ContractNo);
            if (!string.IsNullOrEmpty(contractNo))
            {
                query = query.Where(x => EF.Functions.Like((x.ContractNo ?? "").ToLower(), PortQuery.Equal(contractNo), PortQuery.Esc));
            }

            var orgId = StrValue(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            var exists = await query.AnyAsync();
            var flagExists = exists ? "1" : "0";

            return Ok(new
            {
                Success = true,
                FlagExists = flagExists
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Contract_Contract_ValidateContractNoExists", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xóa hợp đồng (Delete).
    /// Gate: Contract_ContractController.cs:923-1141
    ///       + 8364-8540 (strWhereClause_Contract_ContractSearch_ContractCode_In)
    ///       + BaseController.cs:6138-6144 (RT_Contract_Contract_Get)
    /// API:  ContractContractController.cs:20-88 (WA_Contract_Contract_Save)
    ///       ContractContractController.cs:375-443 (WA_Contract_Contract_Get)
    /// BIZ:  QContract.cs:31734-31954 (WAS_Contract_Contract_Save), 31319-31548 (Contract_Contract_Save),
    ///       19808-23558 (Contract_Contract_SaveX_New20240312),
    ///       34323-34428 (Contract_Contract_GetInfoPartyX),
    ///       10526-10787 (WAS_Contract_Contract_Get), 9380-9575 (Contract_Contract_Get),
    ///       9747-10524 (Contract_Contract_GetX)
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_DeleteReq? req)
    {
        try
        {
            // Gate :926 — if (!CUtils.IsNullOrEmpty(model))
            // Nếu body rỗng hoặc req null -> gate trả về JsonResultEntry: "Mã hợp đồng không hợp lệ" (:1136-1137)
            if (req == null)
            {
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Mã hợp đồng không hợp lệ" },
                    Title = "Mã hợp đồng không hợp lệ"
                });
            }

            // Hỗ trợ trường hợp client truyền chuỗi JSON thô trong Model như gate MVC (Contract_ContractController.cs:923, 928)
            var flagRedirect = req.FlagRedirect;
            var listContracts = req.Lst_Contract_Contract;
            var contractCodeSingle = req.ContractCode;
            var contractNameSingle = req.ContractName;

            if (!string.IsNullOrWhiteSpace(req.Model))
            {
                try
                {
                    var modelObj = System.Text.Json.JsonSerializer.Deserialize<Contract_Contract_DeleteReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        flagRedirect ??= modelObj.FlagRedirect;
                        listContracts ??= modelObj.Lst_Contract_Contract;
                        contractCodeSingle ??= modelObj.ContractCode;
                        contractNameSingle ??= modelObj.ContractName;
                    }
                }
                catch
                {
                    // Nếu Model là mã đơn thuần
                    contractCodeSingle ??= req.Model;
                }
            }

            if ((listContracts == null || listContracts.Count == 0) && !string.IsNullOrWhiteSpace(contractCodeSingle))
            {
                listContracts = new List<Contract_Contract_DeleteItem>
                {
                    new Contract_Contract_DeleteItem
                    {
                        ContractCode = contractCodeSingle,
                        ContractName = contractNameSingle
                    }
                };
            }

            // Gate :929-932, 1128-1132 — objContract_ContractInput.Lst_Contract_Contract rỗng
            if (listContracts == null || listContracts.Count == 0)
            {
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Mã hợp đồng không hợp lệ" },
                    Title = "Mã hợp đồng không hợp lệ"
                });
            }

            // Gate :934-936 — danh sách mã hợp đồng cần xóa
            var listContractCode = listContracts
                .Where(it => !string.IsNullOrWhiteSpace(it.ContractCode))
                .Select(item => StdParam(item.ContractCode)!)
                .Distinct()
                .ToList();

            if (listContractCode.Count == 0)
            {
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Mã hợp đồng không hợp lệ" },
                    Title = "Mã hợp đồng không hợp lệ"
                });
            }

            // Gate :977, :978-980 — RT_Contract_Contract_Get tìm kiếm theo danh sách ContractCode
            var existingContracts = await _db.Set<Contract_Contract>()
                .Where(x => listContractCode.Contains(x.ContractCode))
                .ToListAsync();

            if (existingContracts.Count == 0)
            {
                // Gate :1122-1126 — Không tìm thấy hợp đồng nào
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Mã hợp đồng không hợp lệ" },
                    Title = "Mã hợp đồng không hợp lệ"
                });
            }

            // Mở transaction xóa (BIZ Contract_Contract_Save: QContract.cs:31366, 31503, 31511)
            var matchedCodes = existingContracts.Select(x => x.ContractCode).ToList();
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // BIZ QContract.cs:21768-21860 (Clear All): xóa các bảng con liên quan đến hợp đồng
                // 1. Contract_ContractVerifyOtp
                var otps = await _db.Set<Contract_ContractVerifyOtp>()
                    .Where(x => matchedCodes.Contains(x.ContractCode))
                    .ToListAsync();
                if (otps.Count > 0)
                {
                    _db.Set<Contract_ContractVerifyOtp>().RemoveRange(otps);
                }

                // 2. Contract_ContractElement
                var elements = await _db.Set<Contract_ContractElement>()
                    .Where(x => matchedCodes.Contains(x.ContractCode))
                    .ToListAsync();
                if (elements.Count > 0)
                {
                    _db.Set<Contract_ContractElement>().RemoveRange(elements);
                }

                // 3. Contract_ContractTBody
                var tbodies = await _db.Set<Contract_ContractTBody>()
                    .Where(x => matchedCodes.Contains(x.ContractCode))
                    .ToListAsync();
                if (tbodies.Count > 0)
                {
                    _db.Set<Contract_ContractTBody>().RemoveRange(tbodies);
                }

                // 4. Contract_ContractParty
                var parties = await _db.Set<Contract_ContractParty>()
                    .Where(x => matchedCodes.Contains(x.ContractCode))
                    .ToListAsync();
                if (parties.Count > 0)
                {
                    _db.Set<Contract_ContractParty>().RemoveRange(parties);
                }

                // 5. Contract_Contract
                _db.Set<Contract_Contract>().RemoveRange(existingContracts);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Save", "Exception", ex.Message);
            }

            // Gate :1103-1120 — tạo thông báo kết quả
            var flagRedirectVal = StrValue(flagRedirect);
            string message;
            string redirectUrl = "/Contract_Contract"; // Url.Action("Index", "Contract_Contract")

            if (listContractCode.Count == 1)
            {
                var inputItem = listContracts.FirstOrDefault(x => StdParam(x.ContractCode) == listContractCode[0]);
                var contractName = !string.IsNullOrEmpty(inputItem?.ContractName)
                    ? inputItem.ContractName
                    : (existingContracts.FirstOrDefault()?.ContractName ?? "");

                message = $"Xóa hợp đồng '{contractName}' thành công!";
                if (flagRedirectVal == "1") // Client_Flag.Active = "1"
                {
                    redirectUrl = $"/Contract_Contract/Update?contractcode={listContractCode[0]}";
                }
            }
            else
            {
                message = "Xóa hợp đồng thành công!";
            }

            return Ok(new
            {
                Success = true,
                Messages = new[] { message },
                Title = message,
                RedirectUrl = redirectUrl
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Contract_Contract_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Gate :695 — CurrentUserSigningInfo lấy từ UserState.Mst_NNT (NNT của phiên đăng nhập).
    /// Ở đích không có phiên: nhận OrgID tuỳ chọn trong body, không truyền thì không có thông tin (null).
    /// </summary>
    private async Task<object?> GetCurrentUserSigningInfoAsync(string? orgId)
    {
        var org = StdParam(orgId);
        if (string.IsNullOrEmpty(org)) return null;

        var nnt = await _db.Set<Mst_NNT>()
            .AsNoTracking()
            .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(org), PortQuery.Esc))
            .OrderBy(x => x.MST)
            .FirstOrDefaultAsync();

        if (nnt == null) return null;

        return new
        {
            name = nnt.NNTFullName,
            email = nnt.ContactEmail,
            position = nnt.NNTPosition,
            phone = nnt.ContactPhone
        };
    }

    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    /// <summary>CUtils.StrValue của nguồn: null -> "", KHÔNG Trim/ToUpper (gate :718-721, :751).</summary>
    private static string? StrValue(object? value) => value?.ToString() ?? "";
}

/// <summary>
/// Hằng số trạng thái hợp đồng phía client của nguồn (gate Contract_ContractController.cs:572-589).
/// GAP: giá trị chuỗi của ContractStatus_Client KHÔNG có trong source_excerpt.txt (đề bài cấm đọc file ngoài excerpt)
///      -> để null, nhánh so sánh không bao giờ khớp (xem .parity-run\conflicts\10_conflict.md).
/// </summary>
internal static class ContractStatus_Client
{
    public const string? OnProcess = null;
    public const string? ContractWaitingToSign = null;
    public const string? ContractWaitingOtherSign = null;
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:415-436 (Index).
/// </summary>
public class Contract_Contract_IndexReq
{
    public string? ContractNo { get; set; }
    public string? ContractName { get; set; }
    public string? ContractCode { get; set; }
    public string? ContractType { get; set; }
    public string? SignBy { get; set; }
    public string? ContractRefNo { get; set; }
    public string? CustomerCode { get; set; }
    public string? ContractStatus { get; set; }
    public string? ContractDateFrom { get; set; }
    public string? ContractDateTo { get; set; }
    public string? CreatedTimeFrom { get; set; } = "";
    public string? CreatedTimeTo { get; set; } = "";
    public string? CtrStatusPending { get; set; } = "";
    public string? CtrStatusCanceled { get; set; } = "";
    public string? EffStatusProcessing { get; set; } = "";
    public string? EffStatusFinished { get; set; } = "";
    public string? FlagCheckContractNeedToSign { get; set; } = "";
    public string? TypeSearch { get; set; } = "";
    public string? Init { get; set; } = "";
    public int? Page { get; set; }
    public int? RecordCount { get; set; }

    /// <summary>OrgID của phiên (nguồn lấy từ UserState.Mst_NNT.OrgID) — tuỳ chọn, dùng cho CurrentUserSigningInfo.</summary>
    public string? OrgID { get; set; }
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:700-701 (DoSearch — ContractSearchModel).
/// Chỉ khai các trường gate ĐỌC (:718-777); Page/RecordCount là int như model nguồn (không có giá trị mặc định).
/// </summary>
public class Contract_Contract_DoSearchReq
{
    public string? ContractNo { get; set; }
    public string? ContractDateFrom { get; set; }
    public string? ContractDateTo { get; set; }
    public string? CreatedTimeFrom { get; set; }
    public string? CreatedTimeTo { get; set; }
    public string? ContractStatus { get; set; }
    public string? FlagCheckContractNeedTosign { get; set; }
    public string? FlagSendUserAll { get; set; }
    public string? ContractType { get; set; }
    public string? CustomerCode { get; set; }
    public string? Init { get; set; }
    public int Page { get; set; }
    public int RecordCount { get; set; }
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:860 (ValidateContractNoExists).
/// contractno: số hợp đồng cần kiểm tra.
/// OrgID: mã đơn vị (nguồn lấy từ UserState.Mst_NNT.OrgID, ở đích truyền qua body tuỳ chọn theo bài học A.14).
/// </summary>
public class Contract_Contract_ValidateContractNoExistsReq
{
    public string? ContractNo { get; set; }
    public string? OrgID { get; set; }
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:923 (Delete).
/// model: chuỗi JSON chứa thông tin xóa hoặc nhận trực tiếp qua DTO.
/// flagredirect: cờ chuyển hướng ("1": đến trang Update hợp đồng, ngược lại về Index).
/// </summary>
public class Contract_Contract_DeleteReq
{
    public string? Model { get; set; }
    public string? FlagRedirect { get; set; }
    public List<Contract_Contract_DeleteItem>? Lst_Contract_Contract { get; set; }
    public string? ContractCode { get; set; }
    public string? ContractName { get; set; }
}

/// <summary>
/// Mục phần tử hợp đồng cần xóa (gate Contract_ContractController.cs:928, 934, 1109).
/// </summary>
public class Contract_Contract_DeleteItem
{
    public string? ContractCode { get; set; }
    public string? ContractName { get; set; }
}

