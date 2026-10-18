using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ProductController.cs — Mst_Product
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ProductController.cs:29-200 (Index), 241-391 (DoSearch), 395-440 (LoadData), 444-513 (Search_Product_Auto), 518-794 (Create GET), 796-1088 (Create POST), 1100-1152 (GetBOM), 202-237 (GetPrdBase), 4964-5004 (strWhereClause_Mst_Product), 5020-5036 (strWhereGetProductId), 5076-5187 (strWhereClause_Mst_Product_Search), 5188-5205 (strWhereClause_Mst_Product_Get_Base), 5206-5223 (strWhereClause_Mst_Product_Get_Level2), 5250-5312 (strWhereClause_Mst_ProductProductUserCodeOrProductName)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstProductController.cs:19-87 (WA_Mst_Product_Get), 89-158 (WA_Mst_Product_Save)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Product\Product.cs:4230-4643 (Mst_Product_Get), 4644-4802 (WAS_Mst_Product_Get), 4805-5550 (Mst_Product_Save, WAS_Mst_Product_Save, Mst_Product_SaveX)
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

    /// <summary>
    /// Lấy danh sách sản phẩm cơ sở và sản phẩm cấp 2 cho màn hình cập nhật sản phẩm.
    /// Gate: Mst_ProductController.cs:395-440 LoadData(string prcode, string prbscode, string prlv, string prroot, string idx)
    ///       Mst_ProductController.cs:5188-5205 strWhereClause_Mst_Product_Get_Base(...)
    ///       Mst_ProductController.cs:5206-5223 strWhereClause_Mst_Product_Get_Level2(...)
    ///       Mst_ProductController.cs:202-237 GetPrdBase(...)
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    /// </summary>
    [HttpPost("loaddata")]
    public async Task<IActionResult> LoadData([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_LoadDataReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào (Gate: Mst_ProductController.cs:395, 401)
            var prcode = StdParam(req?.prcode ?? req?.ProductCode);
            if (string.IsNullOrEmpty(prcode))
            {
                // Gate Mst_ProductController.cs:401: if (!CUtils.IsNullOrEmpty(prcode)) -> nếu rỗng thì resultEntry.Success = false
                return PortResult.Fail("Mst_Product_Get", "Check.prcode", null);
            }

            var orgId = StdParam(req?.OrgID);
            var networkId = StdParam(req?.NetworkID);

            // 2. Lấy Base product (strWhereClause_Mst_Product_Get_Base: ProductCodeRoot = prcode and ProductLevelSys != "L2PRD")
            // Gate Mst_ProductController.cs:409, 5188-5205; BIZ Product.cs:4352, 4420
            var baseQuery = _db.Set<Mst_Product>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.ProductCodeRoot ?? "").ToLower(), PortQuery.Equal(prcode), PortQuery.Esc)
                         && !EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal("L2PRD"), PortQuery.Esc));

            if (!string.IsNullOrEmpty(orgId))
            {
                baseQuery = baseQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                baseQuery = baseQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            var lstPrdBase = await baseQuery.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode).ToListAsync();

            // 3. Lấy Level2 product (strWhereClause_Mst_Product_Get_Level2: ProductCodeRoot = prcode and ProductLevelSys = "L2PRD")
            // Gate Mst_ProductController.cs:410, 5206-5223; BIZ Product.cs:4352, 4420
            var level2Query = _db.Set<Mst_Product>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.ProductCodeRoot ?? "").ToLower(), PortQuery.Equal(prcode), PortQuery.Esc)
                         && EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal("L2PRD"), PortQuery.Esc));

            if (!string.IsNullOrEmpty(orgId))
            {
                level2Query = level2Query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                level2Query = level2Query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            var lstPrdLevel2 = await level2Query.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode).ToListAsync();

            // 4. Gán vào Mst_ProductUI theo gate Mst_ProductController.cs:413-424
            var productbase = new Mst_ProductUI
            {
                LstPrdBase = lstPrdBase,
                LstPrdLeve2 = lstPrdLevel2,
                // GAP: Bảng Prd_Attribute chưa có entity trong repo đích (Rule A.18)
                LstAttributeBase = null,
                LstAttributeLevel2 = null
            };

            var listproductbase = new List<Mst_ProductUI> { productbase };

            // 5. Trả về JsonView("LoadData", listproductbase) (Gate: Mst_ProductController.cs:424)
            return Ok(new
            {
                Success = true,
                Data = listproductbase
            });
        }
        catch (Exception ex)
        {
            // BIZ Product.cs:4257, 4620: strErrorCodeDefault = TError.ErridQContract.Mst_Product_Get
            return PortResult.Fail("Mst_Product_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Tìm kiếm tự động sản phẩm theo mã người dùng hoặc tên sản phẩm (tối đa 10 bản ghi hoạt động).
    /// Gate: Mst_ProductController.cs:444-513 Search_Product_Auto(string productcodename)
    ///       Mst_ProductController.cs:5250-5312 strWhereClause_Mst_ProductProductUserCodeOrProductName(...)
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    /// </summary>
    [HttpPost("search_product_auto")]
    public async Task<IActionResult> Search_Product_Auto([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_Search_Product_AutoReq? req)
    {
        try
        {
            var listMst_ProductAutoSearch = new List<Mst_Product>();

            // 1. Chuẩn hoá từ khoá tìm kiếm (Gate Mst_ProductController.cs:444, 452)
            var kw = StdStr(req?.productcodename ?? req?.KeyWord ?? req?.ProductCodeUser ?? req?.ProductName);

            if (!string.IsNullOrEmpty(kw))
            {
                // 2. Dựng truy vấn Mst_Product (BIZ Product.cs:4352, 4420)
                var query = _db.Set<Mst_Product>().AsNoTracking();

                // 3. Lọc theo OrgID (Gate Mst_ProductController.cs:459, 5278-5281; BIZ Product.cs:4353)
                var orgId = StdParam(req?.OrgID);
                if (!string.IsNullOrEmpty(orgId))
                {
                    query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
                }

                // 4. Lọc theo NetworkID (Gate Mst_ProductController.cs:458, 5273-5276)
                var networkId = StdParam(req?.NetworkID);
                if (!string.IsNullOrEmpty(networkId))
                {
                    query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
                }

                // 5. Lọc theo FlagActive = "1" (Gate Mst_ProductController.cs:460: FlagActive = Client_Flag.Active; 5282-5285)
                var flagActive = !string.IsNullOrEmpty(req?.FlagActive) ? StdFlag(req.FlagActive) : "1";
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));

                // 6. Lọc theo ProductCodeUser hoặc ProductName (Gate Mst_ProductController.cs:5289-5301)
                var kwPattern = PortQuery.Contains(kw);
                query = query.Where(x => EF.Functions.Like((x.ProductCodeUser ?? "").ToLower(), kwPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.ProductName ?? "").ToLower(), kwPattern, PortQuery.Esc));

                // 7. Sắp xếp theo mp.OrgID, mp.ProductCode asc (BIZ Product.cs:4360-4362)
                var ordered = query.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode);

                // 8. Lấy tối đa 10 bản ghi (Gate Mst_ProductController.cs:476: Ft_RecordCount = "10")
                var recordStart = req?.Ft_RecordStart ?? 0;
                var recordCount = req?.Ft_RecordCount ?? 10;
                if (recordCount > 0 && recordStart >= 0)
                {
                    listMst_ProductAutoSearch = await ordered
                        .Skip((int)Math.Min(recordStart, int.MaxValue))
                        .Take((int)Math.Min(recordCount, int.MaxValue))
                        .ToListAsync();
                }
            }

            // 9. Trả về kết quả JSON theo Gate Mst_ProductController.cs:501
            return Ok(new
            {
                Success = true,
                ListProduct = listMst_ProductAutoSearch,
                Data = listMst_ProductAutoSearch
            });
        }
        catch (Exception ex)
        {
            // BIZ Product.cs:4257, 4620: strErrorCodeDefault = TError.ErridQContract.Mst_Product_Get
            return PortResult.Fail("Mst_Product_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Tạo mới hàng hóa / dịch vụ hoặc khởi tạo dữ liệu màn hình tạo mới.
    /// Gate: Mst_ProductController.cs:518-794 (GET Create View), 796-1088 [HttpPost] Create(string model)
    /// API:  MstProductController.cs:89-158 WA_Mst_Product_Save
    /// BIZ:  Product.cs:4805-5550 Mst_Product_Save -> Mst_Product_SaveX
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_CreateReq? req)
    {
        // 1. Kiểm tra body rỗng (LESSON A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_Product_Create", "Check.strJson", null);
        }

        // 2. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_ProductController.cs:799, 805)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_Product_CreateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.Lst_Mst_ProductUI_Create ??= modelObj.Lst_Mst_ProductUI_Create;
                    req.Lst_Mst_Product ??= modelObj.Lst_Mst_Product;
                    req.OrgID ??= modelObj.OrgID;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.ProductName ??= modelObj.ProductName;
                    req.ProductCodeUser ??= modelObj.ProductCodeUser;
                    req.ProductCode ??= modelObj.ProductCode;
                    req.ProductLevelSys ??= modelObj.ProductLevelSys;
                    req.BrandCode ??= modelObj.BrandCode;
                    req.ProductType ??= modelObj.ProductType;
                    req.ProductGrpCode ??= modelObj.ProductGrpCode;
                    req.UnitCode ??= modelObj.UnitCode;
                    req.FlagBuy ??= modelObj.FlagBuy;
                    req.FlagSell ??= modelObj.FlagSell;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.UPBuy ??= modelObj.UPBuy;
                    req.UPSell ??= modelObj.UPSell;
                    req.FlagRedirect ??= modelObj.FlagRedirect;
                    req.IsInit ??= modelObj.IsInit;
                    req.Init ??= modelObj.Init;
                }
            }
            catch
            {
                // Bỏ qua lỗi deserialize chuỗi JSON model
            }
        }

        // 3. Nếu là yêu cầu khởi tạo dữ liệu màn hình (Gate Mst_ProductController.cs:518-794)
        if (req.IsInit == true || req.Init == "1")
        {
            // GAP: Các bảng Mst_ProductType, Mst_ProductGroup, Mst_Brand, Prd_DynamicField, Mst_Attribute, Mst_VATRate, Product_CustomField
            // chưa có entity trong repo đích (Rule A.18). Gate gọi các API ClientService ra ngoài nên trả về danh sách trống rỗng chuẩn hoá.
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ProductCode = "",
                    NetworkID = req.NetworkID ?? "1001",
                    OrgID = req.OrgID ?? "1001",
                    List_Mst_ProductType = new List<object>(),
                    List_Mst_ProductGroup = new List<object>(),
                    List_Mst_ProductGroupUI = new List<object>(),
                    List_Mst_Brand = new List<object>(),
                    List_Prd_DynamicField = new List<object>(),
                    List_Mst_Attribute = new List<object>(),
                    List_Mst_VATRate = new List<object>(),
                    ListProduct_CustomField = new List<object>()
                }
            });
        }

        // 4. Thu thập danh sách sản phẩm cần tạo
        var items = new List<Mst_Product_CreateItem>();
        if (req.Lst_Mst_ProductUI_Create != null && req.Lst_Mst_ProductUI_Create.Count > 0)
        {
            items.AddRange(req.Lst_Mst_ProductUI_Create);
        }
        else if (req.Lst_Mst_Product != null && req.Lst_Mst_Product.Count > 0)
        {
            items.AddRange(req.Lst_Mst_Product);
        }
        else if (!string.IsNullOrWhiteSpace(req.ProductName) || !string.IsNullOrWhiteSpace(req.ProductCodeUser) || !string.IsNullOrWhiteSpace(req.ProductCode))
        {
            items.Add(new Mst_Product_CreateItem
            {
                OrgID = req.OrgID,
                NetworkID = req.NetworkID,
                ProductCode = req.ProductCode,
                ProductLevelSys = req.ProductLevelSys,
                ProductCodeUser = req.ProductCodeUser,
                ProductName = req.ProductName,
                ProductNameEN = req.ProductNameEN,
                BrandCode = req.BrandCode,
                ProductType = req.ProductType,
                ProductGrpCode = req.ProductGrpCode,
                UnitCode = req.UnitCode,
                ValConvert = req.ValConvert,
                FlagBuy = req.FlagBuy,
                FlagSell = req.FlagSell,
                FlagActive = req.FlagActive,
                UPBuy = req.UPBuy,
                UPSell = req.UPSell,
                QtyMaxSt = req.QtyMaxSt,
                QtyMinSt = req.QtyMinSt,
                QtyEffSt = req.QtyEffSt,
                VATRateCode = req.VATRateCode,
                Remark = req.Remark,
                CustomField1 = req.CustomField1,
                CustomField2 = req.CustomField2,
                CustomField3 = req.CustomField3,
                CustomField4 = req.CustomField4,
                CustomField5 = req.CustomField5,
                FlagSerial = req.FlagSerial,
                FlagLot = req.FlagLot,
                FlagFG = req.FlagFG,
                ProductStd = req.ProductStd,
                ProductExpiry = req.ProductExpiry,
                ProductQuyCach = req.ProductQuyCach,
                ProductOrigin = req.ProductOrigin,
                Lst_Prd_Attribute = req.Lst_Prd_Attribute
            });
        }

        // Guard 1: Danh sách sản phẩm rỗng (BIZ Product.cs:5008-5018)
        if (items.Count == 0)
        {
            return PortResult.Fail("Mst_Product_Create_Input_Mst_ProductTblNotFound", "Check.TableName", "Mst_Product");
        }

        var defaultOrgId = StdParam(req.OrgID);
        var defaultNetworkId = StdParam(req.NetworkID) ?? defaultOrgId ?? "1001";

        // Guard 2: Kiểm tra OrgID và ProductName cho từng item
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var itemOrgId = StdParam(item.OrgID) ?? defaultOrgId;
            if (string.IsNullOrEmpty(itemOrgId))
            {
                return PortResult.Fail("Mst_Product_Create_InvalidOrgID", "Check.OrgID", null);
            }

            if (string.IsNullOrWhiteSpace(item.ProductName))
            {
                return PortResult.Fail("Mst_Product_Create_InvalidProductName", "Check.ProductName", null);
            }
        }

        // 5. Mở Transaction (BIZ Product.cs:4842 _cf.db.BeginTransaction())
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var listSavedProducts = new List<Mst_Product>();
            string? productCodeRoot = null;
            string? productCodeBase = null;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var itemOrgId = StdParam(item.OrgID) ?? defaultOrgId ?? "1001";
                var itemNetworkId = StdParam(item.NetworkID) ?? defaultNetworkId;

                // Sinh mã sản phẩm nếu chưa có (Gate: Mst_ProductController.cs:812-843 WA_Seq_GenObjCode_Get)
                var code = StdParam(item.ProductCode);
                if (string.IsNullOrEmpty(code))
                {
                    code = $"PRD_{DateTime.UtcNow:yyyyMMddHHmmss}_{i + 1}";
                }

                if (i == 0)
                {
                    productCodeRoot = code;
                }

                var levelSys = StdParam(item.ProductLevelSys) ?? "ROOTPRD";
                if (levelSys == "ROOTPRD" || levelSys == "BASEPRD")
                {
                    productCodeBase = code;
                }

                var prdCodeUser = StdParam(item.ProductCodeUser) ?? code;

                // Xoá bản ghi cũ nếu trùng OrgID và ProductCode (BIZ Product.cs:5278-5324)
                var existing = await _db.Set<Mst_Product>()
                    .FirstOrDefaultAsync(x => x.OrgID == itemOrgId && x.ProductCode == code);
                if (existing != null)
                {
                    _db.Set<Mst_Product>().Remove(existing);
                    await _db.SaveChangesAsync();
                }

                var newProduct = new Mst_Product
                {
                    OrgID = itemOrgId,
                    ProductCode = code,
                    NetworkID = itemNetworkId,
                    ProductLevelSys = levelSys,
                    ProductCodeUser = prdCodeUser,
                    BrandCode = StdParam(item.BrandCode),
                    ProductType = StdParam(item.ProductType) ?? "PRODUCT",
                    ProductGrpCode = StdParam(item.ProductGrpCode),
                    ProductName = StdStr(item.ProductName),
                    ProductNameEN = StdStr(item.ProductNameEN),
                    ProductBarCode = StdParam(item.ProductBarCode),
                    ProductCodeNetwork = null,
                    ProductCodeBase = productCodeBase ?? code,
                    ProductCodeRoot = productCodeRoot ?? code,
                    ProductImagePathList = null,
                    ProductFilePathList = null,
                    UnitCode = StdParam(item.UnitCode) ?? "CAI",
                    ValConvert = item.ValConvert,
                    FlagSell = !string.IsNullOrEmpty(item.FlagSell) ? StdFlag(item.FlagSell) : "1",
                    FlagBuy = !string.IsNullOrEmpty(item.FlagBuy) ? StdFlag(item.FlagBuy) : "1",
                    UPBuy = item.UPBuy,
                    UPSell = item.UPSell,
                    QtyMaxSt = item.QtyMaxSt,
                    QtyMinSt = item.QtyMinSt,
                    QtyEffSt = item.QtyEffSt,
                    ListOfPrdDynamicFieldValue = item.ListOfPrdDynamicFieldValue,
                    Remark = StdStr(item.Remark),
                    VATRateCode = StdParam(item.VATRateCode),
                    CustomField1 = StdStr(item.CustomField1),
                    CustomField2 = StdStr(item.CustomField2),
                    CustomField3 = StdStr(item.CustomField3),
                    CustomField4 = StdStr(item.CustomField4),
                    CustomField5 = StdStr(item.CustomField5),
                    FlagFG = StdFlag(item.FlagFG),
                    FlagSerial = StdFlag(item.FlagSerial),
                    FlagLot = StdFlag(item.FlagLot),
                    ProductStd = StdStr(item.ProductStd),
                    ProductExpiry = StdStr(item.ProductExpiry),
                    ProductQuyCach = StdStr(item.ProductQuyCach),
                    ProductMnfUrl = StdStr(item.ProductMnfUrl),
                    ProductIntro = StdStr(item.ProductIntro),
                    ProductUserGuide = StdStr(item.ProductUserGuide),
                    ProductDrawing = StdStr(item.ProductDrawing),
                    ProductOrigin = StdStr(item.ProductOrigin),
                    CreateDTimeUTC = dtimeSys,
                    CreateBy = "port",
                    LUDTimeUTC = dtimeSys,
                    LUBy = "port",
                    FlagActive = !string.IsNullOrEmpty(item.FlagActive) ? StdFlag(item.FlagActive) : "1",
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = "port",
                    CodeGuid = Guid.NewGuid().ToString()
                };

                _db.Set<Mst_Product>().Add(newProduct);
                listSavedProducts.Add(newProduct);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // GAP: Lưu Prd_Attribute, Prd_BOM, Mst_ProductImages, Mst_ProductFiles chưa có entity trong repo đích (Rule A.18)
            // GAP: Gọi Mst_ProductService.Instance.WA_Mst_Product_Create sang hệ thống ngoài Product Center bỏ qua theo Rule P3

            // Trả về kết quả thành công khớp Gate Mst_ProductController.cs:1074-1080
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Hàng hóa đã được tạo thành công!" },
                RedirectUrl = req.FlagRedirect == "1" ? "/api/mst_product/create" : "/api/mst_product/index",
                Data = listSavedProducts
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Product_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Lấy danh sách sản phẩm theo mã người dùng hoặc tên sản phẩm (Gate đặt tên GetBOM nhưng trả về danh sách Mst_Product).
    /// Gate: Mst_ProductController.cs:1100-1152 GetBOM(string prdid = "")
    ///       Mst_ProductController.cs:5020-5036 strWhereGetProductId(Mst_Product data)
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    /// </summary>
    [HttpPost("getbom")]
    public async Task<IActionResult> GetBOM([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_GetBOMReq? req)
    {
        try
        {
            // 1. Dựng truy vấn cơ sở từ bảng Mst_Product (Product.cs:4352, 4420)
            var query = _db.Set<Mst_Product>().AsNoTracking();

            // 2. Lọc theo OrgID nếu có (Gate Mst_ProductController.cs:1121; BIZ Product.cs:4353)
            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            // 3. Lọc theo NetworkID nếu có (Gate Mst_ProductController.cs:1120)
            var networkId = StdParam(req?.NetworkID);
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            // 4. Lọc theo prdid (Gate Mst_ProductController.cs:1104-1110, 5020-5036 strWhereGetProductId):
            // Nguồn gán ProductCodeUser = prdid và ProductName = prdid; mệnh đề WHERE là (ProductCodeUser = prdid or ProductName = prdid)
            var prdid = StdStr(req?.prdid ?? req?.ProductCodeUser ?? req?.ProductName ?? req?.ProductCode);
            if (!string.IsNullOrEmpty(prdid))
            {
                var prdUserPattern = PortQuery.Equal(req?.ProductCodeUser ?? prdid);
                var prdNamePattern = PortQuery.Equal(req?.ProductName ?? prdid);
                query = query.Where(x => EF.Functions.Like((x.ProductCodeUser ?? "").ToLower(), prdUserPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.ProductName ?? "").ToLower(), prdNamePattern, PortQuery.Esc));
            }

            // 5. Sắp xếp theo mp.OrgID, mp.ProductCode asc (BIZ Product.cs:4360-4362)
            var ordered = query.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode);

            // 6. Phân trang dữ liệu nếu có truyền Ft_RecordStart / Ft_RecordCount (BIZ Product.cs:4319-4320, 4375-4376)
            var listProduct = new List<Mst_Product>();
            if (req?.Ft_RecordCount != null && req?.Ft_RecordStart != null)
            {
                var recordStart = req.Ft_RecordStart.Value;
                var recordCount = req.Ft_RecordCount.Value;
                if (recordCount > 0 && recordStart >= 0)
                {
                    listProduct = await ordered
                        .Skip((int)Math.Min(recordStart, int.MaxValue))
                        .Take((int)Math.Min(recordCount, int.MaxValue))
                        .ToListAsync();
                }
            }
            else
            {
                listProduct = await ordered.ToListAsync();
            }

            // LỖI NGUỒN GIỮ NGUYÊN (Mst_ProductController.cs:1140-1151):
            // Action tên GetBOM nhưng mã nguồn tầng gate gọi WA_Prd_BOM_Get lưu vào biến tạm listRT_Mst_Product.Lst_Prd_BOM rồi không sử dụng
            // (đoạn foreach gán BOM bị comment out ở dòng 1144-1149), và return Json(new { Data = objRT_Mst_Product }) trả về danh sách Mst_Product.
            // GAP: Bảng Prd_BOM chưa có entity trong repo đích (Rule A.18).
            return Ok(new
            {
                Success = true,
                Data = listProduct
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

    // Gate Mst_ProductController.cs:415-418 và ModelsUI/Mst_ProductUI.cs:37-41
    public List<Mst_Product>? LstPrdBase { get; set; }
    public List<Mst_Product>? LstPrdLeve2 { get; set; }
    public List<Prd_Attribute>? LstAttributeBase { get; set; }
    public List<Prd_Attribute>? LstAttributeLevel2 { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action LoadData của Mst_ProductController.
/// Gate: Mst_ProductController.cs:395 LoadData(string prcode, string prbscode, string prlv, string prroot, string idx)
/// </summary>
public class Mst_Product_LoadDataReq
{
    // Gate Mst_ProductController.cs:395
    public string? prcode { get; set; }
    public string? ProductCode { get; set; }
    public string? prbscode { get; set; }
    public string? ProductCodeBase { get; set; }
    public string? prlv { get; set; }
    public string? ProductLevelSys { get; set; }
    public string? prroot { get; set; }
    public string? ProductCodeRoot { get; set; }
    public string? idx { get; set; }

    // Các tham số lọc bổ sung (LESSON A.14)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}

/// <summary>
/// DTO thuộc tính sản phẩm theo nguồn D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Prd_Attribute.cs.
/// </summary>
public class Prd_Attribute
{
    public string? OrgID { get; set; }
    public string? ProductCode { get; set; }
    public string? AttributeCode { get; set; }
    public string? NetworkID { get; set; }
    public string? AttributeValue { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action Search_Product_Auto của Mst_ProductController.
/// Gate: Mst_ProductController.cs:444 Search_Product_Auto(string productcodename)
/// </summary>
public class Mst_Product_Search_Product_AutoReq
{
    // Gate Mst_ProductController.cs:444
    public string? productcodename { get; set; }
    public string? KeyWord { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ProductName { get; set; }

    // Các tham số lọc bổ sung (LESSON A.14)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action Create của Mst_ProductController.
/// Gate: Mst_ProductController.cs:518-794, 796-1088 [HttpPost] Create(string model)
/// </summary>
public class Mst_Product_CreateReq
{
    // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_ProductController.cs:799, 805)
    public string? Model { get; set; }

    // Hỗ trợ truyền mảng Lst_Mst_ProductUI_Create theo RQ_Mst_ProductUI (Gate Mst_ProductController.cs:806)
    public List<Mst_Product_CreateItem>? Lst_Mst_ProductUI_Create { get; set; }

    // Hỗ trợ truyền mảng Lst_Mst_Product theo RQ_Mst_Product (Biz Product.cs:5008)
    public List<Mst_Product_CreateItem>? Lst_Mst_Product { get; set; }

    // Hỗ trợ truyền phẳng một sản phẩm duy nhất
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductLevelSys { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ProductName { get; set; }
    public string? ProductNameEN { get; set; }
    public string? BrandCode { get; set; }
    public string? ProductType { get; set; }
    public string? ProductGrpCode { get; set; }
    public string? UnitCode { get; set; }
    public double? ValConvert { get; set; }
    public string? FlagBuy { get; set; }
    public string? FlagSell { get; set; }
    public string? FlagActive { get; set; }
    public double? UPBuy { get; set; }
    public double? UPSell { get; set; }
    public double? QtyMaxSt { get; set; }
    public double? QtyMinSt { get; set; }
    public double? QtyEffSt { get; set; }
    public string? VATRateCode { get; set; }
    public string? Remark { get; set; }
    public string? CustomField1 { get; set; }
    public string? CustomField2 { get; set; }
    public string? CustomField3 { get; set; }
    public string? CustomField4 { get; set; }
    public string? CustomField5 { get; set; }
    public string? FlagSerial { get; set; }
    public string? FlagLot { get; set; }
    public string? FlagFG { get; set; }
    public string? ProductStd { get; set; }
    public string? ProductExpiry { get; set; }
    public string? ProductQuyCach { get; set; }
    public string? ProductOrigin { get; set; }

    // Thuộc tính bổ sung
    public List<Prd_Attribute>? Lst_Prd_Attribute { get; set; }
    public List<Prd_BOM_Item>? Lst_Prd_BOM { get; set; }
    public List<Mst_ProductImages_Item>? Lst_Mst_ProductImages { get; set; }
    public List<Mst_ProductFiles_Item>? Lst_Mst_ProductFiles { get; set; }

    // Flag chuyển trang sau khi tạo thành công (Gate Mst_ProductController.cs:1076)
    public string? FlagRedirect { get; set; }

    // Cờ yêu cầu khởi tạo dữ liệu màn hình (Gate Mst_ProductController.cs:518-794)
    public bool? IsInit { get; set; }
    public string? Init { get; set; }
}

/// <summary>
/// DTO thông tin sản phẩm tạo mới (Mst_ProductController.cs:833-893, RQ_Mst_ProductUI.cs).
/// </summary>
public class Mst_Product_CreateItem
{
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductLevelSys { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ProductName { get; set; }
    public string? ProductNameEN { get; set; }
    public string? ProductBarCode { get; set; }
    public string? ProductCodeBase { get; set; }
    public string? ProductCodeRoot { get; set; }
    public string? BrandCode { get; set; }
    public string? ProductType { get; set; }
    public string? ProductGrpCode { get; set; }
    public string? UnitCode { get; set; }
    public double? ValConvert { get; set; }
    public string? FlagBuy { get; set; }
    public string? FlagSell { get; set; }
    public string? FlagActive { get; set; }
    public double? UPBuy { get; set; }
    public double? UPSell { get; set; }
    public double? QtyMaxSt { get; set; }
    public double? QtyMinSt { get; set; }
    public double? QtyEffSt { get; set; }
    public string? VATRateCode { get; set; }
    public string? Remark { get; set; }
    public string? ListOfPrdDynamicFieldValue { get; set; }
    public string? CustomField1 { get; set; }
    public string? CustomField2 { get; set; }
    public string? CustomField3 { get; set; }
    public string? CustomField4 { get; set; }
    public string? CustomField5 { get; set; }
    public string? FlagSerial { get; set; }
    public string? FlagLot { get; set; }
    public string? FlagFG { get; set; }
    public string? ProductStd { get; set; }
    public string? ProductExpiry { get; set; }
    public string? ProductQuyCach { get; set; }
    public string? ProductMnfUrl { get; set; }
    public string? ProductIntro { get; set; }
    public string? ProductUserGuide { get; set; }
    public string? ProductDrawing { get; set; }
    public string? ProductOrigin { get; set; }
    public string? DTimeUsed { get; set; }
    public string? CodeGuid { get; set; }

    public List<Prd_Attribute>? Lst_Prd_Attribute { get; set; }
}

public class Prd_BOM_Item
{
    public string? OrgID { get; set; }
    public string? ProductCode { get; set; }
    public string? OrgIDParent { get; set; }
    public string? ProductCodeParent { get; set; }
    public double? Qty { get; set; }
    public double? mp_UPBuy { get; set; }
    public double? mp_UPSell { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
}

public class Mst_ProductImages_Item
{
    public int? Idx { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductImagePath { get; set; }
    public string? ProductImageName { get; set; }
    public string? ProductImageSpec { get; set; }
    public string? FlagPrimaryImage { get; set; }
    public string? FlagActive { get; set; }
}

public class Mst_ProductFiles_Item
{
    public int? Idx { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductFilePath { get; set; }
    public string? ProductFileName { get; set; }
    public string? ProductFileDesc { get; set; }
    public string? FlagActive { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action GetBOM của Mst_ProductController.
/// Gate: Mst_ProductController.cs:1100 GetBOM(string prdid = "")
/// </summary>
public class Mst_Product_GetBOMReq
{
    // Gate Mst_ProductController.cs:1100
    public string? prdid { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }

    // Các tham số lọc bổ sung (LESSON A.14)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }
}
