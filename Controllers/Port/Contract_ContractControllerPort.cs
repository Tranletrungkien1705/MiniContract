using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Contract_ContractController.cs:415-698 (Index)
//       + 700-853 (DoSearch), 860-916 (ValidateContractNoExists), 923-1141 (Delete), 2214-2445 (ApprovedAndSign)
//       + 7958-7978 (strWhereClause_Mst_ContractType), 7980-7995 (strWhereClause_Contract_ContractSearch)
//       + 8364-8540 (strWhereClause_Contract_ContractSearch_ContractCode_In)
//       + BaseController.cs:6012-6046 (List_Mst_ContractType_Async), 6138-6144 (RT_Contract_Contract_Get), 145-149 (GetDateTimeServerClient)
//       + BaseController.cs:6667-6737 (ReturnApiUserCreateContract — GAP)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\ContractContractController.cs:375-443 (WA_Contract_Contract_Get)
//       ContractContractController.cs:162-230 (WA_Contract_Contract_Approved), 517-585 (WA_Contract_Contract_PartySign)
//       MstContractTypeController.cs:20-88 (WA_Mst_ContractType_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:10526-10787 (WAS_Contract_Contract_Get),
//       9380-9575 (Contract_Contract_Get), 9747-10524 (Contract_Contract_GetX)
//       QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) → 32612-33654 (Contract_Contract_Approved / _ApprovedX)
//       QContract.cs:36439-36552 (WAS_Contract_Contract_PartySign) → 36218-36437 (Contract_Contract_PartySign)
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
    /// <summary>
    /// Ký và gửi mail hợp đồng (kèm gửi cho đối tác).
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Contract_ContractController.cs:3306-3575 (ApprovedSignAndSendMail_Old)
    ///       BaseController.cs:6667-6737 (ReturnApiUserCreateContract — GAP)
    /// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\ContractContractController.cs:162-230, 517-585
    /// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:34099-34211, 36439-36552 (wrapper)
    ///       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:33656-33873, 36218-36437 (thực hiện)
    /// GAP: - Gọi dịch vụ ngoài `MasterServerService` (lấy API từ master server) — bỏ qua, không mock
    ///      - ContractPartySend (gửi mail) đã commented out trong gate — bỏ qua
    ///      - FlagCheckPageSign / NetworkIDParty / OrgIDParty / MST / UserCodeSign / UserNameSign / ContractFileVersion của PartySign không rõ chuẩn hoá (GAP)
    /// </summary>
    [HttpPost("approvedsignyandsendmail_old")]
    public async Task<IActionResult> ApprovedSignAndSendMail_Old([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ApprovedSignAndSendMail_OldReq? req)
    {
        // Gate :3317 — deserialize ContractModel từ model string nếu có
        Contract_Contract_ApprovedSignAndSendMail_OldReq? model = req;
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<Contract_Contract_ApprovedSignAndSendMail_OldReq>(
                    req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) model = parsed;
            }
            catch
            {
            }
        }

        // Gate :3334 — CUtils.StrValue -> Trim
        var contractCode = StdParam(model?.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            return PortResult.Fail("WAS_Contract_Contract_Approved", "Check.strJson", null);
        }

        // Gate :3344-3345 — FlagOrgCreate = "1" (hợp đồng đi) hay khác (hợp đồng đến)
        var flagOrgCreate = StrValue(model?.FlagOrgCreate);
        var contractFileSpec = model?.SignedFileContent ?? "";
        var contractFileName = model?.ContractFileName ?? "";
        var flagElement = StrValue(model?.FlagElement);
        var flagCheckOTP = StrValue(model?.FlagCheckOTP);
        var contractFileVersion = model?.ContractFileVersion ?? "";

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Gate :3348 hoặc 3388 — gọi biz qua API service (đó là dịch vụ bên ngoài, skip + GAP)
            // Port logic chính: search hợp đồng sau khi tạo
            // Gate :3396-3426 — strWhereClause_Contract_ContractSearch lọc ContractCode (ContractNo cũng là alias tương tự)
            var contract = await _db.Set<Contract_Contract>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));

            if (contract == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("WAS_Contract_Contract_Approved", "Check.ContractCode", contractCode);
            }

            // Gate :3438-3439 — Guid + Session lưu listContract_Result (skip — đó là phần UI cũ)
            // Gate :3441 — ContractPartySend (gửi mail, commented out) — skip + GAP

            await tx.CommitAsync();

            // Gate :3562-3567 — Success=true + AddMessage + RedirectUrl
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ContractCode = contractCode,
                    Message = $"Ký và gửi hợp đồng '{contractCode}' thành công!",
                    RedirectUrl = $"/Contract_Contract/Detail?contractcode={contractCode}"
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("WAS_Contract_Contract_Approved", "Exception", ex.Message);
        }
    }

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
        // Gate :709-715 — pageInfo rỗng, itemCount = 0, pageCount = 0
        var page = req?.Page ?? 0;
        var recordCount = req?.RecordCount ?? 0;
        var dataList = new List<Contract_Contract>();
        var itemCount = 0;
        var pageCount = 0;

        // Gate :718-719 — CUtils.StrValue (KHÔNG Trim/ToUpper)
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
        var contractNo = StrValue(req?.ContractNo);
        var query = _db.Set<Contract_Contract>().AsNoTracking();
        if (!string.IsNullOrEmpty(contractNo))
        {
            query = query.Where(x => EF.Functions.Like((x.ContractNo ?? "").ToLower(), PortQuery.Equal(contractNo), PortQuery.Esc));
        }

        // Gate :791-792 — Ft_RecordStart = Page * RecordCount, Ft_RecordCount = RecordCount
        long recordStart = (long)page * recordCount;
        long recordCountL = recordCount;

        // QContract.cs:9850-9852 — order by ContractDateUTC desc, CreateDTimeUTC desc
        var ordered = query.OrderByDescending(x => x.ContractDateUTC).ThenByDescending(x => x.CreateDTimeUTC);

        // QContract.cs:9856 — MyCount = Count(0)
        itemCount = await ordered.CountAsync();

        dataList = (recordCountL <= 0 || recordStart < 0)
            ? new List<Contract_Contract>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordCountL, int.MaxValue))
                           .ToListAsync();

        // Gate :815-819 — chỉ tính PageCount khi có dòng
        if (dataList.Count > 0)
        {
            pageCount = (itemCount % recordCount == 0) ? (itemCount / recordCount) : (itemCount / recordCount + 1);
        }

        var pageInfo = new PageInfo<Contract_Contract>(dataList, itemCount, pageCount, page, recordCount);

        // Gate :844 — JsonView("BindDataContract_Contract", pageInfo) -> Success = true, Data = pageInfo
        // GAP: cột FilePathActual (QContract.cs:9946-9951) và ctc_TContracName (QContract.cs:9945) không trả được.
        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }

    /// <summary>
    /// Kiểm tra số hợp đồng đã tồn tại chưa (ValidateContractNoExists).
    /// Gate: Contract_ContractController.cs:860-916
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
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_DeleteReq? req)
    {
        try
        {
            if (req == null)
            {
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Mã hợp đồng không hợp lệ" },
                    Title = "Mã hợp đồng không hợp lệ"
                });
            }

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

            if (listContracts == null || listContracts.Count == 0)
            {
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Mã hợp đồng không hợp lệ" },
                    Title = "Mã hợp đồng không hợp lệ"
                });
            }

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

            var existingContracts = await _db.Set<Contract_Contract>()
                .Where(x => listContractCode.Contains(x.ContractCode))
                .ToListAsync();

            if (existingContracts.Count == 0)
            {
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Mã hợp đồng không hợp lệ" },
                    Title = "Mã hợp đồng không hợp lệ"
                });
            }

            var matchedCodes = existingContracts.Select(x => x.ContractCode).ToList();
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var otps = await _db.Set<Contract_ContractVerifyOtp>()
                    .Where(x => matchedCodes.Contains(x.ContractCode))
                    .ToListAsync();
                if (otps.Count > 0)
                {
                    _db.Set<Contract_ContractVerifyOtp>().RemoveRange(otps);
                }

                var elements = await _db.Set<Contract_ContractElement>()
                    .Where(x => matchedCodes.Contains(x.ContractCode))
                    .ToListAsync();
                if (elements.Count > 0)
                {
                    _db.Set<Contract_ContractElement>().RemoveRange(elements);
                }

                var tbodies = await _db.Set<Contract_ContractTBody>()
                    .Where(x => matchedCodes.Contains(x.ContractCode))
                    .ToListAsync();
                if (tbodies.Count > 0)
                {
                    _db.Set<Contract_ContractTBody>().RemoveRange(tbodies);
                }

                var parties = await _db.Set<Contract_ContractParty>()
                    .Where(x => matchedCodes.Contains(x.ContractCode))
                    .ToListAsync();
                if (parties.Count > 0)
                {
                    _db.Set<Contract_ContractParty>().RemoveRange(parties);
                }

                _db.Set<Contract_Contract>().RemoveRange(existingContracts);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Save", "Exception", ex.Message);
            }

            var flagRedirectVal = StrValue(flagRedirect);
            string message;
            string redirectUrl = "/Contract_Contract";

            if (listContractCode.Count == 1)
            {
                var inputItem = listContracts.FirstOrDefault(x => StdParam(x.ContractCode) == listContractCode[0]);
                var contractName = !string.IsNullOrEmpty(inputItem?.ContractName)
                    ? inputItem.ContractName
                    : (existingContracts.FirstOrDefault()?.ContractName ?? "");

                message = $"Xóa hợp đồng '{contractName}' thành công!";
                if (flagRedirectVal == "1")
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
    /// Duyệt &amp; ký hợp đồng (ApprovedAndSign).
    /// Gate: Contract_ContractController.cs:2214-2445
    /// API:  ContractContractController.cs:162-230 (WA_Contract_Contract_Approved),
    ///       ContractContractController.cs:517-585 (WA_Contract_Contract_PartySign)
    /// BIZ:  QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) → 32612-33654 (Contract_Contract_Approved / _ApprovedX)
    ///       QContract.cs:36439-36552 (WAS_Contract_Contract_PartySign) → 36218-36437 (Contract_Contract_PartySign)
    ///
    /// GAP — KHÔNG port phần gọi dịch vụ ngoài (PORT_RULES_MiniContract §1 + LESSONS_AGENTS B.7):
    ///   - ESign.SignFileNew (gate:2321) — thư viện ký số, không có ở đích.
    ///   - WA_Common_GetDTime (gate:2253) — dịch vụ giờ ngoài → thay bằng DateTime.UtcNow (bài học A.13).
    ///   - ReturnApiUserCreateContract → WA_MstSv_Sys_User_Login + WA_Contract_ContractParty_Get (gate:2389-2392,
    ///     BaseController.cs:6667-6737) — dịch vụ MasterServer ngoài.
    ///   - OS_FileCenter_CreateAndMoveFSX (biz:32799) — dịch vụ FileCenter ngoài (không lấy được ContractFilePath từ FS).
    ///   - OS_MstSvTVANService.WA_OS_MstSv_OrgInNetwork_GetByMST (biz:34394, hàm Contract_Contract_GetInfoPartyX) — MasterServer ngoài.
    ///   - Sys_User_GetAbilityViewOfUser (biz:32656) — hàm auth QContract nội bộ, đích không có.
    ///   - InosService thông báo (biz:33496-33516) — dịch vụ INOS ngoài.
    ///   - Contract_Checker_CheckDB (biz:3825) — bảng Contract_Checker KHÔNG có ở đích → không kiểm FlagChecker.
    ///   - Cột không có trên entity đích: ContractFileVersion, ContractFileName, ApprDTimeUTC, ApprBy, EffStatus
    ///     → bỏ guard version + không cập nhật (GAP).
    ///   - Contract_ContractDtl.ContractDtlStatus, Contract_Contract_Base64, Contract_ContractFiles,
    ///     Contract_Attribute_Contract(Dtl), Contract_ContractChannel(OTP) → không có entity → bỏ các UPDATE tương ứng.
    /// Phần port ĐƯỢC (parity 1-1 phần nội bộ):
    ///   (1) Guard CheckDB "phải tồn tại và status ∈ PENDING,ONPROCESS" (biz:32686-32695);
    ///   (2) Guard ContractNo NOT NULL → Contract_Contract_Approved_ContractNoIsNotNull (biz:32769-32780);
    ///   (3) UPDATE Contract_Contract SET ContractStatus='ONPROCESS' + ContractFilePath + LogLUDTimeUTC/LogLUBy (biz:32914-33017);
    ///   (4) UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED' (biz:32974-32991);
    ///   (5) Nếu mọi party CONFIRMED → ContractStatus='CONFIRMED' (biz:42092-42134);
    ///   (6) Ghi Contract_Contract_HistAction FunctionActionType='APPROVED', Desc='Ký Hợp đồng' (biz:33154-33199).
    /// </summary>
    [HttpPost("approvedandsign")]
    public async Task<IActionResult> ApprovedAndSign([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ApprovedAndSignReq? req)
    {
        // Gate :2224 — nếu client gửi model dạng chuỗi JSON, deserialize (bài học A.21)
        Contract_Contract_ApprovedAndSignReq? model = req;
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<Contract_Contract_ApprovedAndSignReq>(
                    req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) model = parsed;
            }
            catch
            {
                // Bỏ qua lỗi parse — dùng các trường phẳng của req
            }
        }

        // Gate :2369 — CUtils.StrValue(...ContractCode); biz:3770 StdParam -> Trim + ToUpper
        var contractCode = StdParam(model?.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            // Mã mặc định biz: TError.ErridQContract.Contract_Contract_Approved (QContract.cs:34112)
            return PortResult.Fail("Contract_Contract_Approved", "Check.strJson", null);
        }

        // Gate :2329-2336 — chỉ để suy ra InfoToken (bỏ ESign); biz:32667 InfoToken = StdStr(InfoToken)
        var tokenInfo = "";
        if (req?.StrSigRec != null && req.StrSigRec.Count > 0)
        {
            var objSignatureDigital = req.StrSigRec.FirstOrDefault(it =>
                string.Equals((it.SignType ?? "").ToUpperInvariant(), "DIGITAL", StringComparison.Ordinal)
                || string.Equals((it.SignType ?? "").ToUpperInvariant(), "SHORT", StringComparison.Ordinal));
            if (objSignatureDigital != null) tokenInfo = (objSignatureDigital.TokenInfo?.ToString() ?? "").Trim();
        }

        // Gate :2371 — ContractFileName
        var contractFileName = (model?.ContractFileName ?? "").Trim();

        // Gate :2373-2374, biz:32680-32681 — StdFlag / StrValue
        var flagCheckOTP = StdFlag(model?.FlagCheckOTP);
        var flagOrgCreate = StrValue(model?.FlagOrgCreate);

        // Người thực hiện — nguồn lấy UserState.SysUser.UserCode (gate:2360); đích không có phiên (bài học A.24)
        var waUserCode = StdParam(model?.WAUserCode)
            ?? StdParam(Request.Headers["WAUserCode"].FirstOrDefault())
            ?? "PORT";

        var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // biz:32686-32695 Contract_Contract_CheckDB(code, "1" (Yes), "PENDING, ONPROCESS")
            // → không tồn tại hoặc status ngoài danh sách ⇒ lỗi.
            //   Tên mã CheckDB KHÔNG nằm trong excerpt (GAP) → dùng mã Approved_Invalid_UserApproved theo excerpt:7991.
            var contract = await _db.Set<Contract_Contract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));

            var status = (contract?.ContractStatus ?? "").ToUpperInvariant();
            var inStatusList = status == "PENDING" || status == "ONPROCESS";
            if (contract == null || !inStatusList)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_Invalid_UserApproved",
                    "Check.ContractCode", contractCode,
                    "Check.ContractStatus", contract?.ContractStatus);
            }

            // biz:32769-32780 ContractNo NOT NULL
            if (string.IsNullOrEmpty(contract.ContractNo))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_ContractNoIsNotNull",
                    "Check.ContractCode", contractCode,
                    "Check.ContractNo", contract.ContractNo);
            }

            // biz:32914-33017 UPDATE Contract_Contract (chỉ các cột đích có)
            contract.ContractStatus = "ONPROCESS";
            if (!string.IsNullOrEmpty(contractFileName)) contract.ContractFilePath = contractFileName;
            contract.LogLUDTimeUTC = nowStr;
            contract.LogLUBy = waUserCode;

            // biz:32974-32991 UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED'
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

            // biz:42092-42134 Check PartySign: mọi party CONFIRMED (hoặc không có party) → ContractStatus='CONFIRMED'
            var anyNotConfirmed = await _db.Set<Contract_ContractParty>()
                .AnyAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                            && !EF.Functions.Like((x.ContractPartyStatus ?? "").ToLower(), PortQuery.Equal("CONFIRMED"), PortQuery.Esc));
            if (!anyNotConfirmed)
            {
                contract.ContractStatus = "CONFIRMED";
                contract.LogLUDTimeUTC = nowStr;
                contract.LogLUBy = waUserCode;
            }

            // biz:33154-33199 Contract_Contract_HistActionX (FunctionActionType="APPROVED", Desc="Ký Hợp đồng")
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
            await tx.CommitAsync();

            // Gate :2432-2437 — Success=true + AddMessage + RedirectUrl Detail
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ContractCode = contractCode,
                    ContractStatus = contract.ContractStatus,
                    FlagOrgCreate = flagOrgCreate,
                    FlagCheckOTP = flagCheckOTP,
                    ContractPartyStatus = parties.Select(p => p.ContractPartyStatus).ToList(),
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description,
                    InfoToken = tokenInfo,
                    Message = $"Duyệt hợp đồng '{contractCode}' thành công!",
                    RedirectUrl = $"/Contract_Contract/Detail?contractcode={contractCode}"
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Contract_Contract_Approved", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Kết thúc/chấm dứt hợp đồng (FinishAndSign).
    /// Gate: Contract_ContractController.cs:6767-7014
    ///       + BaseController.cs:6667-6737 (ReturnApiUserCreateContract — GAP: MasterServer ngoài)
    ///       + Contract_ContractController.cs:7980-7995 (strWhereClause_Contract_ContractSearch)
    ///       + BaseController.cs:6138-6144 (RT_Contract_Contract_Get)
    /// API:  ContractContractPartyController.cs:91-159 (WA_Contract_ContractParty_Finish),
    ///       ContractContractPartyController.cs:304-372 (WA_Contract_ContractParty_UpdEmailSend)
    /// BIZ:  zTemp.cs:4485-4638 (WAS_Contract_ContractParty_Finish) → zTemp.cs:3205-4416 (Contract_ContractParty_FinishX)
    ///       zTemp.1.cs:924-1017 (WAS_Contract_ContractParty_UpdEmailSend) → zTemp.1.cs:770-923 (Contract_ContractParty_UpdEmailSend)
    ///       zTemp.1.cs:581-769 (Contract_ContractParty_UpdEmailSendX)
    ///
    /// GAP — KHÔNG port phần gọi dịch vụ ngoài (PORT_RULES §1 + LESSONS B.7):
    ///   - ReturnApiUserCreateContract (gate:6831-6836, BaseController.cs:6667-6737) — MasterServer ngoài.
    ///   - OS_FileCenter_CreateAndMoveFSX (biz:3433, 3627) — FileCenter ngoài ⇒ ContractFinishReasonFilePath /
    ///     Contract_ContractFiles.ContractFilePath KHÔNG lấy được URL từ FS.
    ///   - RenderRazorViewToString + SendMailGun (gate:6894-6964) — render view + gateway mail ngoài.
    ///   - Contract_Contract_HistActionX (biz:3894) — hàm chưa trích (excerpt:4637) ⇒ ghi hist theo mẫu
    ///     Contract_ContractPartyCancelControllerPort (FunctionActionType='PartyFinsh', Desc='Kết thúc hợp đồng').
    ///   - Notify iNOS/Firebase (biz:3913-4065) — dịch vụ ngoài.
    ///   - Contract_Contract_CheckDB / Contract_ContractParty_CheckDB (biz:3305, 3338) — hàm chưa trích; dùng mã lỗi
    ///     đã xác minh ở các port cùng bảng (Contract_Contract_CheckDB_OrganNotFound,
    ///     Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched, Contract_ContractParty_CheckDB_OrganNotFound).
    ///   - Cột không có trên entity đích: EffStatus, ContractFinishReasonFilePath, ContractFinishReasonFilePathVersion,
    ///     FinishedDTimeUTC/FinishedBy của party, Contract_ContractFiles (bảng chưa có) ⇒ bỏ các guard/ghi tương ứng.
    /// </summary>
    [HttpPost("finishandsign")]
    public async Task<IActionResult> FinishAndSign([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_FinishAndSignReq? req)
    {
        // Gate :6789 — model là chuỗi JSON ContractModelFinish; body rỗng ⇒ objContractModelFinish = null
        // ⇒ Contract_ContractParty.ContractCode = null ⇒ biz StdParam(null) = null ⇒ lỗi CheckDB (OrganNotFound).
        Contract_Contract_FinishAndSignReq? model = req;
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<Contract_Contract_FinishAndSignReq>(
                    req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) model = parsed;
            }
            catch
            {
                // Bỏ qua lỗi parse — dùng các trường phẳng của req (A.21)
            }
        }

        // Gate :6792-6807 — Contract_ContractPartyFinish (CUtils.StrValue: null -> "", KHÔNG Trim/ToUpper)
        var partyFinish = model?.Contract_ContractPartyFinish;
        var contractCodeRaw = StrValue(partyFinish?.ContractCode);
        var contractFinishReasonCodeRaw = StrValue(partyFinish?.ContractFinishReasonCode);
        var contractFileSpecRaw = StrValue(partyFinish?.ContractFileSpec);
        var contractFileNameRaw = StrValue(partyFinish?.ContractFileName);
        var finishFileDescRaw = StrValue(partyFinish?.FinishFileDesc);
        var contractFinishReasonNameRaw = StrValue(partyFinish?.ContractFinishReasonName);
        var finishDescriptionRaw = StrValue(partyFinish?.FinishDescription);
        var finishedDateRaw = StrValue(partyFinish?.FinishedDate);
        var contractFinishReasonFilePathVersionRaw = StrValue(partyFinish?.ContractFinishReasonFilePathVersion);

        // Gate :6803 — MST lấy từ UserState.Mst_NNT.MST; đích không có phiên ⇒ nhận qua body (A.24)
        var mstRaw = !string.IsNullOrEmpty(StrValue(model?.MST)) ? StrValue(model?.MST) : StrValue(req?.MST);

        // Gate :6811-6825 — Lst_Contract_ContractFiles (Idx tăng dần, FlagPublic = IsPublic ? "1" : "0")
        // GAP: bảng Contract_ContractFiles KHÔNG có entity ở đích ⇒ chỉ dựng danh sách để đếm/echo,
        //      KHÔNG ghi DB (biz:3556-3685 bỏ insert).
        var fileList = new List<Contract_Contract_FinishAndSignFile>();
        if (model?.FileList != null && model.FileList.Count > 0)
        {
            fileList.AddRange(model.FileList);
        }

        // Gate :6830 — contractcode = CUtils.StrValue(...ContractCode)
        var contractcode = contractCodeRaw;

        // Người thực hiện — nguồn lấy UserState.SysUser.UserCode (gate:6781); đích không có phiên (A.24)
        var waUserCode = StdParam(model?.WAUserCode)
            ?? StdParam(Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? "port";

        // Gate :6783-6784 — NetworkID/OrgID của phiên (UserState.Mst_NNT) ⇒ nhận qua body (A.24)
        var sessionNetworkId = StdParam(model?.NetworkID);
        var sessionOrgId = StdParam(model?.OrgID);

        // Gate :6780 — AccessToken của phiên; rỗng ⇒ kiểm UserToken (biz:3408-3423)
        var accessToken = StrValue(model?.AccessToken);

        var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // ===== biz:3269-3271 — StdParam cho các MÃ =====
            var strContractCode = StdParam(contractCodeRaw);
            var strMST = StdParam(mstRaw);
            var strContractFinishReasonCode = StdParam(contractFinishReasonCodeRaw);
            // biz:3272-3277 — string.Format("{0}", x).Trim() (KHÔNG ToUpper)
            var strContractFileSpec = contractFileSpecRaw.Trim();
            var strContractFileName = contractFileNameRaw.Trim();
            var strFinishFileDesc = finishFileDescRaw.Trim();
            var strContractFinishReasonName = contractFinishReasonNameRaw.Trim();
            var strFinishDescription = finishDescriptionRaw.Trim();
            var strUserNameSign = StrValue(partyFinish?.UserNameSign).Trim();
            // biz:3278 — StdDate(objFinishedDate)
            var strFinishedDate = StdDate(finishedDateRaw);
            var strContractFinishReasonFilePathVersion = contractFinishReasonFilePathVersionRaw;
            var strUserTokenInput = StrValue(partyFinish?.UserToken);

            // ===== biz:3305-3311 — Contract_Contract_CheckDB(code, "1", Confirmed) =====
            var contract = await _db.Set<Contract_Contract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode ?? ""), PortQuery.Esc));
            if (contract == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_CheckDB_OrganNotFound", "Check.ContractCode", strContractCode);
            }
            // biz:3309 — ContractStatus phải thuộc danh sách Confirmed
            if (!string.Equals(contract.ContractStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched",
                    "Check.ContractCode", strContractCode,
                    "Check.strContractListToCheck", "CONFIRMED",
                    "DB.ContractStatus", contract.ContractStatus);
            }

            // biz:3312-3316 — OrgID/NetworkID/EffStatus/ContractFinishReasonFilePath(+Version) của hợp đồng
            // GAP: EffStatus, ContractFinishReasonFilePath, ContractFinishReasonFilePathVersion KHÔNG có trên entity đích.
            var strOrgID = StdParam(contract.OrgID);
            var strNetworkID = StdParam(sessionNetworkId ?? contract.OrgID);

            // biz:3338-3345 — Contract_ContractParty_CheckDB(code, MST, "1", Confirmed)
            var party = await _db.Set<Contract_ContractParty>()
                .FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode ?? ""), PortQuery.Esc) &&
                    EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST ?? ""), PortQuery.Esc));
            if (party == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_ContractParty_CheckDB_OrganNotFound",
                    "Check.ContractCode", strContractCode,
                    "Check.MST", strMST);
            }
            // biz:3343 — ContractPartyStatus phải thuộc danh sách Confirmed
            if (!string.Equals(party.ContractPartyStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_ContractParty_CheckDB_CheckDB_ContractPartyStatusNotMatched",
                    "Check.ContractCode", strContractCode,
                    "Check.MST", strMST,
                    "Check.strContractPartyStatusListToCheck", "CONFIRMED",
                    "DB.ContractPartyStatus", party.ContractPartyStatus);
            }

            // biz:3347-3351 — OrgID/CustomerCodeSys/NetworkIDParty/OrgIDParty/UserToken của party
            var strCustomerCodeSys = party.CustomerCodeSys;
            var strUserToken = party.UserToken;

            // biz:3354-3365 — EffStatus phải = Processing (GAP: cột EffStatus không có ở đích ⇒ bỏ guard)

            // biz:3366-3378 — ContractFinishReasonCode NOT NULL
            if (string.IsNullOrEmpty(strContractFinishReasonCode))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_ContractParty_FinishX_ContractFinishReasonCodeNotNull",
                    "Check.ContractCode", strContractCode,
                    "Check.strMST", strMST,
                    "Check.strContractFinishReasonCode", strContractFinishReasonCode);
            }

            // biz:3380-3392 — FinishedDate NOT NULL
            if (string.IsNullOrEmpty(strFinishedDate))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_ContractParty_FinishX_FinishedDateNotNull",
                    "Check.ContractCode", strContractCode,
                    "Check.strMST", strMST,
                    "Check.strFinishedDate", strFinishedDate);
            }

            // biz:3394-3406 — ContractFileSpec NOT NULL
            if (string.IsNullOrEmpty(strContractFileSpec))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_ContractParty_FinishX_ContractFileSpecNotNull",
                    "Check.ContractCode", strContractCode,
                    "Check.strMST", strMST,
                    "Check.strContractFileSpec", strContractFileSpec);
            }

            // biz:3408-3423 — không có AccessToken ⇒ UserToken phải khớp party.UserToken
            if (string.IsNullOrEmpty(accessToken))
            {
                if (!string.Equals(strUserToken, strUserTokenInput, StringComparison.Ordinal))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Contract_ContractParty_FinishX_Invalid_UserTokenInput",
                        "Check.ContractCode", strContractCode,
                        "Check.MST", strMST,
                        "Check.UserTokenInput", strUserTokenInput);
                }
            }

            // biz:3426-3453 — lần đầu kết thúc: upload file qua FileCenter (GAP: dịch vụ ngoài)
            //   ⇒ ContractFinishReasonFilePath không có giá trị (cột đích không có).

            // ===== biz:3503-3551 — UPDATE Contract_ContractParty =====
            // GAP: ContractFinishReasonFilePath, FinishedDTimeUTC, FinishedBy không có trên entity đích.
            party.ContractFinishReasonCode = strContractFinishReasonCode;
            party.ContractFinishReasonName = strContractFinishReasonName;
            party.FinishFileDesc = strFinishFileDesc;
            party.FinishDescription = strFinishDescription;
            party.ContractPartyStatus = "FINISHED";
            party.LogLUDTimeUTC = dtimeSys;
            party.LogLUBy = waUserCode;

            // ===== biz:3556-3685 — Contract_ContractFiles (GAP: bảng chưa có ở đích ⇒ bỏ insert) =====

            // ===== biz:3749-3770 — UPDATE Contract_Contract =====
            // GAP: ContractFinishReasonFilePath, ContractFinishReasonFilePathVersion, FinishedDTimeUTC không có trên entity đích.
            contract.ContractFinishReasonCode = strContractFinishReasonCode;
            contract.ContractFinishReasonName = strContractFinishReasonName;
            contract.FinishDescription = strFinishDescription;
            contract.FinishFileDesc = strFinishFileDesc;
            contract.LogLUDTimeUTC = dtimeSys;
            contract.LogLUBy = waUserCode;

            await _db.SaveChangesAsync();

            // ===== biz:3827-3869 — Check PartySign: mọi party FINISHED ⇒ Contract.EffStatus = Finished =====
            // GAP: cột EffStatus không có ở đích ⇒ chỉ ghi LogLUDTimeUTC/LogLUBy (biz:3852-3853).
            var anyNotFinished = await _db.Set<Contract_ContractParty>()
                .AnyAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode ?? ""), PortQuery.Esc)
                            && !EF.Functions.Like((x.ContractPartyStatus ?? "").ToLower(), PortQuery.Equal("FINISHED"), PortQuery.Esc));
            if (!anyNotFinished)
            {
                contract.LogLUDTimeUTC = dtimeSys;
                contract.LogLUBy = waUserCode;
            }

            // ===== biz:3887-3910 — Contract_Contract_HistActionX =====
            // GAP: hàm chưa trích ⇒ ghi theo mẫu port cùng bảng (Contract_ContractPartyCancelControllerPort).
            var maxVersion = await _db.Set<Contract_Contract_HistAction>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode ?? ""), PortQuery.Esc))
                .MaxAsync(x => (int?)x.Version) ?? 0;
            var hist = new Contract_Contract_HistAction
            {
                ContractCode = strContractCode ?? "",
                Version = maxVersion + 1,
                UserCode = waUserCode,
                UserName = strUserNameSign,
                Description = "Kết thúc hợp đồng",
                LogLUDTimeUTC = dtimeSys,
                LogLUBy = waUserCode,
                FunctionName = "Contract_ContractParty_FinishX",
                FunctionActionType = "PartyFinsh"
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);

            await _db.SaveChangesAsync();

            // ===== GAP: Notify iNOS/Firebase (biz:3913-4065) — dịch vụ ngoài =====

            // ===== Gate :6843-6877 — Search lại hợp đồng để gửi mail =====
            // strWhereClause_Contract_ContractSearch (gate:7980-7995) chỉ lọc ContractNo/OrgID ⇒ gate KHÔNG lọc gì
            // (ContractCode không phải tham số của hàm) ⇒ lấy dòng đầu theo ContractDateUTC desc, CreateDTimeUTC desc.
            var sendMailContract = await _db.Set<Contract_Contract>()
                .AsNoTracking()
                .OrderByDescending(x => x.ContractDateUTC)
                .ThenByDescending(x => x.CreateDTimeUTC)
                .FirstOrDefaultAsync();

            // ===== Gate :6882-6886 — tổng hợp dữ liệu gửi mail =====
            var partiesOfContract = await _db.Set<Contract_ContractParty>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode ?? ""), PortQuery.Esc))
                .ToListAsync();

            // Gate :6916-6993 — chỉ gửi khi có ĐÚNG 1 party đã kết thúc (FinishedDTimeUTC NOT NULL)
            // GAP: cột FinishedDTimeUTC không có trên entity đích ⇒ không xác định được "party đã kết thúc"
            //      ⇒ nhánh gửi mail + UpdEmailSend KHÔNG chạy (ghi GAP).
            var partiesToUpdate = new List<Contract_ContractParty>();

            // ===== Gate :6976-6990 — WA_Contract_ContractParty_UpdEmailSend =====
            // biz zTemp.1.cs:726-751 — UPDATE Contract_ContractParty SET EmailSend ... theo (ContractCode, MST)
            foreach (var item in partiesToUpdate)
            {
                var target = await _db.Set<Contract_ContractParty>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode ?? ""), PortQuery.Esc)
                                            && EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(item.MST ?? ""), PortQuery.Esc));
                if (target != null)
                {
                    target.EmailSend = item.EmailSend;
                    target.SendEmailDTimeUTC = dtimeSys;
                    target.SendEmailBy = waUserCode;
                    target.LogLUDTimeUTC = dtimeSys;
                    target.LogLUBy = waUserCode;
                }
            }
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Gate :7000-7005 — Success=true + AddMessage + RedirectUrl Detail
            var message = "Kết thúc hợp đồng '" + contractcode + "' thành công!";
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ContractCode = contractcode,
                    ContractStatus = contract.ContractStatus,
                    ContractPartyStatus = party.ContractPartyStatus,
                    ContractFinishReasonCode = party.ContractFinishReasonCode,
                    ContractFinishReasonName = party.ContractFinishReasonName,
                    FinishDescription = party.FinishDescription,
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description,
                    SearchResultFound = sendMailContract != null,
                    PartiesOfContractCount = partiesOfContract.Count,
                    PartiesUpdatedCount = partiesToUpdate.Count,
                    FileListCount = fileList.Count,
                    Message = message,
                    RedirectUrl = "/Contract_Contract/Detail?contractcode=" + contractcode
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Mã mặc định biz: TError.ErridQContract.Contract_ContractParty_Finish (zTemp.cs:4480)
            return PortResult.Fail("Contract_ContractParty_Finish", "Exception", ex.Message);
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

    /// <summary>CUtils.StdFlag của nguồn: "1" → "1", mọi giá trị khác kể cả null → "0".</summary>
    private static string StdFlag(object? value)
    {
        var s = value?.ToString()?.Trim();
        return s == "1" ? "1" : "0";
    }

    /// <summary>CUtils.StrValue của nguồn: null -> "", KHÔNG Trim/ToUpper (gate :718-721, :751).</summary>
    private static string? StrValue(object? value) => value?.ToString() ?? "";

    /// <summary>
    /// CUtils.StdDate của nguồn (biz:3278): chuỗi ngày hợp lệ -> "yyyy-MM-dd HH:mm:ss", không hợp lệ/rỗng -> null.
    /// </summary>
    private static string? StdDate(object? value)
    {
        var s = value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        return null;
    }
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

