using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Contract_ContractController.cs:415-698 (Index)
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
