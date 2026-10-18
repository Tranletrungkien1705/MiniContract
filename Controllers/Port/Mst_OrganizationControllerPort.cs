using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_OrganizationController.cs — Mst_NNT
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_OrganizationController.cs:25-109
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstNNTController.cs:19-89
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:12121-12218, 12314-12444, 25188-25368
[ApiController]
[Route("api/mst_organization")]
public class Mst_OrganizationControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_OrganizationControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tìm kiếm / phân trang danh sách tổ chức (người nộp thuế - Mst_NNT).
    /// Gate: Mst_OrganizationController.cs:25-109 Index(string init = "init", int recordcount = 10, int page = 0)
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Organization_IndexReq? req)
    {
        // 1. Chuẩn hoá phân trang theo tham số gate: recordcount (mặc định 10), page (mặc định 0)
        // Mst_OrganizationController.cs:25, 79-80
        var pageSize = req?.RecordCount ?? req?.Ft_PageSize ?? 10;
        var pageIndex = req?.Page ?? req?.Ft_PageIndex ?? 0;
        long recordStart = (long)pageIndex * pageSize;

        // 2. Dựng truy vấn Mst_NNT theo Master.cs:25244-25256
        var query = _db.Set<Mst_NNT>().AsNoTracking();

        // Lọc theo OrgID nếu có truyền (UserState.Mst_NNT.OrgID ở gate)
        var orgId = StdParam(req?.OrgID);
        if (!string.IsNullOrEmpty(orgId))
        {
            query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
        }

        // Lọc theo NetworkID nếu có truyền (UserState.Mst_NNT.NetworkID ở gate)
        var networkId = StdParam(req?.NetworkID);
        if (!string.IsNullOrEmpty(networkId))
        {
            query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
        }

        // Lọc theo FlagActive nếu có truyền
        var flagActive = StdFlag(req?.FlagActive);
        if (!string.IsNullOrEmpty(req?.FlagActive))
        {
            query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
        }

        // Lọc theo KeyWord nếu có truyền (tìm theo MST hoặc NNTFullName)
        var keyWord = StdStr(req?.KeyWord);
        if (!string.IsNullOrEmpty(keyWord))
        {
            var kwPattern = PortQuery.Contains(keyWord);
            query = query.Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), kwPattern, PortQuery.Esc)
                                  || EF.Functions.Like((x.NNTFullName ?? "").ToLower(), kwPattern, PortQuery.Esc));
        }

        // 3. Sắp xếp: order by mnnt.MST asc (Master.cs:25255, 25314)
        query = query.OrderBy(x => x.MST);

        // 4. Đếm tổng số bản ghi (Master.cs:25259: select Count(0) MyCount from #tbl_Mst_NNT_Filter_Draft)
        var itemCount = await query.CountAsync();

        // 5. Phân trang (Master.cs:25268-25269, LESSON A.7, A.12)
        var items = (pageSize <= 0 || recordStart < 0)
            ? new List<Mst_NNT>()
            : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                         .Take((int)Math.Min(pageSize, int.MaxValue))
                         .ToListAsync();

        // 6. Ghép thông tin từ bảng liên kết Mst_Area (Master.cs:25311-25313: left join Mst_Area)
        var areas = await _db.Set<Mst_Area>().AsNoTracking().ToListAsync();
        var areaDict = areas.ToDictionary(a => $"{a.OrgID}_{a.AreaCode}", a => a.AreaName ?? "", StringComparer.OrdinalIgnoreCase);

        var dataList = items.Select((item, idx) =>
        {
            var key = $"{item.OrgID}_{item.AreaCode}";
            areaDict.TryGetValue(key, out var areaName);
            return new Mst_NNT_IndexItem(item)
            {
                MyIdxSeq = recordStart + idx,
                ma_AreaName = areaName
            };
        }).ToList();

        // 7. Tính PageCount (Mst_OrganizationController.cs:95)
        var pageCount = 0;
        if (dataList.Count > 0 && pageSize > 0)
        {
            pageCount = (itemCount % pageSize == 0) ? (itemCount / pageSize) : (itemCount / pageSize + 1);
        }

        var pageInfo = new PageInfo<Mst_NNT_IndexItem>(dataList, itemCount, pageCount, pageIndex, pageSize);

        // 8. Trả kết quả chuẩn REST Ok(Success, Data)
        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }

    private static string? StdParam(string? value)
    {
        var s = value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    private static string? StdStr(string? value)
    {
        var s = value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static string StdFlag(string? value)
    {
        return value?.Trim() == "1" ? "1" : "0";
    }
}

public class Mst_Organization_IndexReq
{
    public string? Init { get; set; } = "init";
    public int? RecordCount { get; set; }
    public int? Page { get; set; }
    public int? Ft_PageSize { get; set; }
    public int? Ft_PageIndex { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
    public string? KeyWord { get; set; }
}

public class Mst_NNT_IndexItem
{
    public string MST { get; set; } = "";
    public string? OrgID { get; set; }
    public string? NNTFullName { get; set; }
    public string? NNTShortName { get; set; }
    public string? NetworkID { get; set; }
    public string? MSTParent { get; set; }
    public string? MSTBUCode { get; set; }
    public string? MSTBUPattern { get; set; }
    public string? MSTLevel { get; set; }
    public string? AreaCode { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? DLCode { get; set; }
    public string? NNTAddress { get; set; }
    public string? NNTMobile { get; set; }
    public string? NNTPhone { get; set; }
    public string? NNTFax { get; set; }
    public string? PresentBy { get; set; }
    public string? BusinessRegNo { get; set; }
    public string? NNTPosition { get; set; }
    public string? PresentIDNo { get; set; }
    public string? PresentIDType { get; set; }
    public string? GovTaxID { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }
    public string? CANumber { get; set; }
    public string? CAOrg { get; set; }
    public string? CAEffDTimeUTCStart { get; set; }
    public string? CAEffDTimeUTCEnd { get; set; }
    public string? PackageCode { get; set; }
    public string? CreatedDate { get; set; }
    public string? CreateDTime { get; set; }
    public string? CreateBy { get; set; }
    public string? AccNo { get; set; }
    public string? AccHolder { get; set; }
    public string? BankName { get; set; }
    public string? BizType { get; set; }
    public string? BizFieldCode { get; set; }
    public string? BizSizeCode { get; set; }
    public string? DealerType { get; set; }
    public string? RegisterStatus { get; set; }
    public string? TCTStatus { get; set; }
    public string? FlagActive { get; set; }
    public string? Remark { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // Các cột bổ sung từ bảng liên kết (Master.cs:25288-25301)
    public long? MyIdxSeq { get; set; }
    public string? ma_AreaName { get; set; }
    public string? mgt_GovTaxID { get; set; }
    public string? mgt_GovTaxName { get; set; }
    public string? mp_ProvinceCode { get; set; }
    public string? mp_ProvinceName { get; set; }
    public string? md_DistrictCode { get; set; }
    public string? md_DistrictName { get; set; }

    public Mst_NNT_IndexItem() { }

    public Mst_NNT_IndexItem(Mst_NNT src)
    {
        MST = src.MST;
        OrgID = src.OrgID;
        NNTFullName = src.NNTFullName;
        NNTShortName = src.NNTShortName;
        NetworkID = src.NetworkID;
        MSTParent = src.MSTParent;
        MSTBUCode = src.MSTBUCode;
        MSTBUPattern = src.MSTBUPattern;
        MSTLevel = src.MSTLevel;
        AreaCode = src.AreaCode;
        ProvinceCode = src.ProvinceCode;
        DistrictCode = src.DistrictCode;
        DLCode = src.DLCode;
        NNTAddress = src.NNTAddress;
        NNTMobile = src.NNTMobile;
        NNTPhone = src.NNTPhone;
        NNTFax = src.NNTFax;
        PresentBy = src.PresentBy;
        BusinessRegNo = src.BusinessRegNo;
        NNTPosition = src.NNTPosition;
        PresentIDNo = src.PresentIDNo;
        PresentIDType = src.PresentIDType;
        GovTaxID = src.GovTaxID;
        ContactName = src.ContactName;
        ContactPhone = src.ContactPhone;
        ContactEmail = src.ContactEmail;
        Website = src.Website;
        CANumber = src.CANumber;
        CAOrg = src.CAOrg;
        CAEffDTimeUTCStart = src.CAEffDTimeUTCStart;
        CAEffDTimeUTCEnd = src.CAEffDTimeUTCEnd;
        PackageCode = src.PackageCode;
        CreatedDate = src.CreatedDate;
        CreateDTime = src.CreateDTime;
        CreateBy = src.CreateBy;
        AccNo = src.AccNo;
        AccHolder = src.AccHolder;
        BankName = src.BankName;
        BizType = src.BizType;
        BizFieldCode = src.BizFieldCode;
        BizSizeCode = src.BizSizeCode;
        DealerType = src.DealerType;
        RegisterStatus = src.RegisterStatus;
        TCTStatus = src.TCTStatus;
        FlagActive = src.FlagActive;
        Remark = src.Remark;
        LogLUDTimeUTC = src.LogLUDTimeUTC;
        LogLUBy = src.LogLUBy;
    }
}