/// <summary>Tham số của gate Contract_ContractController.cs:415-436 (Index).</summary>
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

/// <summary>Tham số của gate Contract_ContractController.cs:700-701 (DoSearch — ContractSearchModel).</summary>
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

/// <summary>Tham số của gate Contract_ContractController.cs:860 (ValidateContractNoExists).</summary>
public class Contract_Contract_ValidateContractNoExistsReq
{
    public string? ContractNo { get; set; }
    public string? OrgID { get; set; }
}

/// <summary>Tham số của gate Contract_ContractController.cs:923 (Delete).</summary>
public class Contract_Contract_DeleteReq
{
    public string? Model { get; set; }
    public string? FlagRedirect { get; set; }
    public List<Contract_Contract_DeleteItem>? Lst_Contract_Contract { get; set; }
    public string? ContractCode { get; set; }
    public string? ContractName { get; set; }
}

/// <summary>Mục phần tử hợp đồng cần xóa (gate Contract_ContractController.cs:928, 934, 1109).</summary>
public class Contract_Contract_DeleteItem
{
    public string? ContractCode { get; set; }
    public string? ContractName { get; set; }
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:3306 (ApprovedSignAndSendMail_Old).
/// Client gửi ContractModel dạng chuỗi JSON (gate:3317) hoặc phẳng các trường của ContractModel.
/// </summary>
public class Contract_Contract_ApprovedSignAndSendMail_OldReq
{
    /// <summary>Chuỗi JSON ContractModel (gate:3317).</summary>
    public string? Model { get; set; }

