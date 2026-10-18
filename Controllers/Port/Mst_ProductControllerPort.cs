using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ProductController.cs — Mst_Product
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ProductController.cs:29-200 (Index), 241-391 (DoSearch), 395-440 (LoadData), 444-513 (Search_Product_Auto), 518-794 (Create GET), 796-1088 (Create POST), 1100-1152 (GetBOM), 1154-1208 (SearchProduct), 1210-1319 (GetSearch), 1322-1415 (ProductIngredient), 1418-1901 (Update), 1906-2721 (Edit), 2880-2943 (DeleteUnit), 2950-2960 (ExportTemplate), 4818-4859 (GetImportDicColumsTemplate), 202-237 (GetPrdBase), 4964-5004 (strWhereClause_Mst_Product), 5020-5036 (strWhereGetProductId), 5037-5053 (strWhereSearchProductProductId), 5076-5187 (strWhereClause_Mst_Product_Search), 5188-5205 (strWhereClause_Mst_Product_Get_Base), 5206-5223 (strWhereClause_Mst_Product_Get_Level2), 5224-5249 (strWhereClause_Mst_Product_Update), 5250-5312 (strWhereClause_Mst_ProductProductUserCodeOrProductName)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstProductController.cs:19-87 (WA_Mst_Product_Get), 89-158 (WA_Mst_Product_Save), MstProductGroupController.cs:19-89 (WA_Mst_ProductGroup_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Product\Product.cs:4230-4643 (Mst_Product_Get), 4644-4802 (WAS_Mst_Product_Get), 4805-5550 (Mst_Product_Save, WAS_Mst_Product_Save, Mst_Product_SaveX), 30-146 (WAS_Mst_ProductGroup_Get), 147-293 (Mst_ProductGroup_Get), 295-459 (Mst_ProductGroup_GetX)
[ApiController]
[Route("api/mst_product")]
public class Mst_ProductControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;
    private readonly IWebHostEnvironment? _env;

    private static readonly Dictionary<string, string> ExportTemplateDicColumns = new()
    {
        { "ProductCodeUser", "Mã hàng(*)" },
        { "ProductName", "Tên hàng(*)" },
        { "ProductBarCode", "Mã vạch" },
        { "ProductCodeBase", "Mã hàng liên quan" },
        { "ProductCodeRoot", "Mã hàng gốc" },
        { "BrandCode", "Mã nhãn hiệu" },
        { "ProductGrpCode", "Nhóm hàng (*)" },
        { "ProductType", "Loại hàng" },
        { "ProductImagePathList", "Ảnh sản phẩm" },
        { "ProductFilePathList", "File đính kèm" },
        { "FlagSerial", "QL Serial" },
        { "FlagLot", "QL LOT" },
        { "UnitCode", "Đơn vị" },
        { "ValConvert", "Quy đổi" },
        { "FlagSell", "Được bán" },
        { "FlagBuy", "Được mua" },
        { "UPBuy", "Giá mua" },
        { "UPSell", "Giá bán" },
        { "QtyMinSt", "Tồn nhỏ nhất" },
        { "QtyEffSt", "Tồn tối ưu" },
        { "QtyMaxSt", "Tồn lớn nhất" },
        { "ListAttribute", "Thuộc tính" },
        { "ListBOM", "Hàng thành phần" },
        { "ListOfPrdDynamicFieldValue", "List các trường động" },
        { "ProductStd", "Tiêu chuẩn" },
        { "ProductExpiry", "Hạn sử dụng" },
        { "ProductQuyCach", "Quy cách" },
        { "ProductMnfUrl", "Link quy trình sx" },
        { "ProductIntro", "Chi tiết sản phẩm" },
        { "ProductUserGuide", "Hướng dẫn sử dụng" },
        { "ProductDrawing", "Bản vẽ" },
        { "ProductOrigin", "Xuất xứ" },
        { "Remark", "Ghi chú" }
    };

    public Mst_ProductControllerPort(AppDbContext db, IConfiguration? config = null, IWebHostEnvironment? env = null)
    {
        _db = db;
        _config = config;
        _env = env;
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

    /// <summary>
    /// Tìm kiếm sản phẩm theo mã người dùng hoặc tên sản phẩm (so khớp like %prdid%).
    /// Gate: Mst_ProductController.cs:1154-1208 SearchProduct(string prdid = "")
    ///       Mst_ProductController.cs:5037-5053 strWhereSearchProductProductId(Mst_Product data)
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    /// </summary>
    [HttpPost("searchproduct")]
    public async Task<IActionResult> SearchProduct([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_SearchProductReq? req)
    {
        try
        {
            // 1. Dựng truy vấn cơ sở từ bảng Mst_Product (Product.cs:4352, 4420)
            var query = _db.Set<Mst_Product>().AsNoTracking();

            // 2. Lọc theo OrgID nếu có (Gate Mst_ProductController.cs:1177; BIZ Product.cs:4238, 4353)
            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            // 3. Lọc theo NetworkID nếu có (Gate Mst_ProductController.cs:1176)
            var networkId = StdParam(req?.NetworkID);
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            // 4. Lọc theo prdid (Gate Mst_ProductController.cs:1160-1166, 5037-5053 strWhereSearchProductProductId):
            // Nguồn gán ProductCodeUser = prdid và ProductName = prdid, mệnh đề WHERE là:
            // Mst_Product.ProductCodeUser like '%prdid%' or Mst_Product.ProductName like '%prdid%'
            var prdid = StdStr(req?.prdid ?? req?.ProductCodeUser ?? req?.ProductName ?? req?.ProductCode);
            if (!string.IsNullOrEmpty(prdid))
            {
                var kwPattern = PortQuery.Contains(prdid);
                query = query.Where(x => EF.Functions.Like((x.ProductCodeUser ?? "").ToLower(), kwPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.ProductName ?? "").ToLower(), kwPattern, PortQuery.Esc));
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

            // Trả về kết quả JSON chuẩn hoá { Success = true, Data = listProduct }
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

    /// <summary>
    /// Tìm kiếm hàng hoá/dịch vụ trả về Partial View SearchProduct (JsonView).
    /// Gate: Mst_ProductController.cs:1210-1319 GetSearch(...)
    ///       Mst_ProductController.cs:5076-5187 strWhereClause_Mst_Product_Search(...)
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    /// </summary>
    [HttpPost("getsearch")]
    public async Task<IActionResult> GetSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_GetSearchReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_ProductController.cs:1218-1219, 1228-1229, 1282-1283
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            // 2. Dựng truy vấn cơ sở từ bảng Mst_Product (Product.cs:4352, 4420)
            var query = _db.Set<Mst_Product>().AsNoTracking();

            // 3. Lọc theo OrgID (Gate Mst_ProductController.cs:1279; BIZ Product.cs:4353)
            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            // 4. Lọc theo NetworkID (Gate Mst_ProductController.cs:1278)
            var networkId = StdParam(req?.NetworkID);
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            // 5. Lọc theo checkbox ProductType: CkbProduct, CkbServices, CkbCombo (Gate Mst_ProductController.cs:1240-1251, 5175-5185)
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

            // 6. Lọc theo FlagActive (Gate Mst_ProductController.cs:1263-1266, 5104-5107):
            // Nếu cả 3 checkbox đều rỗng thì gate gán objMst_Product.FlagActive = "1" (dòng 1265)
            if (!string.IsNullOrEmpty(req?.FlagActive))
            {
                var flag = StdFlag(req.FlagActive);
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flag), PortQuery.Esc));
            }
            else if (selectedTypes.Count == 0)
            {
                // Mặc định FlagActive = "1" khi không chọn checkbox nào (Gate Mst_ProductController.cs:1263-1266)
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));
            }

            // 7. Lọc theo ProductGrpCode (Gate Mst_ProductController.cs:1259, 5088-5091)
            var grpCode = StdParam(req?.ProductGrpCode);
            if (!string.IsNullOrEmpty(grpCode))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductGrpCode ?? "").ToLower(), PortQuery.Equal(grpCode), PortQuery.Esc));
            }

            // 8. Lọc theo ProductCodeUserName: khớp ProductCodeUser (=) HOẶC ProductName (like '%...%')
            // Gate Mst_ProductController.cs:1257-1258, 5152-5173
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

            // 9. Lọc theo ProductCode nếu có (Gate Mst_ProductController.cs:5083-5086)
            var prdCode = StdParam(req?.ProductCode);
            if (!string.IsNullOrEmpty(prdCode))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductCode ?? "").ToLower(), PortQuery.Equal(prdCode), PortQuery.Esc));
            }

            // 10. Lọc theo ProductLevelSys nếu có (Gate Mst_ProductController.cs:5092-5095; lưu ý: gate GetSearch comment out ROOTPRD)
            var levelSys = StdParam(req?.ProductLevelSys);
            if (!string.IsNullOrEmpty(levelSys))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal(levelSys), PortQuery.Esc));
            }

            // 11. Lọc theo BrandCode nếu có (Gate Mst_ProductController.cs:5096-5099)
            var brandCode = StdParam(req?.BrandCode);
            if (!string.IsNullOrEmpty(brandCode))
            {
                query = query.Where(x => EF.Functions.Like((x.BrandCode ?? "").ToLower(), PortQuery.Equal(brandCode), PortQuery.Esc));
            }

            // 12. Lọc theo UnitCode nếu có (Gate Mst_ProductController.cs:5100-5103)
            var unitCode = StdParam(req?.UnitCode);
            if (!string.IsNullOrEmpty(unitCode))
            {
                query = query.Where(x => EF.Functions.Like((x.UnitCode ?? "").ToLower(), PortQuery.Equal(unitCode), PortQuery.Esc));
            }

            // GAP: Lọc theo Prd_Attribute (Gate Mst_ProductController.cs:5110-5136):
            // Bảng Prd_Attribute chưa có entity trong repo đích (Rule A.18).

            // 13. Đếm tổng số bản ghi thoả mãn (BIZ Product.cs:4366: select Count(0) MyCount from ...)
            var itemCount = await query.CountAsync();

            // 14. Sắp xếp theo mp.OrgID, mp.ProductCode asc (BIZ Product.cs:4360-4362)
            var ordered = query.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode);

            // 15. Phân trang dữ liệu (LESSON A.7, A.12 chống tràn int và xử lý chỉ số âm/kích thước 0)
            var pagedProducts = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Product>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordCount, int.MaxValue))
                               .ToListAsync();

            // 16. Tính số trang (Gate Mst_ProductController.cs:1232; PageInfo)
            var pageCount = 0;
            if (pagedProducts.Count > 0)
            {
                var ps = pageSize <= 0 ? 10 : pageSize;
                pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
            }

            // 17. Đóng gói kết quả theo RT_Mst_Product (Gate Mst_ProductController.cs:1301-1304; View SearchProduct.cshtml)
            var rtProduct = new RT_Mst_Product
            {
                Lst_Mst_Product = pagedProducts
            };

            var pageInfo = new PageInfo<Mst_Product>(pagedProducts, itemCount, pageCount, pageIndex, pageSize);

            // 18. Trả về kết quả JSON chuẩn hoá (Gate Mst_ProductController.cs:1304 JsonView("SearchProduct", objRT_Mst_Product))
            return Ok(new
            {
                Success = true,
                Data = rtProduct,
                Lst_Mst_Product = pagedProducts,
                PageInfo = pageInfo,
                ItemCount = itemCount,
                PageCount = pageCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            // BIZ Product.cs:4257, 4620: strErrorCodeDefault = TError.ErridQContract.Mst_Product_Get
            return PortResult.Fail("Mst_Product_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Hiển thị thông tin nguyên liệu sản phẩm / thành phần sản phẩm (PartialView ProductIngredient).
    /// Gate: Mst_ProductController.cs:1322-1415 ProductIngredient(...)
    ///       Mst_ProductController.cs:4964-5004 strWhereClause_Mst_Product(...)
    ///       BaseController.cs:4332-4366 List_Mst_ProductGroup_Async
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    ///       MstProductGroupController.cs:19-89 WA_Mst_ProductGroup_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    ///       Product.cs:30-146 WAS_Mst_ProductGroup_Get, 147-293 Mst_ProductGroup_Get, 295-459 Mst_ProductGroup_GetX
    /// </summary>
    [HttpPost("productingredient")]
    public async Task<IActionResult> ProductIngredient([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_ProductIngredientReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_ProductController.cs:1325-1326, 1334-1335:
            var pageSize = req?.PageCount ?? req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            var orgId = StdParam(req?.OrgID);
            var networkId = StdParam(req?.NetworkID);

            // 2. Lấy danh sách nhóm sản phẩm List_Mst_ProductGroup_Async (Gate Mst_ProductController.cs:1337-1359; Biz Product.cs:295-459)
            var groupQuery = _db.Set<Mst_ProductGroup>().AsNoTracking();
            if (!string.IsNullOrEmpty(orgId))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            var listMstProductGroup = await groupQuery.OrderBy(x => x.ProductGrpCode).ToListAsync();

            // 3. Dựng truy vấn cơ sở từ bảng Mst_Product (Product.cs:4352, 4420)
            var query = _db.Set<Mst_Product>().AsNoTracking();

            // 4. Mặc định lọc theo ProductLevelSys = ROOTPRD như gate ProductIngredient thiết lập (Gate Mst_ProductController.cs:1363)
            query = query.Where(x => EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal("ROOTPRD"), PortQuery.Esc));

            // 5. Lọc theo OrgID nếu có (Gate Mst_ProductController.cs:1347, 1379; BIZ Product.cs:4353)
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            // 6. Lọc theo NetworkID nếu có (Gate Mst_ProductController.cs:1346, 1378)
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            // 7. Lọc theo ProductCodeUserName nếu có (Gate Mst_ProductController.cs:1364, 4970-4973 strWhereClause_Mst_Product)
            // LỖI NGUỒN GIỮ NGUYÊN (Mst_ProductController.cs:1361-1366):
            // Gate nhận modelproductGrpCode nhưng không gán vào objMst_Product nên strWhereClause_Mst_Product chỉ lọc theo ProductCodeUser.
            // Cờ FlagFG = "0" được gán ở gate nhưng strWhereClause_Mst_Product không sinh WHERE cho FlagFG.
            var prdCodeUser = StdParam(req?.ProductCodeUserName ?? req?.ProductCodeUser);
            if (!string.IsNullOrEmpty(prdCodeUser))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductCodeUser ?? "").ToLower(), PortQuery.Equal(prdCodeUser), PortQuery.Esc));
            }

            // 8. Đếm tổng số bản ghi thoả mãn (BIZ Product.cs:4366: select Count(0) MyCount from ...)
            var itemCount = await query.CountAsync();

            // 9. Sắp xếp theo mp.OrgID, mp.ProductCode asc (BIZ Product.cs:4360-4362)
            var ordered = query.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode);

            // 10. Phân trang dữ liệu (LESSON A.7, A.12 chống tràn int và xử lý chỉ số âm/kích thước 0)
            var pagedProducts = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Product>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordCount, int.MaxValue))
                               .ToListAsync();

            // 11. Tính số trang (Gate Mst_ProductController.cs:1335)
            var pageCount = 0;
            if (pagedProducts.Count > 0)
            {
                var ps = pageSize <= 0 ? 10 : pageSize;
                pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
            }

            // 12. Trả về kết quả JSON chuẩn hoá (Gate Mst_ProductController.cs:1333-1335, 1359, 1399-1400)
            return Ok(new
            {
                Success = true,
                Data = pagedProducts,
                List_Mst_Product = pagedProducts,
                ListMstProductGroup = listMstProductGroup,
                PageCur = pageIndex,
                PageCount = pageCount,
                ItemCount = itemCount,
                PageSize = pageSize,
                Init = "search"
            });
        }
        catch (Exception ex)
        {
            // BIZ Product.cs:4257, 4620: strErrorCodeDefault = TError.ErridQContract.Mst_Product_Get
            return PortResult.Fail("Mst_Product_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Lấy thông tin hàng hóa / dịch vụ để hiển thị màn hình cập nhật (View Update).
    /// Gate: Mst_ProductController.cs:1418-1901 Update(string prcode, string prbscode, string prroot, string prlv)
    ///       Mst_ProductController.cs:5224-5249 strWhereClause_Mst_Product_Update(...)
    ///       BaseController.cs:4618-4624 RT_Mst_Product_Get
    ///       BaseController.cs:4625-4630 List_Mst_Product_Async
    ///       BaseController.cs:4332-4337 List_Mst_ProductGroup_Async
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_UpdateReq? req)
    {
        try
        {
            // 1. Guard kiểm tra body rỗng (LESSON A.15, A.21)
            if (req == null)
            {
                return PortResult.Fail("Mst_Product_Get", "Check.strJson", null);
            }

            // 2. Chuẩn hoá tham số đầu vào (Gate Mst_ProductController.cs:1418)
            var prcode = StdParam(req.prcode ?? req.ProductCode);
            var prbscode = StdParam(req.prbscode ?? req.ProductCodeBase);
            var prroot = StdParam(req.prroot ?? req.ProductCodeRoot);
            var prlv = StdParam(req.prlv ?? req.ProductLevelSys);

            // Nguồn gate Mst_ProductController.cs:1433 gọi prlv.Equals(...) ném NullReferenceException nếu prlv rỗng/null
            if (string.IsNullOrEmpty(prlv))
            {
                return PortResult.Fail("Mst_Product_Get", "Check.prlv", null);
            }

            var orgId = StdParam(req.OrgID);
            var networkId = StdParam(req.NetworkID);

            var objRT_Mst_Product = new RT_Mst_Product();
            var objMst_Product_Root = new Mst_Product();
            var objMst_Product = new Mst_Product();
            var listUnitCode = new List<Mst_Product>();
            var unitCodeBase = "";

            // 3. Phân nhánh theo prlv (Gate Mst_ProductController.cs:1433-1489)
            if (string.Equals(prlv, "ROOTPRD", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(prlv, "BASEPRD", StringComparison.OrdinalIgnoreCase))
            {
                // Tìm kiếm sản phẩm root / base
                objMst_Product.ProductCodeRoot = prroot;
                objMst_Product.ProductCodeBase = prbscode;
            }
            else if (string.Equals(prlv, "L2PRD", StringComparison.OrdinalIgnoreCase))
            {
                // Tìm kiếm sản phẩm con
                objMst_Product.ProductCode = prcode;
                objMst_Product_Root.ProductCode = prroot;

                // Lấy thông tin sản phẩm root để lấy UnitCodeBase (Gate Mst_ProductController.cs:1451-1488)
                var rootQuery = _db.Set<Mst_Product>().AsNoTracking();
                if (!string.IsNullOrEmpty(prroot))
                {
                    rootQuery = rootQuery.Where(x => EF.Functions.Like((x.ProductCode ?? "").ToLower(), PortQuery.Equal(prroot), PortQuery.Esc));
                }
                if (!string.IsNullOrEmpty(orgId))
                {
                    rootQuery = rootQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
                }
                if (!string.IsNullOrEmpty(networkId))
                {
                    rootQuery = rootQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
                }

                var rootPrd = await rootQuery.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode).FirstOrDefaultAsync();
                if (rootPrd != null && !string.IsNullOrEmpty(rootPrd.UnitCode))
                {
                    unitCodeBase = rootPrd.UnitCode;
                }
            }

            // 4. Lấy thông tin nhóm sản phẩm List_Mst_ProductGroup_Async (Gate Mst_ProductController.cs:1554-1576)
            var groupQuery = _db.Set<Mst_ProductGroup>().AsNoTracking();
            if (!string.IsNullOrEmpty(orgId))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            var listMstProductGroup = await groupQuery.OrderBy(x => x.ProductGrpCode).ToListAsync();

            // Chuyển sang Mst_ProductGroupExt (Gate Mst_ProductController.cs:1780-1804)
            var listMst_ProductGroupUI = new List<Mst_ProductGroupExt>();
            foreach (var grp in listMstProductGroup)
            {
                listMst_ProductGroupUI.Add(new Mst_ProductGroupExt
                {
                    OrgID = grp.OrgID,
                    ProductGrpCode = grp.ProductGrpCode,
                    NetworkID = grp.NetworkID,
                    ProductGrpCodeParent = grp.ProductGrpCodeParent,
                    ProductGrpBUCode = grp.ProductGrpBUCode,
                    ProductGrpBUPattern = grp.ProductGrpBUPattern,
                    ProductGrpName = grp.ProductGrpName,
                    ProductGrpLevel = grp.ProductGrpLevel,
                    FlagActive = grp.FlagActive,
                    LogLUDTimeUTC = grp.LogLUDTimeUTC,
                    LogLUBy = grp.LogLUBy
                });
            }

            // 5. Truy vấn bảng Mst_Product (strWhereClause_Mst_Product_Update: Gate Mst_ProductController.cs:1492, 5224-5249; BIZ Product.cs:4352, 4420)
            var query = _db.Set<Mst_Product>().AsNoTracking();

            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(objMst_Product.ProductCode))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductCode ?? "").ToLower(), PortQuery.Equal(objMst_Product.ProductCode), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(objMst_Product.ProductCodeBase))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductCodeBase ?? "").ToLower(), PortQuery.Equal(objMst_Product.ProductCodeBase), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(objMst_Product.ProductCodeRoot))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductCodeRoot ?? "").ToLower(), PortQuery.Equal(objMst_Product.ProductCodeRoot), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(objMst_Product.ProductLevelSys))
            {
                query = query.Where(x => EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal(objMst_Product.ProductLevelSys), PortQuery.Esc));
            }

            var listProduct = await query.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode).ToListAsync();

            // 6. Xử lý tìm đơn vị cơ bản và đơn vị cấp 2 (Gate Mst_ProductController.cs:1728-1753)
            Mst_Product? objMst_Mst_Product_Root_Base = null;
            if (listProduct.Count > 0)
            {
                objMst_Mst_Product_Root_Base = listProduct.FirstOrDefault(it =>
                    !string.IsNullOrEmpty(it.ProductLevelSys) &&
                    (string.Equals(it.ProductLevelSys, "ROOTPRD", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(it.ProductLevelSys, "BASEPRD", StringComparison.OrdinalIgnoreCase)));

                foreach (var item in listProduct)
                {
                    var productLevelSys = item.ProductLevelSys ?? "";
                    if (string.Equals(productLevelSys, "L2PRD", StringComparison.OrdinalIgnoreCase))
                    {
                        listUnitCode.Add(item);
                    }
                }

                objRT_Mst_Product.Lst_Mst_Product = new List<Mst_Product>();
                if (objMst_Mst_Product_Root_Base != null)
                {
                    objRT_Mst_Product.Lst_Mst_Product.Add(objMst_Mst_Product_Root_Base);
                }
            }
            else
            {
                objRT_Mst_Product.Lst_Mst_Product = new List<Mst_Product>();
            }

            // GAP: Các bảng Mst_ProductType, Mst_Brand, Prd_DynamicField, Mst_Attribute, Mst_VATRate, Product_CustomField, Prd_BOM, Mst_ProductImages, Mst_ProductFiles chưa có entity trong repo đích (Rule A.18)
            // GAP: Mst_ProductService.Instance.WA_Mst_Product_Get gọi dịch vụ ngoài sang ProductCenter (Rule P3)

            var responseData = new Mst_Product_UpdateResponse
            {
                Rt_Mst_Product = objRT_Mst_Product,
                Product = objMst_Mst_Product_Root_Base,
                UnitCodeBase = unitCodeBase,
                ListUnitCode = listUnitCode,
                List_Mst_ProductGroup = listMstProductGroup,
                List_Mst_ProductGroupUI = listMst_ProductGroupUI,
                List_Mst_ProductType = new List<object>(),
                List_Mst_Brand = new List<object>(),
                List_Prd_DynamicField = new List<object>(),
                List_Mst_Attribute = new List<object>(),
                List_Mst_VATRate = new List<object>(),
                ListProduct_CustomField = new List<object>(),
                List_Mst_ProductImages = new List<object>(),
                List_Mst_ProductFiles = new List<object>()
            };

            // 7. Trả về kết quả JSON chuẩn hoá (Gate Mst_ProductController.cs:1888-1901 View(obj_RT_Mst_Product))
            return Ok(new
            {
                Success = true,
                Data = responseData,
                Rt_Mst_Product = objRT_Mst_Product,
                Lst_Mst_Product = objRT_Mst_Product.Lst_Mst_Product,
                Product = objMst_Mst_Product_Root_Base,
                UnitCodeBase = unitCodeBase,
                ListUnitCode = listUnitCode,
                List_Mst_ProductGroup = listMstProductGroup,
                List_Mst_ProductGroupUI = listMst_ProductGroupUI
            });
        }
        catch (Exception ex)
        {
            // BIZ Product.cs:4257, 4620: strErrorCodeDefault = TError.ErridQContract.Mst_Product_Get
            return PortResult.Fail("Mst_Product_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Cập nhật thông tin hàng hoá/dịch vụ (sản phẩm cấp 2 L2PRD hoặc sản phẩm Root/Base).
    /// Gate: Mst_ProductController.cs:1906-2721 Edit(string model)
    ///       Mst_ProductController.cs:2723-2764 MapProduct
    ///       Mst_ProductController.cs:5224-5249 strWhereClause_Mst_Product_Update
    ///       BaseController.cs:4145-4156 WA_Seq_GenObjCode_Get
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get, 90-158 WA_Mst_Product_Save
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get,
    ///       4805-4941 Mst_Product_Save, 4943-5621 Mst_Product_SaveX, 5623-5760 WAS_Mst_Product_Save
    /// </summary>
    [HttpPost("edit")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_EditReq? req)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Hỗ trợ truyền chuỗi model JSON như gate MVC (Mst_ProductController.cs:1906, 1922)
            if (!string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var deserialized = System.Text.Json.JsonSerializer.Deserialize<Mst_Product_EditReq>(
                        req.Model,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (deserialized != null)
                    {
                        req = deserialized;
                    }
                }
                catch
                {
                    // Giữ nguyên req nếu parse chuỗi thất bại
                }
            }

            // 2. Trích xuất thông tin sản phẩm chỉnh sửa
            var inputPrd = req?.Mst_Product;
            if (inputPrd == null && req != null && (!string.IsNullOrEmpty(req.ProductCode) || !string.IsNullOrEmpty(req.ProductLevelSys)))
            {
                inputPrd = new Mst_Product_EditItem
                {
                    OrgID = req.OrgID,
                    NetworkID = req.NetworkID,
                    ProductCode = req.ProductCode,
                    ProductLevelSys = req.ProductLevelSys,
                    ProductCodeUser = req.ProductCodeUser,
                    ProductName = req.ProductName,
                    ProductNameEN = req.ProductNameEN,
                    ProductBarCode = req.ProductBarCode,
                    ProductCodeBase = req.ProductCodeBase,
                    ProductCodeRoot = req.ProductCodeRoot,
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
                    ListOfPrdDynamicFieldValue = req.ListOfPrdDynamicFieldValue,
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
                    ProductMnfUrl = req.ProductMnfUrl,
                    ProductIntro = req.ProductIntro,
                    ProductUserGuide = req.ProductUserGuide,
                    ProductDrawing = req.ProductDrawing,
                    ProductOrigin = req.ProductOrigin
                };
            }

            // Guard: Nếu không có dữ liệu sản phẩm hoặc thiếu ProductLevelSys -> báo lỗi Mst_Product_Save (Gate bắt NullReferenceException dòng 2714)
            if (inputPrd == null || string.IsNullOrWhiteSpace(inputPrd.ProductLevelSys))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Product_Save", "Exception", "Thông tin sản phẩm không hợp lệ hoặc thiếu ProductLevelSys.");
            }

            var orgId = StdParam(inputPrd.OrgID ?? req?.OrgID) ?? "1001";
            var networkId = StdParam(inputPrd.NetworkID ?? req?.NetworkID) ?? "1001";
            var prdLevelSys = StdParam(inputPrd.ProductLevelSys);
            var flagRedirect = StdFlag(req?.FlagRedirect);
            var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // =========================================================================
            // NHÁNH 1: Sản phẩm cấp 2 L2PRD (Gate Mst_ProductController.cs:1964-2168)
            // =========================================================================
            if (string.Equals(prdLevelSys, "L2PRD", StringComparison.OrdinalIgnoreCase))
            {
                var prdCode = StdParam(inputPrd.ProductCode);
                if (string.IsNullOrEmpty(prdCode))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Product_Save", "Exception", "Mã sản phẩm không được để trống.");
                }

                // Tìm sản phẩm hiện có theo OrgID và ProductCode (strWhereClause_Mst_Product_Update dòng 1971)
                var existingPrd = await _db.Set<Mst_Product>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc)
                                           && EF.Functions.Like((x.ProductCode ?? "").ToLower(), PortQuery.Equal(prdCode), PortQuery.Esc));

                if (existingPrd == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Product_Save", "Exception", $"Không tìm thấy sản phẩm {prdCode} để sửa.");
                }

                // Cập nhật thông tin cơ bản qua MapProduct (Gate Mst_ProductController.cs:1977, 2723-2764)
                if (inputPrd.ProductCodeUser != null) existingPrd.ProductCodeUser = StdParam(inputPrd.ProductCodeUser);
                if (inputPrd.ProductName != null) existingPrd.ProductName = StdStr(inputPrd.ProductName);
                if (inputPrd.ProductNameEN != null) existingPrd.ProductNameEN = StdStr(inputPrd.ProductNameEN);
                if (inputPrd.ProductBarCode != null) existingPrd.ProductBarCode = StdParam(inputPrd.ProductBarCode);
                if (inputPrd.ListOfPrdDynamicFieldValue != null) existingPrd.ListOfPrdDynamicFieldValue = inputPrd.ListOfPrdDynamicFieldValue;
                if (inputPrd.BrandCode != null) existingPrd.BrandCode = StdParam(inputPrd.BrandCode);
                if (inputPrd.ProductType != null) existingPrd.ProductType = StdParam(inputPrd.ProductType);
                if (inputPrd.ProductGrpCode != null) existingPrd.ProductGrpCode = StdParam(inputPrd.ProductGrpCode);
                if (inputPrd.UnitCode != null) existingPrd.UnitCode = StdParam(inputPrd.UnitCode);
                if (inputPrd.ValConvert.HasValue) existingPrd.ValConvert = inputPrd.ValConvert;
                if (!string.IsNullOrEmpty(inputPrd.FlagSell)) existingPrd.FlagSell = StdFlag(inputPrd.FlagSell);
                if (!string.IsNullOrEmpty(inputPrd.FlagBuy)) existingPrd.FlagBuy = StdFlag(inputPrd.FlagBuy);
                if (inputPrd.UPBuy.HasValue) existingPrd.UPBuy = inputPrd.UPBuy;
                if (inputPrd.UPSell.HasValue) existingPrd.UPSell = inputPrd.UPSell;
                if (inputPrd.QtyMaxSt.HasValue) existingPrd.QtyMaxSt = inputPrd.QtyMaxSt;
                if (inputPrd.QtyMinSt.HasValue) existingPrd.QtyMinSt = inputPrd.QtyMinSt;
                if (inputPrd.QtyEffSt.HasValue) existingPrd.QtyEffSt = inputPrd.QtyEffSt;
                if (inputPrd.VATRateCode != null) existingPrd.VATRateCode = StdParam(inputPrd.VATRateCode);
                if (inputPrd.Remark != null) existingPrd.Remark = StdStr(inputPrd.Remark);
                if (inputPrd.CustomField1 != null) existingPrd.CustomField1 = StdStr(inputPrd.CustomField1);
                if (inputPrd.CustomField2 != null) existingPrd.CustomField2 = StdStr(inputPrd.CustomField2);
                if (inputPrd.CustomField3 != null) existingPrd.CustomField3 = StdStr(inputPrd.CustomField3);
                if (inputPrd.CustomField4 != null) existingPrd.CustomField4 = StdStr(inputPrd.CustomField4);
                if (inputPrd.CustomField5 != null) existingPrd.CustomField5 = StdStr(inputPrd.CustomField5);
                if (!string.IsNullOrEmpty(inputPrd.FlagFG)) existingPrd.FlagFG = StdFlag(inputPrd.FlagFG);
                if (!string.IsNullOrEmpty(inputPrd.FlagSerial)) existingPrd.FlagSerial = StdFlag(inputPrd.FlagSerial);
                if (!string.IsNullOrEmpty(inputPrd.FlagLot)) existingPrd.FlagLot = StdFlag(inputPrd.FlagLot);
                if (!string.IsNullOrEmpty(inputPrd.FlagActive)) existingPrd.FlagActive = StdFlag(inputPrd.FlagActive);
                if (inputPrd.ProductStd != null) existingPrd.ProductStd = StdStr(inputPrd.ProductStd);
                if (inputPrd.ProductExpiry != null) existingPrd.ProductExpiry = StdStr(inputPrd.ProductExpiry);
                if (inputPrd.ProductQuyCach != null) existingPrd.ProductQuyCach = StdStr(inputPrd.ProductQuyCach);
                if (inputPrd.ProductMnfUrl != null) existingPrd.ProductMnfUrl = StdStr(inputPrd.ProductMnfUrl);
                if (inputPrd.ProductIntro != null) existingPrd.ProductIntro = StdStr(inputPrd.ProductIntro);
                if (inputPrd.ProductUserGuide != null) existingPrd.ProductUserGuide = StdStr(inputPrd.ProductUserGuide);
                if (inputPrd.ProductDrawing != null) existingPrd.ProductDrawing = StdStr(inputPrd.ProductDrawing);
                if (inputPrd.ProductOrigin != null) existingPrd.ProductOrigin = StdStr(inputPrd.ProductOrigin);

                existingPrd.LUDTimeUTC = dtimeSys;
                existingPrd.LUBy = "port";
                existingPrd.LogLUDTimeUTC = dtimeSys;
                existingPrd.LogLUBy = "port";

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // GAP: Mst_ProductService.Instance.WA_Mst_Product_Update gọi sang hệ thống ngoài Product Center bỏ qua theo Rule P3
                // GAP: Prd_BOM, Mst_ProductImages, Mst_ProductFiles chưa có entity trong repo đích (Rule A.18)

                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Hàng hóa đã được sửa thành công!" },
                    RedirectUrl = flagRedirect == "1" ? "/api/mst_product/create" : "/api/mst_product/index"
                });
            }
            // =========================================================================
            // NHÁNH 2: Sản phẩm ROOTPRD hoặc BASEPRD (Gate Mst_ProductController.cs:2169-2711)
            // =========================================================================
            else if (string.Equals(prdLevelSys, "ROOTPRD", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(prdLevelSys, "BASEPRD", StringComparison.OrdinalIgnoreCase))
            {
                var proRoot = StdParam(inputPrd.ProductCodeRoot) ?? StdParam(inputPrd.ProductCode);
                var proBase = StdParam(inputPrd.ProductCodeBase) ?? StdParam(inputPrd.ProductCode);

                if (string.IsNullOrEmpty(proRoot))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Product_Save", "Exception", "Mã sản phẩm gốc (ProductCodeRoot) không được để trống.");
                }

                // Tìm các sản phẩm có cùng ProductCodeRoot (Gate Mst_ProductController.cs:2179-2188)
                var rootPattern = PortQuery.Equal(proRoot);
                var listProductSearch = await _db.Set<Mst_Product>()
                    .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc)
                             && (EF.Functions.Like((x.ProductCodeRoot ?? "").ToLower(), rootPattern, PortQuery.Esc)
                                 || EF.Functions.Like((x.ProductCode ?? "").ToLower(), rootPattern, PortQuery.Esc)))
                    .ToListAsync();

                if (listProductSearch.Count == 0)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Product_Save", "Exception", $"Không tìm thấy sản phẩm có mã gốc {proRoot}.");
                }

                // 2.1. Cập nhật thông tin sản phẩm khớp ProductCode (Gate Mst_ProductController.cs:2208-2245)
                var targetPrd = listProductSearch.FirstOrDefault(it => string.Equals(it.ProductCode, inputPrd.ProductCode, StringComparison.OrdinalIgnoreCase))
                             ?? listProductSearch.FirstOrDefault(it => string.Equals(it.ProductCodeRoot, proRoot, StringComparison.OrdinalIgnoreCase)
                                                                    && string.Equals(it.ProductLevelSys, "ROOTPRD", StringComparison.OrdinalIgnoreCase))
                             ?? listProductSearch[0];

                if (inputPrd.ProductCodeUser != null) targetPrd.ProductCodeUser = StdParam(inputPrd.ProductCodeUser);
                if (inputPrd.ProductName != null) targetPrd.ProductName = StdStr(inputPrd.ProductName);
                if (inputPrd.ProductNameEN != null) targetPrd.ProductNameEN = StdStr(inputPrd.ProductNameEN);
                if (inputPrd.ProductBarCode != null) targetPrd.ProductBarCode = StdParam(inputPrd.ProductBarCode);
                if (!string.IsNullOrEmpty(inputPrd.FlagBuy)) targetPrd.FlagBuy = StdFlag(inputPrd.FlagBuy);
                if (!string.IsNullOrEmpty(inputPrd.FlagLot)) targetPrd.FlagLot = StdFlag(inputPrd.FlagLot);
                if (!string.IsNullOrEmpty(inputPrd.FlagSell)) targetPrd.FlagSell = StdFlag(inputPrd.FlagSell);
                if (!string.IsNullOrEmpty(inputPrd.FlagSerial)) targetPrd.FlagSerial = StdFlag(inputPrd.FlagSerial);
                if (inputPrd.ProductDrawing != null) targetPrd.ProductDrawing = StdStr(inputPrd.ProductDrawing);
                if (inputPrd.ProductExpiry != null) targetPrd.ProductExpiry = StdStr(inputPrd.ProductExpiry);
                if (inputPrd.ProductIntro != null) targetPrd.ProductIntro = StdStr(inputPrd.ProductIntro);
                if (inputPrd.ProductMnfUrl != null) targetPrd.ProductMnfUrl = StdStr(inputPrd.ProductMnfUrl);
                if (inputPrd.ProductOrigin != null) targetPrd.ProductOrigin = StdStr(inputPrd.ProductOrigin);
                if (inputPrd.ProductQuyCach != null) targetPrd.ProductQuyCach = StdStr(inputPrd.ProductQuyCach);
                if (inputPrd.ProductStd != null) targetPrd.ProductStd = StdStr(inputPrd.ProductStd);
                if (inputPrd.ProductUserGuide != null) targetPrd.ProductUserGuide = StdStr(inputPrd.ProductUserGuide);
                if (inputPrd.QtyEffSt.HasValue) targetPrd.QtyEffSt = inputPrd.QtyEffSt;
                if (inputPrd.QtyMaxSt.HasValue) targetPrd.QtyMaxSt = inputPrd.QtyMaxSt;
                if (inputPrd.QtyMinSt.HasValue) targetPrd.QtyMinSt = inputPrd.QtyMinSt;
                if (inputPrd.Remark != null) targetPrd.Remark = StdStr(inputPrd.Remark);
                if (inputPrd.UPBuy.HasValue) targetPrd.UPBuy = inputPrd.UPBuy;
                if (inputPrd.UPSell.HasValue) targetPrd.UPSell = inputPrd.UPSell;
                if (inputPrd.BrandCode != null) targetPrd.BrandCode = StdParam(inputPrd.BrandCode);
                if (inputPrd.ProductGrpCode != null) targetPrd.ProductGrpCode = StdParam(inputPrd.ProductGrpCode);
                if (inputPrd.VATRateCode != null) targetPrd.VATRateCode = StdParam(inputPrd.VATRateCode);
                if (inputPrd.ListOfPrdDynamicFieldValue != null) targetPrd.ListOfPrdDynamicFieldValue = inputPrd.ListOfPrdDynamicFieldValue;
                if (inputPrd.CustomField1 != null) targetPrd.CustomField1 = StdStr(inputPrd.CustomField1);
                if (inputPrd.CustomField2 != null) targetPrd.CustomField2 = StdStr(inputPrd.CustomField2);
                if (inputPrd.CustomField3 != null) targetPrd.CustomField3 = StdStr(inputPrd.CustomField3);
                if (inputPrd.CustomField4 != null) targetPrd.CustomField4 = StdStr(inputPrd.CustomField4);
                if (inputPrd.CustomField5 != null) targetPrd.CustomField5 = StdStr(inputPrd.CustomField5);
                if (!string.IsNullOrEmpty(inputPrd.FlagFG)) targetPrd.FlagFG = StdFlag(inputPrd.FlagFG);
                if (!string.IsNullOrEmpty(inputPrd.FlagActive)) targetPrd.FlagActive = StdFlag(inputPrd.FlagActive);

                targetPrd.LUDTimeUTC = dtimeSys;
                targetPrd.LUBy = "port";
                targetPrd.LogLUDTimeUTC = dtimeSys;
                targetPrd.LogLUBy = "port";

                // 2.2. Xử lý thêm thuộc tính mới - nối thêm vào ProductName (Gate Mst_ProductController.cs:2340-2356)
                if (req?.Lst_Prd_Attribute_New != null && req.Lst_Prd_Attribute_New.Count > 0)
                {
                    foreach (var item in listProductSearch)
                    {
                        foreach (var it in req.Lst_Prd_Attribute_New)
                        {
                            if (!string.IsNullOrEmpty(it.AttributeValue))
                            {
                                item.ProductName = item.ProductName + "-" + it.AttributeValue.Trim();
                            }
                        }
                    }
                }

                // 2.3. Kiểm tra đơn vị trùng (Gate Mst_ProductController.cs:2380-2403)
                // LỖI NGUỒN GIỮ NGUYÊN (Mst_ProductController.cs:2398-2400): Khi trùng đơn vị, gate gán resultEntry.Success = true kèm câu cảnh báo
                var listUnitOld = listProductSearch
                    .Select(x => x.UnitCode?.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                if (req?.Lst_Unit != null && req.Lst_Unit.Count > 0 && req.Lst_Mst_ProductUI_Create != null && req.Lst_Mst_ProductUI_Create.Count > 0)
                {
                    foreach (var item in req.Lst_Mst_ProductUI_Create)
                    {
                        var uCode = item.UnitCode?.Trim();
                        if (!string.IsNullOrEmpty(uCode) && listUnitOld.Any(oldUnit => string.Equals(oldUnit, uCode, StringComparison.OrdinalIgnoreCase)))
                        {
                            await tx.RollbackAsync();
                            return Ok(new
                            {
                                Success = true,
                                Messages = new[] { "Mã đơn vị '" + item.UnitCode + "' không được trùng!" }
                            });
                        }
                    }
                }

                // 2.4. Cập nhật thông tin đơn vị cho sản phẩm cơ sở / con (Gate Mst_ProductController.cs:2404-2440)
                if (req?.Lst_Unit != null && req.Lst_Unit.Count > 0)
                {
                    foreach (var unitItem in req.Lst_Unit)
                    {
                        var matchingPrd = listProductSearch.FirstOrDefault(it =>
                            string.Equals(it.ProductCode, unitItem.ProductCode, StringComparison.OrdinalIgnoreCase));
                        if (matchingPrd != null)
                        {
                            if (unitItem.UnitCode != null) matchingPrd.UnitCode = StdParam(unitItem.UnitCode);
                            if (unitItem.UPBuy.HasValue) matchingPrd.UPBuy = unitItem.UPBuy;
                            if (unitItem.UPSell.HasValue) matchingPrd.UPSell = unitItem.UPSell;
                            if (unitItem.ValConvert.HasValue) matchingPrd.ValConvert = unitItem.ValConvert;
                            if (!string.IsNullOrEmpty(unitItem.FlagBuy)) matchingPrd.FlagBuy = StdFlag(unitItem.FlagBuy);
                            if (!string.IsNullOrEmpty(unitItem.FlagSell)) matchingPrd.FlagSell = StdFlag(unitItem.FlagSell);
                            matchingPrd.LUDTimeUTC = dtimeSys;
                            matchingPrd.LUBy = "port";
                        }
                    }
                }

                // 2.5. Thêm sản phẩm mới khi thêm đơn vị quy đổi (Gate Mst_ProductController.cs:2442-2525)
                if (req?.Lst_Mst_ProductUI_Create != null && req.Lst_Mst_ProductUI_Create.Count > 0)
                {
                    for (int i = 0; i < req.Lst_Mst_ProductUI_Create.Count; i++)
                    {
                        var newItem = req.Lst_Mst_ProductUI_Create[i];
                        var newCode = !string.IsNullOrEmpty(newItem.ProductCode)
                            ? StdParam(newItem.ProductCode)!
                            : $"PRD_NEW_{DateTime.UtcNow:yyyyMMddHHmmss}_{i + 1}";

                        var timestampSuffix = DateTime.Now.ToString("yyMMddHHmmsss");
                        var baseUserCode = StdParam(newItem.ProductCodeUser) ?? targetPrd.ProductCodeUser ?? "PRD";
                        var newUserCode = $"{baseUserCode}-{timestampSuffix}";

                        var newProductEntity = new Mst_Product
                        {
                            OrgID = orgId,
                            NetworkID = networkId,
                            ProductCode = newCode,
                            ProductLevelSys = StdParam(newItem.ProductLevelSys) ?? "BASEPRD",
                            ProductCodeUser = newUserCode,
                            BrandCode = StdParam(newItem.BrandCode) ?? targetPrd.BrandCode,
                            ProductType = StdParam(newItem.ProductType) ?? targetPrd.ProductType ?? "PRODUCT",
                            ProductGrpCode = StdParam(newItem.ProductGrpCode) ?? targetPrd.ProductGrpCode,
                            ProductName = StdStr(newItem.ProductName) ?? targetPrd.ProductName,
                            ProductNameEN = StdStr(newItem.ProductNameEN) ?? targetPrd.ProductNameEN,
                            ProductBarCode = StdParam(newItem.ProductBarCode),
                            ProductCodeNetwork = null,
                            ProductCodeBase = StdParam(newItem.ProductCodeBase) ?? proBase,
                            ProductCodeRoot = proRoot,
                            ProductImagePathList = null,
                            ProductFilePathList = null,
                            UnitCode = StdParam(newItem.UnitCode) ?? "CAI",
                            ValConvert = newItem.ValConvert ?? 1.0,
                            FlagSell = !string.IsNullOrEmpty(newItem.FlagSell) ? StdFlag(newItem.FlagSell) : targetPrd.FlagSell ?? "1",
                            FlagBuy = !string.IsNullOrEmpty(newItem.FlagBuy) ? StdFlag(newItem.FlagBuy) : targetPrd.FlagBuy ?? "1",
                            UPBuy = newItem.UPBuy ?? targetPrd.UPBuy,
                            UPSell = newItem.UPSell ?? targetPrd.UPSell,
                            QtyMaxSt = newItem.QtyMaxSt,
                            QtyMinSt = newItem.QtyMinSt,
                            QtyEffSt = newItem.QtyEffSt,
                            ListOfPrdDynamicFieldValue = null,
                            Remark = StdStr(newItem.Remark) ?? targetPrd.Remark,
                            VATRateCode = StdParam(newItem.VATRateCode) ?? targetPrd.VATRateCode,
                            FlagFG = !string.IsNullOrEmpty(newItem.FlagFG) ? StdFlag(newItem.FlagFG) : targetPrd.FlagFG,
                            FlagSerial = !string.IsNullOrEmpty(newItem.FlagSerial) ? StdFlag(newItem.FlagSerial) : targetPrd.FlagSerial,
                            FlagLot = !string.IsNullOrEmpty(newItem.FlagLot) ? StdFlag(newItem.FlagLot) : targetPrd.FlagLot,
                            ProductStd = StdStr(newItem.ProductStd) ?? targetPrd.ProductStd,
                            ProductExpiry = StdStr(newItem.ProductExpiry) ?? targetPrd.ProductExpiry,
                            ProductQuyCach = StdStr(newItem.ProductQuyCach) ?? targetPrd.ProductQuyCach,
                            ProductMnfUrl = StdStr(newItem.ProductMnfUrl) ?? targetPrd.ProductMnfUrl,
                            ProductIntro = StdStr(newItem.ProductIntro) ?? targetPrd.ProductIntro,
                            ProductUserGuide = StdStr(newItem.ProductUserGuide) ?? targetPrd.ProductUserGuide,
                            ProductDrawing = StdStr(newItem.ProductDrawing) ?? targetPrd.ProductDrawing,
                            ProductOrigin = StdStr(newItem.ProductOrigin) ?? targetPrd.ProductOrigin,
                            CreateDTimeUTC = dtimeSys,
                            CreateBy = "port",
                            LUDTimeUTC = dtimeSys,
                            LUBy = "port",
                            FlagActive = !string.IsNullOrEmpty(newItem.FlagActive) ? StdFlag(newItem.FlagActive) : "1",
                            LogLUDTimeUTC = dtimeSys,
                            LogLUBy = "port",
                            CodeGuid = Guid.NewGuid().ToString()
                        };

                        _db.Set<Mst_Product>().Add(newProductEntity);
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // GAP: Mst_ProductService.Instance.WA_Mst_Product_Update gọi sang hệ thống ngoài Product Center bỏ qua theo Rule P3
                // GAP: Prd_Attribute, Prd_BOM, Mst_ProductImages, Mst_ProductFiles chưa có entity trong repo đích (Rule A.18)

                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Hàng hóa đã được sửa thành công!" },
                    RedirectUrl = flagRedirect == "1" ? "/api/mst_product/create" : "/api/mst_product/index"
                });
            }
            else
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Product_Save", "Exception", $"ProductLevelSys '{prdLevelSys}' không được hỗ trợ.");
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // BIZ Product.cs:4827, 4918: strErrorCodeDefault = TError.ErridQContract.Mst_Product_Save
            return PortResult.Fail("Mst_Product_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xóa đơn vị hàng hóa/dịch vụ (sản phẩm cấp 2 L2PRD hoặc sản phẩm liên quan).
    /// Gate: Mst_ProductController.cs:2880-2943 DeleteUnit(string prdCode, string prdBase, string prdRoot, string prdlvs)
    ///       Mst_ProductController.cs:5224-5249 strWhereClause_Mst_Product_Update(Mst_Product model)
    /// API:  MstProductController.cs:19-87 WA_Mst_Product_Get
    /// BIZ:  Product.cs:4230-4643 Mst_Product_Get, 4644-4802 WAS_Mst_Product_Get
    /// </summary>
    [HttpPost("deleteunit")]
    public async Task<IActionResult> DeleteUnit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_DeleteUnitReq? req)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Kiểm tra mã sản phẩm trống (Gate Mst_ProductController.cs:2884, 2936-2940)
            var rawPrdCode = req?.prdCode ?? req?.ProductCode;
            if (string.IsNullOrWhiteSpace(rawPrdCode))
            {
                await tx.RollbackAsync();
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Mã sản phẩm trống!" },
                    Title = "Mã sản phẩm trống!"
                });
            }

            var prdCode = StdParam(rawPrdCode);
            if (string.IsNullOrEmpty(prdCode))
            {
                await tx.RollbackAsync();
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Mã sản phẩm trống!" },
                    Title = "Mã sản phẩm trống!"
                });
            }

            // 2. Tra cứu sản phẩm trong hệ thống qua WA_Mst_Product_Get (Gate Mst_ProductController.cs:2886-2915, BIZ Product.cs:4230-4643)
            var query = _db.Set<Mst_Product>().AsQueryable();

            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            query = query.Where(x => EF.Functions.Like((x.ProductCode ?? "").ToLower(), PortQuery.Equal(prdCode), PortQuery.Esc));

            var existingPrd = await query.FirstOrDefaultAsync();

            // 3. Nếu không tìm thấy sản phẩm trong hệ thống (Gate Mst_ProductController.cs:2930-2934)
            if (existingPrd == null)
            {
                await tx.RollbackAsync();
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { $"Mã sản phẩm '{rawPrdCode}' không có trong hệ thống!" },
                    Title = $"Mã sản phẩm '{rawPrdCode}' không có trong hệ thống!"
                });
            }

            // 4. Xóa bản ghi sản phẩm khỏi cơ sở dữ liệu (Gate Mst_ProductController.cs:2918-2924)
            _db.Set<Mst_Product>().Remove(existingPrd);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // GAP: Mst_ProductService.Instance.WA_Mst_Product_Delete gọi sang hệ thống ngoài Product Center bỏ qua theo Rule P3 (Mst_ProductController.cs:2924)

            // 5. Trả về thông báo thành công và chuyển hướng về Index (Gate Mst_ProductController.cs:2926-2928)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Xóa đơn vị thành công!" },
                Title = "Xóa đơn vị thành công!",
                RedirectUrl = "/api/mst_product/index"
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // BIZ Product.cs:4257, 4620: strErrorCodeDefault = TError.ErridQContract.Mst_Product_Get
            return PortResult.Fail("Mst_Product_DeleteUnit", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xuất tệp mẫu Excel để nhập sản phẩm.
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ProductController.cs:2950-2960 ExportTemplate()
    ///       hàm phụ: Mst_ProductController.cs:4818-4859 (GetImportDicColumsTemplate), BaseController.cs:664-682 (GenExcelExportFilePath)
    /// </summary>
    [HttpPost("exporttemplate")]
    [HttpPost("export-template")]
    public IActionResult ExportTemplate([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Product_ExportTemplateReq? req = null)
    {
        try
        {
            // 1. Tạo danh sách rỗng (Gate line 2952: var list = new List<Mst_Product>())
            var list = new List<Mst_Product>();

            // 2. Lấy từ điển cột từ GetImportDicColumsTemplate() gồm 33 cột (Gate lines 2954, 4818-4859)
            var dicColNames = ExportTemplateDicColumns;

            // 3. Sinh đường dẫn file vật lý và URL ảo (Gate lines 2955-2956: GenExcelExportFilePath)
            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_Product));

            // 4. Xuất danh sách rỗng ra file Excel chứa dòng tiêu đề (Gate line 2957: ExcelExport.ExportToExcelFromList)
            PortExcel.ExportToExcelFromList(list, dicColNames, filePath, nameof(Mst_Product));

            // 5. Trả kết quả thành công khớp gate MVC (Gate line 2959)
            return Ok(new
            {
                Success = true,
                Title = "Đã xuất excel thành công!",
                CheckSuccess = "1",
                strUrl = virtualPath
            });
        }
        catch (Exception ex)
        {
            // Gate lines 2950-2960; xử lý ngoại lệ hệ thống và trả lỗi chuẩn
            return PortResult.Fail("Mst_Product_ExportTemplate", "Exception", ex.Message);
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

/// <summary>
/// Tham số đầu vào cho action SearchProduct của Mst_ProductController.
/// Gate: Mst_ProductController.cs:1154 SearchProduct(string prdid = "")
/// </summary>
public class Mst_Product_SearchProductReq
{
    // Gate Mst_ProductController.cs:1154
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

/// <summary>
/// DTO kết quả trả về RT_Mst_Product theo nguồn D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\RT_Mst_Product.cs.
/// </summary>
public class RT_Mst_Product
{
    public List<Mst_Product>? Lst_Mst_Product { get; set; }
    public List<Prd_Attribute>? Lst_Prd_Attribute { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action GetSearch của Mst_ProductController.
/// Gate: Mst_ProductController.cs:1210-1220 GetSearch(...)
/// </summary>
public class Mst_Product_GetSearchReq
{
    // Gate Mst_ProductController.cs:1210-1219
    public string? ProductCodeUserName { get; set; }
    public string? ProductGrpCode { get; set; }
    public string? CkbProduct { get; set; }
    public string? CkbServices { get; set; }
    public string? CkbCombo { get; set; }
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
    public string? FlagActive { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ProductCode { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action ProductIngredient của Mst_ProductController.
/// Gate: Mst_ProductController.cs:1322-1326
/// </summary>
public class Mst_Product_ProductIngredientReq
{
    // Gate Mst_ProductController.cs:1322-1326
    public string? ProductCodeUserName { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ModelproductGrpCode { get; set; }
    public string? ProductGrpCode { get; set; }
    public string? Init { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? PageCount { get; set; }
    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    // Các tham số lọc bổ sung (LESSON A.14)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action Update của Mst_ProductController.
/// Gate: Mst_ProductController.cs:1418-1422 Update(string prcode, string prbscode, string prroot, string prlv)
/// </summary>
public class Mst_Product_UpdateReq
{
    // Gate Mst_ProductController.cs:1418-1422
    public string? prcode { get; set; }
    public string? prbscode { get; set; }
    public string? prroot { get; set; }
    public string? prlv { get; set; }

    // Hỗ trợ alias tên trường chuẩn (ProductCode, ProductCodeBase, ProductCodeRoot, ProductLevelSys)
    public string? ProductCode { get; set; }
    public string? ProductCodeBase { get; set; }
    public string? ProductCodeRoot { get; set; }
    public string? ProductLevelSys { get; set; }

    // Các tham số lọc bổ sung (LESSON A.14)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}

/// <summary>
/// DTO chứa dữ liệu trả về cho action Update của Mst_ProductController.
/// Gate: Mst_ProductController.cs:1888-1901
/// </summary>
public class Mst_Product_UpdateResponse
{
    public RT_Mst_Product? Rt_Mst_Product { get; set; }
    public Mst_Product? Product { get; set; }
    public string? UnitCodeBase { get; set; }
    public List<Mst_Product> ListUnitCode { get; set; } = new();
    public List<Mst_ProductGroup> List_Mst_ProductGroup { get; set; } = new();
    public List<Mst_ProductGroupExt> List_Mst_ProductGroupUI { get; set; } = new();
    public List<object> List_Mst_ProductType { get; set; } = new();
    public List<object> List_Mst_Brand { get; set; } = new();
    public List<object> List_Prd_DynamicField { get; set; } = new();
    public List<object> List_Mst_Attribute { get; set; } = new();
    public List<object> List_Mst_VATRate { get; set; } = new();
    public List<object> ListProduct_CustomField { get; set; } = new();
    public List<object> List_Mst_ProductImages { get; set; } = new();
    public List<object> List_Mst_ProductFiles { get; set; } = new();
}

/// <summary>
/// DTO mở rộng thông tin nhóm hàng hoá hiển thị trên giao diện Update.
/// Gate: Mst_ProductController.cs:1780-1804
/// </summary>
public class Mst_ProductGroupExt
{
    public string? OrgID { get; set; }
    public string? ProductGrpCode { get; set; }
    public string? NetworkID { get; set; }
    public string? ProductGrpCodeParent { get; set; }
    public string? ProductGrpBUCode { get; set; }
    public string? ProductGrpBUPattern { get; set; }
    public string? ProductGrpName { get; set; }
    public string? ProductGrpLevel { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action Edit của Mst_ProductController.
/// Gate: Mst_ProductController.cs:1906-2721 Edit(string model)
/// </summary>
public class Mst_Product_EditReq
{
    // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_ProductController.cs:1906, 1922)
    public string? Model { get; set; }

    // Thông tin sản phẩm chỉnh sửa theo RQ_Mst_ProductUI (Gate Mst_ProductController.cs:1929)
    public Mst_Product_EditItem? Mst_Product { get; set; }

    // Danh sách sản phẩm mới khi thêm đơn vị khác đơn vị cơ bản (Gate Mst_ProductController.cs:1923, 2442)
    public List<Mst_Product_EditItem>? Lst_Mst_ProductUI_Create { get; set; }

    // Danh sách thuộc tính cần cập nhật (Gate Mst_ProductController.cs:2267)
    public List<Prd_Attribute>? Lst_Prd_Attribute { get; set; }

    // Danh sách thuộc tính mới (Gate Mst_ProductController.cs:2338)
    public List<Prd_Attribute>? Lst_Prd_Attribute_New { get; set; }

    // Danh sách thuộc tính cần xoá (Gate Mst_ProductController.cs:2250)
    public List<Prd_Attribute>? Lst_Prd_Attribute_Delete { get; set; }

    // Danh sách đơn vị cần cập nhật (Gate Mst_ProductController.cs:2389, 2409)
    public List<Mst_Unit_EditItem>? Lst_Unit { get; set; }

    // Danh sách đơn vị cần xoá (Gate Mst_ProductController.cs:2362)
    public List<Mst_Unit_EditItem>? Lst_Unit_Delete { get; set; }

    // Danh sách hình ảnh, tệp tin và BOM
    public List<Mst_ProductImages_Item>? Lst_Mst_ProductImages { get; set; }
    public List<Mst_ProductFiles_Item>? Lst_Mst_ProductFiles { get; set; }
    public List<Prd_BOM_Item>? Lst_Prd_BOM { get; set; }

    // Cờ chuyển hướng sau khi sửa (Gate Mst_ProductController.cs:1932, 2158, 2702)
    public string? FlagRedirect { get; set; }

    // Hỗ trợ truyền phẳng các trường sản phẩm ở cấp cao nhất
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
}

/// <summary>
/// DTO thông tin sản phẩm chỉnh sửa (Gate Mst_ProductController.cs:1929, RQ_Mst_ProductUI.cs).
/// </summary>
public class Mst_Product_EditItem
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

    public List<Prd_Attribute>? Lst_Prd_Attribute { get; set; }
}

/// <summary>
/// DTO thông tin đơn vị quy đổi chỉnh sửa (Gate Mst_ProductController.cs:2389, 2409).
/// </summary>
public class Mst_Unit_EditItem
{
    public string? UnitCode { get; set; }
    public string? ProductCode { get; set; }
    public double? UPBuy { get; set; }
    public double? UPSell { get; set; }
    public double? ValConvert { get; set; }
    public string? FlagBuy { get; set; }
    public string? FlagSell { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action DeleteUnit của Mst_ProductController.
/// Gate: Mst_ProductController.cs:2880-2881 DeleteUnit(string prdCode, string prdBase, string prdRoot, string prdlvs)
/// </summary>
public class Mst_Product_DeleteUnitReq
{
    // Gate Mst_ProductController.cs:2880
    public string? prdCode { get; set; }
    public string? prdBase { get; set; }
    public string? prdRoot { get; set; }
    public string? prdlvs { get; set; }

    // Hỗ trợ alias tên trường chuẩn
    public string? ProductCode { get; set; }
    public string? ProductCodeBase { get; set; }
    public string? ProductCodeRoot { get; set; }
    public string? ProductLevelSys { get; set; }

    // Các tham số lọc bổ sung (LESSON A.14)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}

/// <summary>
/// Tham số đầu vào cho action ExportTemplate của Mst_ProductController.
/// Gate: Mst_ProductController.cs:2950-2960 ExportTemplate()
/// </summary>
public class Mst_Product_ExportTemplateReq
{
}

