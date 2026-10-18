using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ProductController.cs — Mst_Product
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ProductController.cs:29-200 (Index), 241-391 (DoSearch), 202-237 (GetPrdBase), 4964-5004 (strWhereClause_Mst_Product), 5076-5187 (strWhereClause_Mst_Product_Search)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstProductController.cs:19-87 (WA_Mst_Product_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Product\Product.cs:4230-4643 (Mst_Product_Get), 4644-4802 (WAS_Mst_Product_Get)
[ApiController]
[Route("api/mst_product")]
public class Mst_ProductControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Mst_ProductControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Danh sách / Tìm kiếm phân trang sản phẩm gốc (ProductLevelSys = ROOTPRD).
    /// Gate: Mst_ProductController.cs:29-200 Index(...)
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_ProductController.cs:37-38, 105-106:
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            // 2. Dựng truy vấn cơ sở từ bảng Mst_Product (Product.cs:4352, 4420)
            var query = _db.Set<Mst_Product>().AsNoTracking();

            // 3. Mặc định lọc theo ProductLevelSys = ROOTPRD như gate Index thiết lập (Gate Mst_ProductController.cs:88, 111)
            var levelSys = !string.IsNullOrEmpty(req?.ProductLevelSys)
                ? StdParam(req.ProductLevelSys) ?? "ROOTPRD"
                : "ROOTPRD";
            query = query.Where(x => EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal(levelSys), PortQuery.Esc));

            // 4. Lọc theo OrgID (Gate Mst_ProductController.cs:125; BIZ Product.cs:4353)
            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            // 5. Lọc theo NetworkID (Gate Mst_ProductController.cs:124)
            var networkId = StdParam(req?.NetworkID);
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            // 6. Lọc theo FlagActive (Gate Mst_ProductController.cs:89, 112)
            var flagActiveRaw = req?.FlagActive;
            if (!string.IsNullOrEmpty(flagActiveRaw))
            {
                var flagActive = StdFlag(flagActiveRaw);
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            // 7. Lọc theo ProductGrpCode (Gate Mst_ProductController.cs:4978-4981)
            var grpCode = StdParam(req?.ProductGrpCode);
            if (!string.IsNullOrEmpty(grpCode))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductGrpCode ?? "").ToLower(), PortQuery.Equal(grpCode), PortQuery.Esc));
            }

            // 8. Lọc theo ProductCodeUser (Gate Mst_ProductController.cs:4970-4973)
            var prdCodeUser = StdParam(req?.ProductCodeUser);
            if (!string.IsNullOrEmpty(prdCodeUser))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductCodeUser ?? "").ToLower(), PortQuery.Equal(prdCodeUser), PortQuery.Esc));
            }

            // 9. Lọc theo BrandCode (Gate Mst_ProductController.cs:4986-4989)
            var brandCode = StdParam(req?.BrandCode);
            if (!string.IsNullOrEmpty(brandCode))
            {
                query = query.Where(x => EF.Functions.Like((x.BrandCode ?? "").ToLower(), PortQuery.Equal(brandCode), PortQuery.Esc));
            }

            // 10. Lọc theo ProductType (Gate Mst_ProductController.cs:4990-4993)
            var prdType = StdParam(req?.ProductType);
            if (!string.IsNullOrEmpty(prdType))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal(prdType), PortQuery.Esc));
            }

            // 11. Lọc theo UnitCode (Gate Mst_ProductController.cs:4994-4997)
            var unitCode = StdParam(req?.UnitCode);
            if (!string.IsNullOrEmpty(unitCode))
            {
                query = query.Where(x => EF.Functions.Like((x.UnitCode ?? "").ToLower(), PortQuery.Equal(unitCode), PortQuery.Esc));
            }

            // 12. Lọc theo ProductName (Gate Mst_ProductController.cs:4998-5001)
            var prdName = StdStr(req?.ProductName);
            if (!string.IsNullOrEmpty(prdName))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductName ?? "").ToLower(), PortQuery.Contains(prdName), PortQuery.Esc));
            }

            // 13. Lọc theo KeyWord hoặc ProductCodeUserName (Gate Mst_ProductController.cs:30, 242)
            var keyWord = StdStr(req?.KeyWord ?? req?.ProductCodeUserName);
            if (!string.IsNullOrEmpty(keyWord))
            {
                var kwPattern = PortQuery.Contains(keyWord);
                query = query.Where(x => EF.Functions.Like((x.ProductName ?? "").ToLower(), kwPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.ProductCodeUser ?? "").ToLower(), kwPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.ProductCode ?? "").ToLower(), kwPattern, PortQuery.Esc));
            }

            // 14. Đếm tổng số bản ghi thoả mãn (BIZ Product.cs:4366: select Count(0) MyCount from ...)
            var itemCount = await query.CountAsync();

            // 15. Sắp xếp theo mp.OrgID, mp.ProductCode asc (BIZ Product.cs:4360-4362)
            var ordered = query.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode);

            // 16. Phân trang dữ liệu (LESSON A.7, A.12 chống tràn int và xử lý chỉ số âm/kích thước 0)
            var pagedRoots = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Product>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordCount, int.MaxValue))
                               .ToListAsync();

            // 17. Chuyển đổi sang Mst_ProductUI và đếm sản phẩm cơ sở GetPrdBase (Gate Mst_ProductController.cs:124-162, 202-237)
            var listGetProduct = new List<Mst_ProductUI>();
            foreach (var prdroot in pagedRoots)
            {
                var rootCode = prdroot.ProductCode;
                var itemOrgId = prdroot.OrgID;

                // Gate GetPrdBase: Mst_ProductController.cs:134, 202-237 đếm sản phẩm có ProductLevelSys = "BASEPRD" và ProductCodeRoot = prdroot.ProductCode
                var baseCount = await _db.Set<Mst_Product>()
                    .AsNoTracking()
                    .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(itemOrgId), PortQuery.Esc)
                             && EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal("BASEPRD"), PortQuery.Esc)
                             && EF.Functions.Like((x.ProductCodeRoot ?? "").ToLower(), PortQuery.Equal(rootCode), PortQuery.Esc))
                    .CountAsync();

                // Gate Mst_ProductController.cs:135: countbase = (listProductBase.Lst_Mst_Product.Count + 1).ToString();
                var countbase = (baseCount + 1).ToString();

                var item = new Mst_ProductUI
                {
                    CountPrdBase = countbase,
                    ProductCodeUser = prdroot.ProductCodeUser,
                    ProductName = prdroot.ProductName,
                    ProductType = prdroot.ProductType,
                    ProductGrpCode = prdroot.ProductGrpCode,
                    BrandCode = prdroot.BrandCode,
                    UnitCode = prdroot.UnitCode,
                    FlagBuy = prdroot.FlagBuy,
                    FlagActive = prdroot.FlagActive,
                    FlagSell = prdroot.FlagSell,
                    UPBuy = prdroot.UPBuy,
                    UPSell = prdroot.UPSell,
                    ProductCode = prdroot.ProductCode,
                    ProductCodeBase = prdroot.ProductCodeBase,
                    ProductCodeRoot = prdroot.ProductCodeRoot,
                    OrgID = prdroot.OrgID,
                    NetworkID = prdroot.NetworkID,
                    // GAP: Các bảng Mst_ProductType, Mst_ProductGroup, Mst_Brand, Mst_VATRate chưa có entity trong repo đích (Rule A.18)
                    mpt_ProductType = null,
                    mpt_ProductTypeName = null,
                    mpg_ProductGrpName = null,
                    mb_BrandName = null,
                    mvat_VATRate = null,
                    Remark = prdroot.Remark,
                    CustomField1 = prdroot.CustomField1,
                    CustomField2 = prdroot.CustomField2,
                    CustomField3 = prdroot.CustomField3,
                    CustomField4 = prdroot.CustomField4,
                    CustomField5 = prdroot.CustomField5,
                    CreateDTimeUTC = prdroot.CreateDTimeUTC,
                    CreateBy = prdroot.CreateBy,
                    LUDTimeUTC = prdroot.LUDTimeUTC,
                    LUBy = prdroot.LUBy,
                    LogLUDTimeUTC = prdroot.LogLUDTimeUTC,
                    LogLUBy = prdroot.LogLUBy
                };
                listGetProduct.Add(item);
            }

            // 18. Tính số trang (Gate Mst_ProductController.cs:169: (itemCount % recordcount == 0) ? itemCount / recordcount : itemCount / recordcount + 1)
            var pageCount = 0;
            if (listGetProduct.Count > 0)
            {
                var ps = pageSize <= 0 ? 10 : pageSize;
                pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
            }

            var pageInfo = new PageInfo<Mst_ProductUI>(listGetProduct, itemCount, pageCount, pageIndex, pageSize);

            // 19. Trả về cấu trúc PageInfo bọc trong Success/Data (Data/PortQuery.cs:31, LESSON A.5, A.11)
            return Ok(new
            {
                Success = true,
                Data = pageInfo
            });
        }
        catch (Exception ex)
        {
            // BIZ Product.cs:4257, 4620: strErrorCodeDefault = TError.ErridQContract.Mst_Product_Get
            return PortResult.Fail("Mst_Product_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Tìm kiếm nâng cao phân trang sản phẩm (ProductLevelSys = ROOTPRD).
    /// Gate: Mst_ProductController.cs:241-391 DoSearch(...)
    ///       Mst_ProductController.cs:5076-5187 strWhereClause_Mst_Product_Search(...)
    ///       Mst_ProductController.cs:202-237 GetPrdBase(...)
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    /// </summary>
    [HttpPost("dosearch")]
    public async Task<IActionResult> DoSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_DoSearchReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_ProductController.cs:250-251, 314-315
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            // 2. Dựng truy vấn cơ sở từ bảng Mst_Product (Product.cs:4352, 4420)
            var query = _db.Set<Mst_Product>().AsNoTracking();

            // 3. Mặc định lọc theo ProductLevelSys = ROOTPRD như gate DoSearch thiết lập (Gate Mst_ProductController.cs:288; BIZ Product.cs:5092-5095)
            var levelSys = !string.IsNullOrEmpty(req?.ProductLevelSys)
                ? StdParam(req.ProductLevelSys) ?? "ROOTPRD"
                : "ROOTPRD";
            query = query.Where(x => EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal(levelSys), PortQuery.Esc));

            // 4. Lọc theo OrgID (Gate Mst_ProductController.cs:311; BIZ Product.cs:4353)
            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            // 5. Lọc theo NetworkID (Gate Mst_ProductController.cs:310)
            var networkId = StdParam(req?.NetworkID);
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            // 6. Lọc theo checkbox ProductType: ckbproduct, ckbservices, ckbcombo (Gate Mst_ProductController.cs:271-284, 5175-5185)
            var selectedTypes = new List<string>();
            if (req?.CkbProduct == "1") selectedTypes.Add("PRODUCT");
            if (req?.CkbServices == "1") selectedTypes.Add("SERVICE");
            if (req?.CkbCombo == "1") selectedTypes.Add("COMBO");

            if (selectedTypes.Count > 0)
            {
                var lowerTypes = selectedTypes.Select(t => t.ToLowerInvariant()).ToList();
                query = query.Where(x => lowerTypes.Contains((x.ProductType ?? "").ToLower()));
            }
            else if (!string.IsNullOrEmpty(req?.ProductType))
            {
                var prdType = StdParam(req.ProductType);
                if (!string.IsNullOrEmpty(prdType))
                {
                    query = query.Where(x => EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal(prdType), PortQuery.Esc));
                }
            }

            // 7. Lọc theo FlagActive (Gate Mst_ProductController.cs:292, 5104-5107)
            if (!string.IsNullOrEmpty(req?.FlagActive))
            {
                var flag = StdFlag(req.FlagActive);
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flag), PortQuery.Esc));
            }

            // 8. Lọc theo ProductGrpCode (Gate Mst_ProductController.cs:291, 5088-5091)
            var grpCode = StdParam(req?.ProductGrpCode);
            if (!string.IsNullOrEmpty(grpCode))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductGrpCode ?? "").ToLower(), PortQuery.Equal(grpCode), PortQuery.Esc));
            }

            // 9. Lọc theo productCodeUserName: khớp ProductCodeUser (=) HOẶC ProductName (like '%...%') (Gate Mst_ProductController.cs:289-290, 5152-5173)
            var kw = StdStr(req?.ProductCodeUserName ?? req?.KeyWord);
            if (!string.IsNullOrEmpty(kw))
            {
                var kwEqual = PortQuery.Equal(kw);
                var kwContains = PortQuery.Contains(kw);
                query = query.Where(x => EF.Functions.Like((x.ProductCodeUser ?? "").ToLower(), kwEqual, PortQuery.Esc)
                                      || EF.Functions.Like((x.ProductName ?? "").ToLower(), kwContains, PortQuery.Esc));
            }
            else
            {
                var prdCodeUser = StdParam(req?.ProductCodeUser);
                if (!string.IsNullOrEmpty(prdCodeUser))
                {
                    query = query.Where(x => EF.Functions.Like((x.ProductCodeUser ?? "").ToLower(), PortQuery.Equal(prdCodeUser), PortQuery.Esc));
                }

                var prdName = StdStr(req?.ProductName);
                if (!string.IsNullOrEmpty(prdName))
                {
                    query = query.Where(x => EF.Functions.Like((x.ProductName ?? "").ToLower(), PortQuery.Contains(prdName), PortQuery.Esc));
                }
            }

            // 10. Lọc theo ProductCode nếu có (Gate Mst_ProductController.cs:5083-5086)
            var prdCode = StdParam(req?.ProductCode);
            if (!string.IsNullOrEmpty(prdCode))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductCode ?? "").ToLower(), PortQuery.Equal(prdCode), PortQuery.Esc));
            }

            // 11. Lọc theo BrandCode (Gate Mst_ProductController.cs:5096-5099)
            var brandCode = StdParam(req?.BrandCode);
            if (!string.IsNullOrEmpty(brandCode))
            {
                query = query.Where(x => EF.Functions.Like((x.BrandCode ?? "").ToLower(), PortQuery.Equal(brandCode), PortQuery.Esc));
            }

            // 12. Lọc theo UnitCode (Gate Mst_ProductController.cs:5100-5103)
            var unitCode = StdParam(req?.UnitCode);
            if (!string.IsNullOrEmpty(unitCode))
            {
                query = query.Where(x => EF.Functions.Like((x.UnitCode ?? "").ToLower(), PortQuery.Equal(unitCode), PortQuery.Esc));
            }

            // GAP: Lọc theo Prd_Attribute (Gate Mst_ProductController.cs:5110-5136):
            // Bảng Prd_Attribute chưa có entity trong repo đích (Rule A.18). Khi bảng được bổ sung sẽ kích hoạt lọc theo thuộc tính.

            // 13. Đếm tổng số bản ghi thoả mãn (BIZ Product.cs:4366: select Count(0) MyCount from ...)
            var itemCount = await query.CountAsync();

            // 14. Sắp xếp theo mp.OrgID, mp.ProductCode asc (BIZ Product.cs:4360-4362)
            var ordered = query.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode);

            // 15. Phân trang dữ liệu (LESSON A.7, A.12 chống tràn int và xử lý chỉ số âm/kích thước 0)
            var pagedRoots = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Product>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordCount, int.MaxValue))
                               .ToListAsync();

            // 16. Chuyển đổi sang Mst_ProductUI và đếm sản phẩm cơ sở GetPrdBase (Gate Mst_ProductController.cs:334-368, 202-237)
            var listGetProduct = new List<Mst_ProductUI>();
            foreach (var prdroot in pagedRoots)
            {
                var rootCode = prdroot.ProductCode;
                var itemOrgId = prdroot.OrgID;

                // Gate GetPrdBase: Mst_ProductController.cs:343, 202-237 đếm sản phẩm có ProductLevelSys = "BASEPRD" và ProductCodeRoot = prdroot.ProductCode
                var baseCount = await _db.Set<Mst_Product>()
                    .AsNoTracking()
                    .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(itemOrgId), PortQuery.Esc)
                             && EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal("BASEPRD"), PortQuery.Esc)
                             && EF.Functions.Like((x.ProductCodeRoot ?? "").ToLower(), PortQuery.Equal(rootCode), PortQuery.Esc))
                    .CountAsync();

                // Gate Mst_ProductController.cs:344: countbase = (listProductBase.Lst_Mst_Product.Count + 1).ToString();
                var countbase = (baseCount + 1).ToString();

                var item = new Mst_ProductUI
                {
                    CountPrdBase = countbase,
                    ProductCodeUser = prdroot.ProductCodeUser,
                    ProductName = prdroot.ProductName,
                    ProductType = prdroot.ProductType,
                    ProductGrpCode = prdroot.ProductGrpCode,
                    BrandCode = prdroot.BrandCode,
                    UnitCode = prdroot.UnitCode,
                    FlagBuy = prdroot.FlagBuy,
                    FlagActive = prdroot.FlagActive,
                    FlagSell = prdroot.FlagSell,
                    UPBuy = prdroot.UPBuy,
                    UPSell = prdroot.UPSell,
                    ProductCode = prdroot.ProductCode,
                    ProductCodeBase = prdroot.ProductCodeBase,
                    ProductCodeRoot = prdroot.ProductCodeRoot,
                    OrgID = prdroot.OrgID,
                    NetworkID = prdroot.NetworkID,
                    // GAP: Các bảng Mst_ProductType, Mst_ProductGroup, Mst_Brand, Mst_VATRate chưa có entity trong repo đích (Rule A.18)
                    mpt_ProductType = null,
                    mpt_ProductTypeName = null,
                    mpg_ProductGrpName = null,
                    mb_BrandName = null,
                    mvat_VATRate = null,
                    Remark = prdroot.Remark,
                    CustomField1 = prdroot.CustomField1,
                    CustomField2 = prdroot.CustomField2,
                    CustomField3 = prdroot.CustomField3,
                    CustomField4 = prdroot.CustomField4,
                    CustomField5 = prdroot.CustomField5,
                    CreateDTimeUTC = prdroot.CreateDTimeUTC,
                    CreateBy = prdroot.CreateBy,
                    LUDTimeUTC = prdroot.LUDTimeUTC,
                    LUBy = prdroot.LUBy,
                    LogLUDTimeUTC = prdroot.LogLUDTimeUTC,
                    LogLUBy = prdroot.LogLUBy
                };
                listGetProduct.Add(item);
            }

            // 17. Tính số trang (Gate Mst_ProductController.cs:375: (itemCount % recordcount == 0) ? itemCount / recordcount : itemCount / recordcount + 1)
            var pageCount = 0;
            if (listGetProduct.Count > 0)
            {
                var ps = pageSize <= 0 ? 10 : pageSize;
                pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
            }

            var pageInfo = new PageInfo<Mst_ProductUI>(listGetProduct, itemCount, pageCount, pageIndex, pageSize);

            // 18. Trả về cấu trúc PageInfo bọc trong Success/Data (Data/PortQuery.cs:31, LESSON A.5, A.11)
            return Ok(new
            {
                Success = true,
                Data = pageInfo
            });
        }
        catch (Exception ex)
        {
            // BIZ Product.cs:4257, 4620: strErrorCodeDefault = TError.ErridQContract.Mst_Product_Get
            return PortResult.Fail("Mst_Product_Get", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim().ToUpperInvariant();
    }

    private static string? StdStr(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim().Normalize();
    }

    private static string StdFlag(string? s)
    {
        return s?.Trim() == "1" ? "1" : "0";
    }
}