    // Các trường ContractModel (gate:3334-3338)
    public string? ContractCode { get; set; }
    public string? SignedFileContent { get; set; }
    public string? ContractFileName { get; set; }
    public string? FlagElement { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagOrgCreate { get; set; }
    public string? ContractFileVersion { get; set; }

    /// <summary>Danh sách đối tác (gate:3477, PartyList) — GAP: không port (phần mail commented out).</summary>
    public List<object>? PartyList { get; set; }
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:2214 (ApprovedAndSign).
/// Client có thể gửi `model` (chuỗi JSON ContractModel) + `contractelement` (JSON) + `strsigrec` (JSON) như gate,
/// HOẶC gửi phẳng các trường của ContractModel + danh sách (bài học A.21 — KHÔNG bịa body khác nguồn).
/// </summary>
public class Contract_Contract_ApprovedAndSignReq
{
    /// <summary>Chuỗi JSON ContractModel (đường dẫn thô của gate: `model`).</summary>
    public string? Model { get; set; }

    // Các trường ContractModel (gate:2369-2379, 2415-2423)
    public string? ContractCode { get; set; }
    public string? ContractFileName { get; set; }
    public string? ContractFileVersion { get; set; }
    public string? SignedFileContent { get; set; }
    public string? FlagElement { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagOrgCreate { get; set; }
    public string? OtpCode { get; set; }

    /// <summary>Danh sách Contract_ContractElement (gate:2238, 2376).</summary>
    public List<Contract_ContractElementItem>? ContractElement { get; set; }

    /// <summary>Danh sách SignatureRectangle (gate:2230-2232, 2331-2336).</summary>
    public List<Contract_SignatureRectangleItem>? StrSigRec { get; set; }

    /// <summary>Người thực hiện — nguồn lấy UserState.SysUser.UserCode (gate:2360); đích không có phiên (bài học A.24).</summary>
    public string? WAUserCode { get; set; }
}

/// <summary>Phần tử của Contract_ContractElement (gate:2338-2348) — GAP: cột SignTypeCTS/TimeOfSigning/ElementType không có trên entity đích.</summary>
public class Contract_ContractElementItem
{
    public string? ContractCode { get; set; }
    public string? ElementType { get; set; }
    public string? SignType { get; set; }
    public string? SignTypeCTS { get; set; }
    public string? TimeOfSigning { get; set; }
}

/// <summary>Phần tử của SignatureRectangle (gate:2228-2237, 2331-2336).</summary>
public class Contract_SignatureRectangleItem
{
    public string? SignType { get; set; }
    public string? TimeOfSigning { get; set; }
    public object? TokenInfo { get; set; }
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:6767 (FinishAndSign — `string model` = ContractModelFinish).
/// A.21: body là đối tượng JSON tương ứng model (KHÔNG bọc thêm strJson).
/// Các trường NetworkID/OrgID/AccessToken/WAUserCode/MST là "UserState" của nguồn — đích nhận qua body (A.24).
/// </summary>
public class Contract_Contract_FinishAndSignReq
{
    /// <summary>Chuỗi JSON ContractModelFinish (đường dẫn thô của gate: `model`).</summary>
    public string? Model { get; set; }

    // UserState thay thế (gate:6780-6784, 6803)
    public string? AccessToken { get; set; }
    public string? WAUserCode { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? MST { get; set; }

    /// <summary>Gate :6792 — objContractModelFinish.Contract_ContractPartyFinish</summary>
    public Contract_Contract_FinishAndSignParty? Contract_ContractPartyFinish { get; set; }

    /// <summary>Gate :6808-6825 — objContractModelFinish.FileList</summary>
    public List<Contract_Contract_FinishAndSignFile>? FileList { get; set; }
}

/// <summary>Gate :6792-6807 — Contract_ContractPartyFinish (CUtils.StrValue từng trường).</summary>
public class Contract_Contract_FinishAndSignParty
{
    public string? ContractCode { get; set; }
    public string? ContractFinishReasonCode { get; set; }
    public string? ContractFileSpec { get; set; }
    public string? ContractFileName { get; set; }
    public string? FinishFileDesc { get; set; }
    public string? ContractFinishReasonName { get; set; }
    public string? FinishDescription { get; set; }
    public string? FinishedDate { get; set; }
    public string? ContractFinishReasonFilePathVersion { get; set; }
    public string? UserToken { get; set; }
    public string? UserNameSign { get; set; }
}

/// <summary>Gate :6811-6825 — phần tử FileList của ContractModelFinish.</summary>
public class Contract_Contract_FinishAndSignFile
{
    public string? FileName { get; set; }
    public string? Description { get; set; }
    public string? IsFilePath { get; set; }
    public bool IsPublic { get; set; }
    public string? FilePath { get; set; }
    public string? FileId { get; set; }
}