/// <summary>
/// Tham số đầu vào cho action Index của Mst_ProductController.
/// </summary>
public class Mst_Product_IndexReq
{
    // Gate Mst_ProductController.cs:29-38
    public string? ProductCodeUserName { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ProductGrpCode { get; set; }
    public string? CkbProduct { get; set; }
    public string? CkbServices { get; set; }
    public string? CkbCombo { get; set; }
    public string? FlagActive { get; set; }
    public string? Init { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    // Các tham số lọc bổ sung
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? KeyWord { get; set; }
    public string? ProductLevelSys { get; set; }
    public string? BrandCode { get; set; }
    public string? ProductType { get; set; }
    public string? UnitCode { get; set; }
    public string? ProductName { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action DoSearch của Mst_ProductController.
/// Gate: Mst_ProductController.cs:241-251
/// </summary>
public class Mst_Product_DoSearchReq
{
    // Gate Mst_ProductController.cs:241-251
    public string? ProductCodeUserName { get; set; }
    public string? ProductGrpCode { get; set; }
    public string? CkbProduct { get; set; }
    public string? CkbServices { get; set; }
    public string? CkbCombo { get; set; }
    public string? FlagActive { get; set; }
    public string? LstAttribute { get; set; }
    public string? Init { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    // Các tham số lọc bổ sung (LESSON A.14)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? KeyWord { get; set; }
    public string? ProductLevelSys { get; set; }
    public string? BrandCode { get; set; }
    public string? ProductType { get; set; }
    public string? UnitCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ProductCode { get; set; }
}

/// <summary>
/// DTO hiển thị danh sách sản phẩm theo Gate Mst_ProductController.cs:124-162 và ModelsUI/Mst_ProductUI.cs.
/// </summary>
public class Mst_ProductUI
{
    // Gate Mst_ProductController.cs:139-160
    public string? CountPrdBase { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ProductName { get; set; }
    public string? ProductType { get; set; }
    public string? ProductGrpCode { get; set; }
    public string? BrandCode { get; set; }
    public string? UnitCode { get; set; }
    public string? FlagBuy { get; set; }
    public string? FlagActive { get; set; }
    public string? FlagSell { get; set; }
    public double? UPBuy { get; set; }
    public double? UPSell { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductCodeBase { get; set; }
    public string? ProductCodeRoot { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? mpt_ProductType { get; set; }
    public string? mpt_ProductTypeName { get; set; }
    public string? mpg_ProductGrpName { get; set; }
    public string? mb_BrandName { get; set; }
    public double? mvat_VATRate { get; set; }
    public string? Remark { get; set; }
    public string? CustomField1 { get; set; }
    public string? CustomField2 { get; set; }
    public string? CustomField3 { get; set; }
    public string? CustomField4 { get; set; }
    public string? CustomField5 { get; set; }
    public string? CreateDTimeUTC { get; set; }
    public string? CreateBy { get; set; }
    public string? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}
