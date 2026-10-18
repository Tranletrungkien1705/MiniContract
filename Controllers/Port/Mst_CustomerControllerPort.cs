using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_CustomerController.cs — Mst_Customer
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_CustomerController.cs:35-207 (Index), 211-323 (DoSearch), 435-561 (Export), 567-578 (ExportTemplate), 1372-1461 (Delete), 1466-1799 (Create GET), 1801-2038 (Create POST), 2043-2166 (Customer_Check), 2170-2606 (Update)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstCustomerController.cs:92-162 (WA_Mst_Customer_Get), 19-89 (WA_Mst_Customer_Save)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Customer\Customer.cs:1464-1612 (WAS_Mst_Customer_Get), 996-1462 (Mst_Customer_Get), 2968-3070 (WAS_Mst_Customer_Save), 1619-1732 (Mst_Customer_Save), 2359-2966 (Mst_Customer_SaveX_New20231205)
[ApiController]
[Route("api/mst_customer")]
public class Mst_CustomerControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment? _env;
    private readonly IConfiguration? _config;

    // Từ điển cột xuất excel theo GetImportDicColumsTemplate() (Mst_CustomerController.cs:1322-1366)
    private static readonly Dictionary<string, string> DicColNames = new()
    {
        { "CustomerCode", "Mã khách hàng(*)" },
        { "CustomerName", "Tên khách hàng(*)" },
        { "mct_CustomerTypeName", "Tên loại khách hàng" },
        { "mcs_CustomerSourceName", "Tên nguồn khách hàng" },
        { "mcg_CustomerGrpName", "Tên nhóm khách hàng" },
        { "CustomerPhoneNo", "Điện thoại cố định" },
        { "CustomerMobilePhone", "Điện thoại di động" },
        { "CustomerAvatarPath", "Đường dẫn ảnh avatar" },
        { "CustomerGender", "Giới tính" },
        { "CustomerEmail", "Email khách hàng" },
        { "CustomerAddress", "Địa chỉ" },
        { "ProvinceCode", "Tỉnh thành" },
        { "DistrictCode", "Quận huyện" },
        { "WardCode", "Phường xã" },
        { "AreaCode", "Khu vực" },
        { "FlagDealer", "Là đại lý (1)" },
        { "FlagSupplier", "Là nhà cung cấp (1)" },
        { "FlagShipper", "Là đơn vị giao hàng (1)" },
        { "FlagEndUser", "Là khách hàng cuối (1)" },
        { "FlagBank", "Là ngân hàng (1)" },
        { "FlagInsurrance", "Bảo hiểm (1)" },
        { "RepresentName", "Người đại diện" },
        { "RepresentPosition", "Chức vụ" },
        { "GovIDCardNo", "Số giấy tờ" },
        { "GovIDType", "Loại giấy tờ" },
        { "BankAccountNo", "Số tài khoản" },
        { "BankName", "Tên ngân hàng" },
        { "ContactName", "Người liên hệ" },
        { "ContactPhone", "Điện thoại liên hệ" },
        { "ContactEmail", "Email liên hệ" },
        { "Fax", "Fax" },
        { "CustomerDateOfBirth", "Ngày sinh/ Ngày thành lập" },
        { "Facebook", "Facebook" },
        { "InvoiceCustomerName", "Người mua hàng" },
        { "InvoiceOrgName", "Tên tổ chức" },
        { "MST", "Mã số thuế" },
        { "InvoiceCustomerAddress", "Địa chỉ hợp đồng" },
        { "InvoiceEmailSend", "Email nhận hợp đồng" },
        { "ListOfCustDynamicFieldValue", "Thuộc tính thêm" }
    };

    public Mst_CustomerControllerPort(AppDbContext db, IWebHostEnvironment? env = null, IConfiguration? config = null)
    {
        _db = db;
        _env = env;
        _config = config;
    }

    /// <summary>
    /// Danh sách / Tìm kiếm phân trang khách hàng.
    /// Gate: Mst_CustomerController.cs:35-207 Index(...)
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Customer_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_CustomerController.cs:35-45, 165-166
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            // 2. Dựng truy vấn lọc theo các cột của Mst_Customer (Customer.cs:1120-1137, 1344-1386)
            var query = _db.Set<Mst_Customer>().AsNoTracking();

            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            var networkId = StdParam(req?.NetworkID);
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            var flagActive = StdFlag(req?.FlagActive);
            if (!string.IsNullOrEmpty(req?.FlagActive))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            var customerName = StdStr(req?.CustomerName);
            if (!string.IsNullOrEmpty(customerName))
            {
                query = query.Where(x => EF.Functions.Like((x.CustomerName ?? "").ToLower(), PortQuery.Contains(customerName), PortQuery.Esc));
            }

            var keyWord = StdStr(req?.KeyWord);
            if (!string.IsNullOrEmpty(keyWord))
            {
                var kwPattern = PortQuery.Contains(keyWord);
                query = query.Where(x => EF.Functions.Like((x.CustomerName ?? "").ToLower(), kwPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.CustomerCode ?? "").ToLower(), kwPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.CustomerMobilePhone ?? "").ToLower(), kwPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.CustomerEmail ?? "").ToLower(), kwPattern, PortQuery.Esc));
            }

            // Lọc theo mảng customerType (Gate Mst_CustomerController.cs:37)
            if (req?.CustomerType != null && req.CustomerType.Length > 0)
            {
                var types = req.CustomerType.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToUpperInvariant()).ToList();
                if (types.Count > 0)
                {
                    query = query.Where(x => types.Contains((x.CustomerType ?? "").ToUpper()));
                }
            }

            // Lọc theo mảng customerSourceCode (Gate Mst_CustomerController.cs:38)
            if (req?.CustomerSourceCode != null && req.CustomerSourceCode.Length > 0)
            {
                var sources = req.CustomerSourceCode.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToUpperInvariant()).ToList();
                if (sources.Count > 0)
                {
                    query = query.Where(x => sources.Contains((x.CustomerSourceCode ?? "").ToUpper()));
                }
            }

            // Lọc theo mảng customerMobilePhone (Gate Mst_CustomerController.cs:40)
            if (req?.CustomerMobilePhone != null && req.CustomerMobilePhone.Length > 0)
            {
                var phones = req.CustomerMobilePhone.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
                if (phones.Count > 0)
                {
                    query = query.Where(x => phones.Contains(x.CustomerMobilePhone ?? ""));
                }
            }

            // Lọc theo mảng contactEmail (Gate Mst_CustomerController.cs:41)
            if (req?.ContactEmail != null && req.ContactEmail.Length > 0)
            {
                var emails = req.ContactEmail.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim().ToLowerInvariant()).ToList();
                if (emails.Count > 0)
                {
                    query = query.Where(x => emails.Contains((x.ContactEmail ?? "").ToLower()) || emails.Contains((x.CustomerEmail ?? "").ToLower()));
                }
            }

            // Lọc theo mảng customerGrpCode (Gate Mst_CustomerController.cs:39, Customer.cs:1126-1128)
            if (req?.CustomerGrpCode != null && req.CustomerGrpCode.Length > 0)
            {
                var grpCodes = req.CustomerGrpCode.Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim().ToUpperInvariant()).ToList();
                if (grpCodes.Count > 0)
                {
                    var matchingCusCodes = await _db.Set<Mst_CustomerInCustomerGroup>().AsNoTracking()
                        .Where(cg => grpCodes.Contains((cg.CustomerGrpCode ?? "").ToUpper()))
                        .Select(cg => cg.CustomerCodeSys)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(x => matchingCusCodes.Contains(x.CustomerCodeSys));
                }
            }

            // 3. Sắp xếp: order by mc.CreateDTimeUTC desc (Customer.cs:1136, 1250)
            query = query.OrderByDescending(x => x.CreateDTimeUTC).ThenBy(x => x.CustomerCodeSys);

            // 4. Tổng số dòng (MyCount: Customer.cs:1140)
            var itemCount = await query.CountAsync();

            // 5. Phân trang theo mẫu chuẩn LESSON A.12, A.7
            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Customer>()
                : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                             .Take((int)Math.Min(recordCount, int.MaxValue))
                             .ToListAsync();

            // 6. Tính PageCount (Mst_CustomerController.cs:180)
            var pageCount = 0;
            if (list.Count > 0 && pageSize > 0)
            {
                pageCount = (itemCount % pageSize == 0) ? (itemCount / pageSize) : (itemCount / pageSize + 1);
            }

            // 7. Nối thông tin từ các bảng liên kết (Customer.cs:1181-1250, Mst_CustomerController.cs:182-199)
            await EnrichCustomersAsync(list);

            var pageInfo = new PageInfo<Mst_Customer>(list, itemCount, pageCount, pageIndex, pageSize);

            return Ok(new
            {
                Success = true,
                Data = pageInfo
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Customer_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Tìm kiếm khách hàng theo nhiều tiêu chí.
    /// Gate: Mst_CustomerController.cs:211-323 DoSearch(...)
    /// API:  MstCustomerController.cs:92-162 WA_Mst_Customer_Get(...)
    /// BIZ:  Customer.cs:1464-1612 WAS_Mst_Customer_Get, 996-1462 Mst_Customer_Get
    /// </summary>
    [HttpPost("dosearch")]
    public async Task<IActionResult> DoSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Customer_DoSearchReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_CustomerController.cs:225-226, 273-274
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            // 2. Dựng truy vấn lọc theo StrWhereClauseMst_CustomerSearch (Mst_CustomerController.cs:325-429)
            //    và các cột của Mst_Customer (Customer.cs:1120-1137, 1344-1386)
            var query = _db.Set<Mst_Customer>().AsNoTracking();

            // Lọc theo ngữ cảnh OrgID, NetworkID, FlagActive nếu có (LESSON A.14)
            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            var networkId = StdParam(req?.NetworkID);
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            if (!string.IsNullOrEmpty(req?.FlagActive))
            {
                var flagActive = StdFlag(req?.FlagActive);
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            // Lọc theo mảng customerGrpCode (Gate Mst_CustomerController.cs:344-354, Customer.cs:1126-1128)
            if (req?.CustomerGrpCode != null && req.CustomerGrpCode.Length > 0)
            {
                var grpCodes = req.CustomerGrpCode.Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim().ToUpperInvariant()).ToList();
                if (grpCodes.Count > 0)
                {
                    var matchingCusCodes = await _db.Set<Mst_CustomerInCustomerGroup>().AsNoTracking()
                        .Where(cg => grpCodes.Contains((cg.CustomerGrpCode ?? "").ToUpper()))
                        .Select(cg => cg.CustomerCodeSys)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(x => matchingCusCodes.Contains(x.CustomerCodeSys));
                }
            }

            // Lọc theo mảng customerType (Gate Mst_CustomerController.cs:356-367)
            if (req?.CustomerType != null && req.CustomerType.Length > 0)
            {
                var types = req.CustomerType.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToUpperInvariant()).ToList();
                if (types.Count > 0)
                {
                    query = query.Where(x => types.Contains((x.CustomerType ?? "").ToUpper()));
                }
            }

            // Lọc theo mảng customerSourceCode (Gate Mst_CustomerController.cs:369-380)
            if (req?.CustomerSourceCode != null && req.CustomerSourceCode.Length > 0)
            {
                var sources = req.CustomerSourceCode.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToUpperInvariant()).ToList();
                if (sources.Count > 0)
                {
                    query = query.Where(x => sources.Contains((x.CustomerSourceCode ?? "").ToUpper()));
                }
            }

            // Lọc theo customerName (Gate Mst_CustomerController.cs:383-393: CustomerName LIKE %..% OR CustomerCode LIKE %..%)
            var customerName = StdStr(req?.CustomerName);
            if (!string.IsNullOrEmpty(customerName))
            {
                var cnPattern = PortQuery.Contains(customerName);
                query = query.Where(x => EF.Functions.Like((x.CustomerName ?? "").ToLower(), cnPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.CustomerCode ?? "").ToLower(), cnPattern, PortQuery.Esc));
            }

            // Lọc theo customerMobilePhone (Gate Mst_CustomerController.cs:395-398: CustomerMobilePhone LIKE %..%)
            var phone = StdStr(req?.CustomerMobilePhone);
            if (!string.IsNullOrEmpty(phone))
            {
                query = query.Where(x => EF.Functions.Like((x.CustomerMobilePhone ?? "").ToLower(), PortQuery.Contains(phone), PortQuery.Esc));
            }

            // Lọc theo contactEmail (Gate Mst_CustomerController.cs:399-402: ContactEmail LIKE %..%)
            var email = StdStr(req?.ContactEmail);
            if (!string.IsNullOrEmpty(email))
            {
                var emailPattern = PortQuery.Contains(email);
                query = query.Where(x => EF.Functions.Like((x.ContactEmail ?? "").ToLower(), emailPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.CustomerEmail ?? "").ToLower(), emailPattern, PortQuery.Esc));
            }

            // Lọc theo flagDealer (Gate Mst_CustomerController.cs:403-406: FlagDealer = flagDealer)
            var flagDealer = StdStr(req?.FlagDealer);
            if (!string.IsNullOrEmpty(flagDealer))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagDealer ?? "").ToLower(), PortQuery.Equal(flagDealer), PortQuery.Esc));
            }

            // Lọc theo flagSupplier (Gate Mst_CustomerController.cs:407-410: FlagSupplier = flagSupplier)
            var flagSupplier = StdStr(req?.FlagSupplier);
            if (!string.IsNullOrEmpty(flagSupplier))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagSupplier ?? "").ToLower(), PortQuery.Equal(flagSupplier), PortQuery.Esc));
            }

            // Lọc theo flagShipper (Gate Mst_CustomerController.cs:411-414: FlagShipper = flagShipper)
            var flagShipper = StdStr(req?.FlagShipper);
            if (!string.IsNullOrEmpty(flagShipper))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagShipper ?? "").ToLower(), PortQuery.Equal(flagShipper), PortQuery.Esc));
            }

            // Lọc theo flagEndUser (Gate Mst_CustomerController.cs:415-418: FlagEndUser = flagEndUser)
            var flagEndUser = StdStr(req?.FlagEndUser);
            if (!string.IsNullOrEmpty(flagEndUser))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagEndUser ?? "").ToLower(), PortQuery.Equal(flagEndUser), PortQuery.Esc));
            }

            // Lọc theo flagBank (Gate Mst_CustomerController.cs:419-422: FlagBank = flagBank)
            var flagBank = StdStr(req?.FlagBank);
            if (!string.IsNullOrEmpty(flagBank))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagBank ?? "").ToLower(), PortQuery.Equal(flagBank), PortQuery.Esc));
            }

            // Lọc theo flagInsurrance (Gate Mst_CustomerController.cs:423-426: FlagInsurrance = flagInsurrance)
            var flagInsurrance = StdStr(req?.FlagInsurrance);
            if (!string.IsNullOrEmpty(flagInsurrance))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagInsurrance ?? "").ToLower(), PortQuery.Equal(flagInsurrance), PortQuery.Esc));
            }

            // 3. Sắp xếp: order by mc.CreateDTimeUTC desc (Customer.cs:1136, 1250)
            query = query.OrderByDescending(x => x.CreateDTimeUTC).ThenBy(x => x.CustomerCodeSys);

            // 4. Tổng số dòng (MyCount: Customer.cs:1140)
            var itemCount = await query.CountAsync();

            // 5. Phân trang theo mẫu chuẩn LESSON A.12, A.7
            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Customer>()
                : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                             .Take((int)Math.Min(recordCount, int.MaxValue))
                             .ToListAsync();

            // 6. Tính PageCount (Mst_CustomerController.cs:307)
            var pageCount = 0;
            if (list.Count > 0 && pageSize > 0)
            {
                pageCount = (itemCount % pageSize == 0) ? (itemCount / pageSize) : (itemCount / pageSize + 1);
            }

            // 7. Nối thông tin từ các bảng liên kết (Customer.cs:1181-1250, Mst_CustomerController.cs:287-301)
            await EnrichCustomersAsync(list);

            var pageInfo = new PageInfo<Mst_Customer>(list, itemCount, pageCount, pageIndex, pageSize);

            return Ok(new
            {
                Success = true,
                Data = pageInfo
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Customer_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xuất danh sách khách hàng ra file Excel.
    /// Gate: Mst_CustomerController.cs:435-561 Export(...)
    /// API:  MstCustomerController.cs:92-162 WA_Mst_Customer_Get(...)
    /// BIZ:  Customer.cs:1464-1612 WAS_Mst_Customer_Get, 996-1462 Mst_Customer_Get
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Customer_ExportReq? req)
    {
        try
        {
            // 1. Phân trang: Gate Mst_CustomerController.cs:448-449, 482-483 (BaseController.Ft_RecordCount = 123456000)
            long recordStart = req?.Ft_RecordStart ?? ((long)(req?.Page ?? req?.PageIndex ?? 0) * (req?.RecordCount ?? req?.PageSize ?? 0));
            long recordCount = req?.Ft_RecordCount ?? req?.RecordCount ?? req?.PageSize ?? int.MaxValue;

            // 2. Dựng truy vấn lọc theo StrWhereClauseMst_CustomerSearch (Mst_CustomerController.cs:325-429, 453-467)
            //    và các cột của Mst_Customer (Customer.cs:1120-1137, 1344-1386)
            var query = _db.Set<Mst_Customer>().AsNoTracking();

            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            var networkId = StdParam(req?.NetworkID);
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            if (!string.IsNullOrEmpty(req?.FlagActive))
            {
                var flagActive = StdFlag(req?.FlagActive);
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            // Lọc theo mảng customerGrpCode (Gate Mst_CustomerController.cs:344-354, Customer.cs:1126-1128)
            if (req?.CustomerGrpCode != null && req.CustomerGrpCode.Length > 0)
            {
                var grpCodes = req.CustomerGrpCode.Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim().ToUpperInvariant()).ToList();
                if (grpCodes.Count > 0)
                {
                    var matchingCusCodes = await _db.Set<Mst_CustomerInCustomerGroup>().AsNoTracking()
                        .Where(cg => grpCodes.Contains((cg.CustomerGrpCode ?? "").ToUpper()))
                        .Select(cg => cg.CustomerCodeSys)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(x => matchingCusCodes.Contains(x.CustomerCodeSys));
                }
            }

            // Lọc theo mảng customerType (Gate Mst_CustomerController.cs:356-367)
            if (req?.CustomerType != null && req.CustomerType.Length > 0)
            {
                var types = req.CustomerType.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToUpperInvariant()).ToList();
                if (types.Count > 0)
                {
                    query = query.Where(x => types.Contains((x.CustomerType ?? "").ToUpper()));
                }
            }

            // Lọc theo mảng customerSourceCode (Gate Mst_CustomerController.cs:369-380)
            if (req?.CustomerSourceCode != null && req.CustomerSourceCode.Length > 0)
            {
                var sources = req.CustomerSourceCode.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToUpperInvariant()).ToList();
                if (sources.Count > 0)
                {
                    query = query.Where(x => sources.Contains((x.CustomerSourceCode ?? "").ToUpper()));
                }
            }

            // Lọc theo customerName (Gate Mst_CustomerController.cs:383-393: CustomerName LIKE %..% OR CustomerCode LIKE %..%)
            var customerName = StdStr(req?.CustomerName);
            if (!string.IsNullOrEmpty(customerName))
            {
                var cnPattern = PortQuery.Contains(customerName);
                query = query.Where(x => EF.Functions.Like((x.CustomerName ?? "").ToLower(), cnPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.CustomerCode ?? "").ToLower(), cnPattern, PortQuery.Esc));
            }

            // Lọc theo customerMobilePhone (Gate Mst_CustomerController.cs:395-398: CustomerMobilePhone LIKE %..%)
            var phone = StdStr(req?.CustomerMobilePhone);
            if (!string.IsNullOrEmpty(phone))
            {
                query = query.Where(x => EF.Functions.Like((x.CustomerMobilePhone ?? "").ToLower(), PortQuery.Contains(phone), PortQuery.Esc));
            }

            // Lọc theo contactEmail (Gate Mst_CustomerController.cs:399-402: ContactEmail LIKE %..%)
            var email = StdStr(req?.ContactEmail);
            if (!string.IsNullOrEmpty(email))
            {
                var emailPattern = PortQuery.Contains(email);
                query = query.Where(x => EF.Functions.Like((x.ContactEmail ?? "").ToLower(), emailPattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.CustomerEmail ?? "").ToLower(), emailPattern, PortQuery.Esc));
            }

            // Lọc theo flagDealer (Gate Mst_CustomerController.cs:403-406: FlagDealer = flagDealer)
            var flagDealer = StdStr(req?.FlagDealer);
            if (!string.IsNullOrEmpty(flagDealer))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagDealer ?? "").ToLower(), PortQuery.Equal(flagDealer), PortQuery.Esc));
            }

            // Lọc theo flagSupplier (Gate Mst_CustomerController.cs:407-410: FlagSupplier = flagSupplier)
            var flagSupplier = StdStr(req?.FlagSupplier);
            if (!string.IsNullOrEmpty(flagSupplier))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagSupplier ?? "").ToLower(), PortQuery.Equal(flagSupplier), PortQuery.Esc));
            }

            // Lọc theo flagShipper (Gate Mst_CustomerController.cs:411-414: FlagShipper = flagShipper)
            var flagShipper = StdStr(req?.FlagShipper);
            if (!string.IsNullOrEmpty(flagShipper))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagShipper ?? "").ToLower(), PortQuery.Equal(flagShipper), PortQuery.Esc));
            }

            // Lọc theo flagEndUser (Gate Mst_CustomerController.cs:415-418: FlagEndUser = flagEndUser)
            var flagEndUser = StdStr(req?.FlagEndUser);
            if (!string.IsNullOrEmpty(flagEndUser))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagEndUser ?? "").ToLower(), PortQuery.Equal(flagEndUser), PortQuery.Esc));
            }

            // Lọc theo flagBank (Gate Mst_CustomerController.cs:419-422: FlagBank = flagBank)
            var flagBank = StdStr(req?.FlagBank);
            if (!string.IsNullOrEmpty(flagBank))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagBank ?? "").ToLower(), PortQuery.Equal(flagBank), PortQuery.Esc));
            }

            // Lọc theo flagInsurrance (Gate Mst_CustomerController.cs:423-426: FlagInsurrance = flagInsurrance)
            var flagInsurrance = StdStr(req?.FlagInsurrance);
            if (!string.IsNullOrEmpty(flagInsurrance))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagInsurrance ?? "").ToLower(), PortQuery.Equal(flagInsurrance), PortQuery.Esc));
            }

            // 3. Sắp xếp: order by mc.CreateDTimeUTC desc (Customer.cs:1136, 1250)
            query = query.OrderByDescending(x => x.CreateDTimeUTC).ThenBy(x => x.CustomerCodeSys);

            // 4. Phân trang (Customer.cs:1143-1151, LESSON A.12)
            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Customer>()
                : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                             .Take((int)Math.Min(recordCount, int.MaxValue))
                             .ToListAsync();

            // 5. Nối thông tin từ các bảng liên kết (Customer.cs:1181-1250, Mst_CustomerController.cs:182-199)
            await EnrichCustomersAsync(list);

            // 6. Xuất Excel và trả URL (Mst_CustomerController.cs:555-560)
            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_Customer));
            PortExcel.ExportToExcelFromList(list, DicColNames, filePath, nameof(Mst_Customer));

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
            return PortResult.Fail("Mst_Customer_Export", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xuất file Excel mẫu để import khách hàng.
    /// Gate: Mst_CustomerController.cs:567-578 ExportTemplate()
    /// </summary>
    [HttpPost("exporttemplate")]
    public async Task<IActionResult> ExportTemplate([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Customer_ExportTemplateReq? req = null)
    {
        try
        {
            var list = new List<Mst_Customer>();
            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_Customer));
            PortExcel.ExportToExcelFromList(list, DicColNames, filePath, nameof(Mst_Customer));

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
            return PortResult.Fail("Mst_Customer_ExportTemplate", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Nhập dữ liệu khách hàng từ file Excel.
    /// Gate: Mst_CustomerController.cs:587-1318 Import(HttpPostedFileWrapper file)
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        try
        {
            if (file == null && Request?.HasFormContentType == true && Request.Form.Files.Count > 0)
            {
                file = Request.Form.Files[0];
            }

            if (file == null || file.Length == 0)
            {
                return PortResult.Fail("Mst_Customer_Import", "Message", "File excel import không hợp lệ!");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                return PortResult.Fail("Mst_Customer_Import", "Message", "File excel import không hợp lệ!");
            }

            var keysInOrder = DicColNames.Keys.ToList();
            List<Dictionary<string, string?>> rows;
            int columnCount;

            using (var stream = file.OpenReadStream())
            {
                var (cols, readRows) = PortExcel.ReadSheet(stream, keysInOrder, firstDataRow: 2);
                columnCount = cols;
                rows = readRows;
            }

            if (rows.Count == 0)
            {
                return PortResult.Fail("Mst_Customer_Import", "Message", "File excel import không có dữ liệu!");
            }

            if (columnCount != 39)
            {
                return PortResult.Fail("Mst_Customer_Import", "Message", "File excel import không hợp lệ!");
            }

            var networkId = Request?.Headers["NetworkID"].FirstOrDefault() ?? _config?["Biz_NetworkID"] ?? "1001";
            var orgId = Request?.Headers["OrgID"].FirstOrDefault() ?? _config?["Biz_OrgID"] ?? "7200000001";
            var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

            // 1. Tải dữ liệu hiện tại để kiểm tra
            // Khách hàng hiện tại (Mst_CustomerController.cs:614-645)
            var listCustomer_Cur = await _db.Set<Mst_Customer>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc))
                .ToListAsync();

            // Danh sách loại giấy tờ (Mst_CustomerController.cs:684-716)
            var listMst_GovIDType = await _db.Set<Mst_GovIDType>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .ToListAsync();

            // Nguồn và nhóm khách hàng (Mst_CustomerController.cs:731-773)
            var list_Mst_CustomerSource = await _db.Set<Mst_CustomerSource>()
                .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc))
                .ToListAsync();

            var list_Mst_CustomerGroup = await _db.Set<Mst_CustomerGroup>()
                .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc))
                .ToListAsync();

            // 2. Vòng lặp duyệt dữ liệu và validate từng dòng (Mst_CustomerController.cs:807-1173)
            var idx = 0;
            var newCustomerGroups = new List<Mst_CustomerGroup>();
            var newCustomerSources = new List<Mst_CustomerSource>();

            foreach (var dr in rows)
            {
                dr["FlagCustomerAvatarPath"] = "1";

                #region ["Check null"]
                var customerCode = dr.GetValueOrDefault("CustomerCode");
                if (string.IsNullOrWhiteSpace(customerCode))
                {
                    return PortResult.Fail("Mst_Customer_Import", "Message", "Mã khách hàng không được trống!");
                }
                var customerName = dr.GetValueOrDefault("CustomerName");
                if (string.IsNullOrWhiteSpace(customerName))
                {
                    return PortResult.Fail("Mst_Customer_Import", "Message", "Tên khách hàng không được trống!");
                }
                #endregion

                #region ["Check duplicate"]
                for (var i = 0; i < rows.Count; i++)
                {
                    var customerCur = (rows[i].GetValueOrDefault("CustomerCode") ?? "").Trim();
                    for (var j = 0; j < rows.Count; j++)
                    {
                        if (i != j)
                        {
                            var _customerCur = (rows[j].GetValueOrDefault("CustomerCode") ?? "").Trim();
                            if (customerCur.Equals(_customerCur, StringComparison.OrdinalIgnoreCase))
                            {
                                var exitsData = "Mã khách hàng '" + customerCur + "' bị lặp trong file excel!";
                                return PortResult.Fail("Mst_Customer_Import", "Message", exitsData);
                            }
                        }
                    }
                }
                #endregion

                var iRows = 2;
                iRows = (iRows + idx + 1);
                var strRows = " - Dòng " + iRows;
                idx++;

                var customerCodeCur = (dr.GetValueOrDefault("CustomerCode") ?? "").Trim();

                #region ["Check loại KH"]
                var cusTypeName = dr.GetValueOrDefault("mct_CustomerTypeName");
                if (!string.IsNullOrWhiteSpace(cusTypeName))
                {
                    var cusTypeLower = cusTypeName.Trim().ToLowerInvariant();
                    if (cusTypeLower != "cá nhân" && cusTypeLower != "tổ chức")
                    {
                        var exitsData = "Loại khách hàng của mã khách " + customerCodeCur + " chỉ có thể nhập 'Cá nhân' hoặc 'Tổ chức'" + strRows + "!";
                        return PortResult.Fail("Mst_Customer_Import", "Message", exitsData);
                    }
                    else
                    {
                        dr["CustomerType"] = cusTypeLower == "cá nhân" ? "CANHAN" : "TOCHUC";
                    }
                }
                #endregion

                #region ["Check Giới tính"]
                var cusGender = dr.GetValueOrDefault("CustomerGender");
                if (!string.IsNullOrWhiteSpace(cusGender))
                {
                    var cusGenderLower = cusGender.Trim().ToLowerInvariant();
                    if (cusGenderLower != "nữ" && cusGenderLower != "nam" && cusGenderLower != "khác")
                    {
                        var exitsData = "Giới tính của mã khách " + customerCodeCur + " chỉ có thể nhập 'Nữ', 'Nam' hoặc 'Khác'" + strRows + "!";
                        return PortResult.Fail("Mst_Customer_Import", "Message", exitsData);
                    }
                    else
                    {
                        if (cusGenderLower == "nữ")
                        {
                            dr["CustomerGender"] = "FEMALE";
                        }
                        else if (cusGenderLower == "nam")
                        {
                            dr["CustomerGender"] = "MALE";
                        }
                        else
                        {
                            dr["CustomerGender"] = "OTHER";
                        }
                    }
                }
                #endregion

                #region ["Check Loại giấy tờ"]
                var govIDType = dr.GetValueOrDefault("GovIDType");
                if (!string.IsNullOrWhiteSpace(govIDType))
                {
                    var existGovIDType = listMst_GovIDType.FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.GovIDTypeName) &&
                        x.GovIDTypeName.Trim().Equals(govIDType.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (existGovIDType == null)
                    {
                        var exitsData = "Loại giấy tờ của mã khách " + customerCodeCur + " không có trong hệ thống " + strRows + "!";
                        return PortResult.Fail("Mst_Customer_Import", "Message", exitsData);
                    }
                    else
                    {
                        dr["GovIDType"] = existGovIDType.GovIDType;
                    }
                }
                #endregion

                #region ["Dynamic Field"]
                var listOfCustDynamicFieldValue = dr.GetValueOrDefault("ListOfCustDynamicFieldValue");
                if (!string.IsNullOrWhiteSpace(listOfCustDynamicFieldValue))
                {
                    var listCustomer_DynamicParamTypesExcel = new List<CustomerDynamicFieldParamType>();
                    var arrDynamicField = listOfCustDynamicFieldValue.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (arrDynamicField != null && arrDynamicField.Length > 0)
                    {
                        foreach (var subArr in arrDynamicField)
                        {
                            var itemArr = subArr.Split(':');
                            if (itemArr != null && itemArr.Length == 2 && !string.IsNullOrWhiteSpace(itemArr[0]))
                            {
                                listCustomer_DynamicParamTypesExcel.Add(new CustomerDynamicFieldParamType
                                {
                                    Title = itemArr[0].Trim(),
                                    DefaultValue = itemArr[1].Trim(),
                                });
                            }
                            else
                            {
                                var exitsData = "Trường động mã khách hàng'" + customerCodeCur + "' không hợp lệ" + strRows;
                                return PortResult.Fail("Mst_Customer_Import", "Message", exitsData);
                            }
                        }
                    }

                    if (listCustomer_DynamicParamTypesExcel.Count > 0)
                    {
                        var listArr = new List<string>();
                        foreach (var item in listCustomer_DynamicParamTypesExcel)
                        {
                            var itemCode = DateTime.UtcNow.ToString("yyyy.MM.dd.HH.mm.ss.fff");
                            var itemDynamicField = $"\"{itemCode}\":\"{item.DefaultValue}\"";
                            listArr.Add(itemDynamicField);
                        }
                        dr["ListOfCustDynamicFieldValue"] = "{" + string.Join(",", listArr) + "}";
                    }
                }
                #endregion

                #region ["Thêm nhóm và nguồn KH nếu chưa tồn tại"]
                var cusGrpName = dr.GetValueOrDefault("mcg_CustomerGrpName");
                if (!string.IsNullOrWhiteSpace(cusGrpName))
                {
                    var lstCusGrpName = cusGrpName.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var lstCusGrpCode = new List<string>();
                    foreach (var grpNameRaw in lstCusGrpName)
                    {
                        var grpName = grpNameRaw.Trim();
                        var existGrpName = list_Mst_CustomerGroup.FirstOrDefault(grp =>
                            grp.CustomerGrpName != null && grp.CustomerGrpName.Equals(grpName, StringComparison.OrdinalIgnoreCase));

                        if (existGrpName == null)
                        {
                            var newGrpCode = $"GRP_{Guid.NewGuid():N}"[..10].ToUpperInvariant();
                            var objMst_CustomerGroup = new Mst_CustomerGroup
                            {
                                OrgID = orgId,
                                CustomerGrpCode = newGrpCode,
                                CustomerGrpName = cusGrpName, // Lỗi nguồn giữ nguyên (Mst_CustomerController.cs:1053)
                                CustomerGrpDesc = "Create by excel",
                                FlagActive = "1",
                                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                                LogLUBy = userCode
                            };
                            lstCusGrpCode.Add(objMst_CustomerGroup.CustomerGrpCode);
                            list_Mst_CustomerGroup.Add(objMst_CustomerGroup);
                            newCustomerGroups.Add(objMst_CustomerGroup);
                        }
                        else
                        {
                            lstCusGrpCode.Add(existGrpName.CustomerGrpCode);
                        }
                    }
                    dr["CustomerGrpCode"] = string.Join(",", lstCusGrpCode);
                }

                var cusSrcName = dr.GetValueOrDefault("mcs_CustomerSourceName");
                if (!string.IsNullOrWhiteSpace(cusSrcName))
                {
                    var existSrcName = list_Mst_CustomerSource.FirstOrDefault(src =>
                        src.CustomerSourceName != null && src.CustomerSourceName.Equals(cusSrcName.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (existSrcName == null)
                    {
                        var newSrcCode = $"SRC_{Guid.NewGuid():N}"[..10].ToUpperInvariant();
                        var objMst_CustomerSource = new Mst_CustomerSource
                        {
                            OrgID = orgId,
                            CustomerSourceCode = newSrcCode,
                            CustomerSourceName = cusSrcName.Trim(),
                            CustomerSourceDesc = "Create by excel",
                            FlagActive = "1",
                            LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                            LogLUBy = userCode
                        };
                        list_Mst_CustomerSource.Add(objMst_CustomerSource);
                        newCustomerSources.Add(objMst_CustomerSource);
                        dr["CustomerSourceCode"] = objMst_CustomerSource.CustomerSourceCode;
                    }
                    else
                    {
                        dr["CustomerSourceCode"] = existSrcName.CustomerSourceCode;
                    }
                }
                #endregion

                #region ["Check trùng số ĐT & email & MST"]
                var cusMobilePhone = dr.GetValueOrDefault("CustomerMobilePhone");
                if (!string.IsNullOrWhiteSpace(cusMobilePhone))
                {
                    var existCusPhoneNo = listCustomer_Cur.FirstOrDefault(cus =>
                        !string.IsNullOrWhiteSpace(cus.CustomerMobilePhone) &&
                        cus.CustomerMobilePhone.Trim().Equals(cusMobilePhone.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (existCusPhoneNo != null)
                    {
                        var exitsData = "Số điện thoại " + cusMobilePhone + strRows + " đã tồn tại trong hệ thống'";
                        return PortResult.Fail("Mst_Customer_Import", "Message", exitsData);
                    }
                }

                var cusEmail = dr.GetValueOrDefault("CustomerEmail");
                if (!string.IsNullOrWhiteSpace(cusEmail))
                {
                    var existCusEmail = listCustomer_Cur.FirstOrDefault(cus =>
                        !string.IsNullOrWhiteSpace(cus.CustomerEmail) &&
                        cus.CustomerEmail.Trim().Equals(cusEmail.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (existCusEmail != null)
                    {
                        var exitsData = "Email " + cusEmail + strRows + " đã tồn tại trong hệ thống'";
                        return PortResult.Fail("Mst_Customer_Import", "Message", exitsData);
                    }
                }

                var cusMST = dr.GetValueOrDefault("MST");
                if (!string.IsNullOrWhiteSpace(cusMST))
                {
                    var existCusMST = listCustomer_Cur.FirstOrDefault(cus =>
                        !string.IsNullOrWhiteSpace(cus.MST) &&
                        cus.MST.Trim().Equals(cusMST.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (existCusMST != null)
                    {
                        var exitsData = "Mã số thuế " + cusMST + strRows + " đã tồn tại trong hệ thống'";
                        return PortResult.Fail("Mst_Customer_Import", "Message", exitsData);
                    }
                }
                #endregion
            }

            // 3. Chuẩn bị entities lưu xuống DB (Mst_CustomerController.cs:1175-1229)
            var customersToSave = new List<Mst_Customer>();
            var customerGroupsToSave = new List<Mst_CustomerInCustomerGroup>();
            var dtimeNowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            foreach (var dr in rows)
            {
                var customerCode = (dr.GetValueOrDefault("CustomerCode") ?? "").Trim();
                var customerCodeSys = $"CUS{Guid.NewGuid():N}"[..12].ToUpperInvariant();
                var grpCodeStr = dr.GetValueOrDefault("CustomerGrpCode") ?? "";

                var cus = new Mst_Customer
                {
                    OrgID = orgId,
                    CustomerCodeSys = customerCodeSys,
                    NetworkID = networkId,
                    CustomerCode = customerCode,
                    CustomerName = dr.GetValueOrDefault("CustomerName")?.Trim(),
                    CustomerType = dr.GetValueOrDefault("CustomerType"),
                    CustomerSourceCode = dr.GetValueOrDefault("CustomerSourceCode"),
                    CustomerPhoneNo = dr.GetValueOrDefault("CustomerPhoneNo"),
                    CustomerMobilePhone = dr.GetValueOrDefault("CustomerMobilePhone"),
                    CustomerAvatarPath = dr.GetValueOrDefault("CustomerAvatarPath"),
                    FlagCustomerAvatarPath = "1",
                    CustomerGender = dr.GetValueOrDefault("CustomerGender"),
                    CustomerEmail = dr.GetValueOrDefault("CustomerEmail"),
                    CustomerAddress = dr.GetValueOrDefault("CustomerAddress"),
                    ProvinceCode = dr.GetValueOrDefault("ProvinceCode"),
                    DistrictCode = dr.GetValueOrDefault("DistrictCode"),
                    WardCode = dr.GetValueOrDefault("WardCode"),
                    AreaCode = dr.GetValueOrDefault("AreaCode"),
                    FlagDealer = dr.GetValueOrDefault("FlagDealer"),
                    FlagSupplier = dr.GetValueOrDefault("FlagSupplier"),
                    FlagShipper = dr.GetValueOrDefault("FlagShipper"),
                    FlagEndUser = dr.GetValueOrDefault("FlagEndUser"),
                    FlagBank = dr.GetValueOrDefault("FlagBank"),
                    FlagInsurrance = dr.GetValueOrDefault("FlagInsurrance"),
                    RepresentName = dr.GetValueOrDefault("RepresentName"),
                    RepresentPosition = dr.GetValueOrDefault("RepresentPosition"),
                    GovIDCardNo = dr.GetValueOrDefault("GovIDCardNo"),
                    GovIDType = dr.GetValueOrDefault("GovIDType"),
                    BankAccountNo = dr.GetValueOrDefault("BankAccountNo"),
                    BankName = dr.GetValueOrDefault("BankName"),
                    ContactName = dr.GetValueOrDefault("ContactName"),
                    ContactPhone = dr.GetValueOrDefault("ContactPhone"),
                    ContactEmail = dr.GetValueOrDefault("ContactEmail"),
                    Fax = dr.GetValueOrDefault("Fax"),
                    CustomerDateOfBirth = dr.GetValueOrDefault("CustomerDateOfBirth"),
                    Facebook = dr.GetValueOrDefault("Facebook"),
                    InvoiceCustomerName = dr.GetValueOrDefault("InvoiceCustomerName"),
                    InvoiceOrgName = dr.GetValueOrDefault("InvoiceOrgName"),
                    MST = dr.GetValueOrDefault("MST"),
                    InvoiceCustomerAddress = dr.GetValueOrDefault("InvoiceCustomerAddress"),
                    InvoiceEmailSend = dr.GetValueOrDefault("InvoiceEmailSend"),
                    ListOfCustDynamicFieldValue = dr.GetValueOrDefault("ListOfCustDynamicFieldValue"),
                    CustomerGrpCode = "", // Gate line 1225: customer.CustomerGrpCode = "" (SQL truncate avoid)
                    FlagActive = "1",
                    CreateDTimeUTC = dtimeNowStr,
                    CreateBy = userCode,
                    LUDTimeUTC = dtimeNowStr,
                    LUBy = userCode,
                    LogLUDTimeUTC = dtimeNowStr,
                    LogLUBy = userCode
                };

                if (!string.IsNullOrEmpty(grpCodeStr))
                {
                    var grpCodes = grpCodeStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var gc in grpCodes)
                    {
                        customerGroupsToSave.Add(new Mst_CustomerInCustomerGroup
                        {
                            OrgID = orgId,
                            CustomerCodeSys = customerCodeSys,
                            CustomerGrpCode = gc.Trim(),
                            LogLUDTimeUTC = dtimeNowStr,
                            LogLUBy = userCode
                        });
                    }
                }

                customersToSave.Add(cus);
            }

            // 4. Lưu dữ liệu trong Transaction (Customer.cs:1619-1732, 2359-2966)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (newCustomerGroups.Count > 0)
                {
                    _db.Set<Mst_CustomerGroup>().AddRange(newCustomerGroups);
                }
                if (newCustomerSources.Count > 0)
                {
                    _db.Set<Mst_CustomerSource>().AddRange(newCustomerSources);
                }
                if (newCustomerGroups.Count > 0 || newCustomerSources.Count > 0)
                {
                    await _db.SaveChangesAsync();
                }

                _db.Set<Mst_Customer>().AddRange(customersToSave);
                if (customerGroupsToSave.Count > 0)
                {
                    _db.Set<Mst_CustomerInCustomerGroup>().AddRange(customerGroupsToSave);
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return Ok(new
            {
                Success = true,
                Messages = new[] { "Đã nhập dữ liệu excel thành công!" },
                Title = "Đã nhập dữ liệu excel thành công!",
                RedirectUrl = "/Mst_Customer/Index"
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Customer_Import", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xóa khách hàng.
    /// Gate: Mst_CustomerController.cs:1372-1461 Delete(string listCustomers)
    /// API:  MstCustomerController.cs:19-89 WA_Mst_Customer_Save
    /// BIZ:  Customer.cs:2968-3070 WAS_Mst_Customer_Save -> 1619-1732 Mst_Customer_Save -> 2359-2966 Mst_Customer_SaveX_New20231205
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Customer_DeleteReq? req)
    {
        try
        {
            var customerList = new List<Mst_Customer_DeleteItem>();

            if (req != null)
            {
                if (req.Lst_Mst_Customer != null && req.Lst_Mst_Customer.Count > 0)
                {
                    customerList.AddRange(req.Lst_Mst_Customer);
                }

                if (req.ListCustomers != null)
                {
                    if (req.ListCustomers is JsonElement elem)
                    {
                        if (elem.ValueKind == JsonValueKind.String)
                        {
                            var str = elem.GetString();
                            if (!string.IsNullOrWhiteSpace(str))
                            {
                                try
                                {
                                    var parsed = JsonSerializer.Deserialize<List<Mst_Customer_DeleteItem>>(str, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    if (parsed != null && parsed.Count > 0)
                                    {
                                        customerList.AddRange(parsed);
                                    }
                                }
                                catch
                                {
                                    try
                                    {
                                        var single = JsonSerializer.Deserialize<Mst_Customer_DeleteItem>(str, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        if (single != null && (!string.IsNullOrEmpty(single.CustomerCodeSys) || !string.IsNullOrEmpty(single.CustomerCode)))
                                        {
                                            customerList.Add(single);
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        else if (elem.ValueKind == JsonValueKind.Array)
                        {
                            try
                            {
                                var parsed = JsonSerializer.Deserialize<List<Mst_Customer_DeleteItem>>(elem.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (parsed != null && parsed.Count > 0)
                                {
                                    customerList.AddRange(parsed);
                                }
                            }
                            catch { }
                        }
                        else if (elem.ValueKind == JsonValueKind.Object)
                        {
                            try
                            {
                                var single = JsonSerializer.Deserialize<Mst_Customer_DeleteItem>(elem.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (single != null && (!string.IsNullOrEmpty(single.CustomerCodeSys) || !string.IsNullOrEmpty(single.CustomerCode)))
                                {
                                    customerList.Add(single);
                                }
                            }
                            catch { }
                        }
                    }
                    else if (req.ListCustomers is string str && !string.IsNullOrWhiteSpace(str))
                    {
                        try
                        {
                            var parsed = JsonSerializer.Deserialize<List<Mst_Customer_DeleteItem>>(str, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (parsed != null && parsed.Count > 0)
                            {
                                customerList.AddRange(parsed);
                            }
                        }
                        catch
                        {
                            try
                            {
                                var single = JsonSerializer.Deserialize<Mst_Customer_DeleteItem>(str, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (single != null && (!string.IsNullOrEmpty(single.CustomerCodeSys) || !string.IsNullOrEmpty(single.CustomerCode)))
                                {
                                    customerList.Add(single);
                                }
                            }
                            catch { }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(req.CustomerCodeSys) || !string.IsNullOrEmpty(req.CustomerCode))
                {
                    customerList.Add(new Mst_Customer_DeleteItem
                    {
                        CustomerCodeSys = req.CustomerCodeSys,
                        CustomerCode = req.CustomerCode,
                        OrgID = req.OrgID,
                        NetworkID = req.NetworkID
                    });
                }
            }

            // 1. Kiểm tra danh sách khách hàng đầu vào (Customer.cs:2415-2425: Mst_Customer_Save_Input_Mst_CustomerTblNotFound)
            if (customerList.Count == 0)
            {
                return PortResult.Fail("Mst_Customer_Save_Input_Mst_CustomerTblNotFound", "Check.TableName", "Mst_Customer");
            }

            var defaultOrgId = Request?.Headers["OrgID"].FirstOrDefault() ?? _config?["Biz_OrgID"] ?? "7200000001";
            var defaultNetworkId = Request?.Headers["NetworkID"].FirstOrDefault() ?? _config?["Biz_NetworkID"] ?? "1001";

            // 2. Tra cứu và chuẩn bị danh sách bản ghi cần xóa từ Mst_Customer và Mst_CustomerInCustomerGroup
            var allMatchingCustomers = new List<Mst_Customer>();
            var allMatchingGroups = new List<Mst_CustomerInCustomerGroup>();

            foreach (var item in customerList)
            {
                var cusCodeSys = StdParam(item.CustomerCodeSys);
                var cusCode = StdParam(item.CustomerCode);
                var orgId = StdParam(item.OrgID) ?? defaultOrgId;

                if (string.IsNullOrEmpty(cusCodeSys) && !string.IsNullOrEmpty(cusCode))
                {
                    var found = await _db.Set<Mst_Customer>()
                        .FirstOrDefaultAsync(x => EF.Functions.Like((x.CustomerCode ?? "").ToLower(), PortQuery.Equal(cusCode), PortQuery.Esc)
                                               && EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
                    if (found != null)
                    {
                        cusCodeSys = found.CustomerCodeSys;
                    }
                }

                if (!string.IsNullOrEmpty(cusCodeSys))
                {
                    var matchingCus = await _db.Set<Mst_Customer>()
                        .Where(x => EF.Functions.Like((x.CustomerCodeSys ?? "").ToLower(), PortQuery.Equal(cusCodeSys), PortQuery.Esc)
                                 && EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc))
                        .ToListAsync();

                    foreach (var c in matchingCus)
                    {
                        if (!allMatchingCustomers.Any(x => x.OrgID == c.OrgID && x.CustomerCodeSys == c.CustomerCodeSys))
                        {
                            allMatchingCustomers.Add(c);
                        }
                    }

                    var matchingGrp = await _db.Set<Mst_CustomerInCustomerGroup>()
                        .Where(x => EF.Functions.Like((x.CustomerCodeSys ?? "").ToLower(), PortQuery.Equal(cusCodeSys), PortQuery.Esc)
                                 && EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc))
                        .ToListAsync();

                    foreach (var g in matchingGrp)
                    {
                        if (!allMatchingGroups.Any(x => x.OrgID == g.OrgID && x.CustomerGrpCode == g.CustomerGrpCode && x.CustomerCodeSys == g.CustomerCodeSys))
                        {
                            allMatchingGroups.Add(g);
                        }
                    }
                }
            }

            // 3. Mở Transaction và thực hiện xóa bản ghi (Customer.cs:1650, 2653-2690)
            //    (UserOwner_Customer và Mst_CustomerInArea chưa có entity trong MiniContract -> ghi GAP)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (allMatchingGroups.Count > 0)
                {
                    _db.Set<Mst_CustomerInCustomerGroup>().RemoveRange(allMatchingGroups);
                }

                if (allMatchingCustomers.Count > 0)
                {
                    _db.Set<Mst_Customer>().RemoveRange(allMatchingCustomers);
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // 4. Trả kết quả thành công khớp gate MVC (Mst_CustomerController.cs:1451-1453)
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Xóa khách hàng thành công!" },
                    Title = "Xóa khách hàng thành công!",
                    RedirectUrl = "/Mst_Customer/Index"
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Customer_Save", "Exception", ex.Message);
            }
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Customer_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Tạo mới khách hàng.
    /// Gate: Mst_CustomerController.cs:1466-1799 (Create GET), 1801-2038 (Create POST)
    /// API:  MstCustomerController.cs:19-89 WA_Mst_Customer_Save
    /// BIZ:  Customer.cs:2968-3070 WAS_Mst_Customer_Save -> 1619-1732 Mst_Customer_Save -> 2359-2966 Mst_Customer_SaveX_New20231205
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Customer_CreateReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc thiếu dữ liệu (LESSON A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_Customer_Create", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_CustomerController.cs:1803, 1808)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_Customer_CreateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.OrgID ??= modelObj.OrgID;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.CustomerCode ??= modelObj.CustomerCode;
                    req.CustomerCodeSys ??= modelObj.CustomerCodeSys;
                    req.CustomerType ??= modelObj.CustomerType;
                    req.CustomerGrpCode ??= modelObj.CustomerGrpCode;
                    req.CustomerSourceCode ??= modelObj.CustomerSourceCode;
                    req.CustomerSourceNameActual ??= modelObj.CustomerSourceNameActual;
                    req.CustomerName ??= modelObj.CustomerName;
                    req.CustomerNameEN ??= modelObj.CustomerNameEN;
                    req.CustomerGender ??= modelObj.CustomerGender;
                    req.CustomerPhoneNo ??= modelObj.CustomerPhoneNo;
                    req.CustomerMobilePhone ??= modelObj.CustomerMobilePhone;
                    req.ProvinceCode ??= modelObj.ProvinceCode;
                    req.DistrictCode ??= modelObj.DistrictCode;
                    req.WardCode ??= modelObj.WardCode;
                    req.AreaCode ??= modelObj.AreaCode;
                    req.CustomerAvatarName ??= modelObj.CustomerAvatarName;
                    req.CustomerAvatarSpec ??= modelObj.CustomerAvatarSpec;
                    req.FlagCustomerAvatarPath ??= modelObj.FlagCustomerAvatarPath;
                    req.CustomerAvatarPath ??= modelObj.CustomerAvatarPath;
                    req.CustomerAddress ??= modelObj.CustomerAddress;
                    req.CustomerEmail ??= modelObj.CustomerEmail;
                    req.CustomerDateOfBirth ??= modelObj.CustomerDateOfBirth;
                    req.GovIDType ??= modelObj.GovIDType;
                    req.GovIDCardNo ??= modelObj.GovIDCardNo;
                    req.GovIDCardDate ??= modelObj.GovIDCardDate;
                    req.GovIDCardPlace ??= modelObj.GovIDCardPlace;
                    req.TaxCode ??= modelObj.TaxCode;
                    req.BankCode ??= modelObj.BankCode;
                    req.BankName ??= modelObj.BankName;
                    req.BankAccountNo ??= modelObj.BankAccountNo;
                    req.RepresentName ??= modelObj.RepresentName;
                    req.RepresentPosition ??= modelObj.RepresentPosition;
                    req.UserCodeOwner ??= modelObj.UserCodeOwner;
                    req.ContactName ??= modelObj.ContactName;
                    req.ContactPhone ??= modelObj.ContactPhone;
                    req.ContactEmail ??= modelObj.ContactEmail;
                    req.Fax ??= modelObj.Fax;
                    req.Facebook ??= modelObj.Facebook;
                    req.InvoiceCustomerName ??= modelObj.InvoiceCustomerName;
                    req.InvoiceCustomerAddress ??= modelObj.InvoiceCustomerAddress;
                    req.InvoiceOrgName ??= modelObj.InvoiceOrgName;
                    req.InvoiceEmailSend ??= modelObj.InvoiceEmailSend;
                    req.MST ??= modelObj.MST;
                    req.ListOfCustDynamicFieldValue ??= modelObj.ListOfCustDynamicFieldValue;
                    req.FlagDealer ??= modelObj.FlagDealer;
                    req.FlagSupplier ??= modelObj.FlagSupplier;
                    req.FlagEndUser ??= modelObj.FlagEndUser;
                    req.FlagShipper ??= modelObj.FlagShipper;
                    req.FlagBank ??= modelObj.FlagBank;
                    req.FlagInsurrance ??= modelObj.FlagInsurrance;
                    req.DTimeUsed ??= modelObj.DTimeUsed;
                    req.Remark ??= modelObj.Remark;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.FlagRedirect ??= modelObj.FlagRedirect;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.CustomerCode) && string.IsNullOrWhiteSpace(req.CustomerName) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_Customer_Create", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào (Mst_CustomerController.cs:1811-1867)
        var orgId = StdParam(req.OrgID) ?? Request?.Headers["OrgID"].FirstOrDefault() ?? _config?["Biz_OrgID"] ?? "7200000001";
        var networkId = StdParam(req.NetworkID) ?? Request?.Headers["NetworkID"].FirstOrDefault() ?? _config?["Biz_NetworkID"] ?? "1001";
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

        var customerCode = StdParam(req.CustomerCode);
        if (string.IsNullOrWhiteSpace(customerCode))
        {
            return PortResult.Fail("Mst_Customer_Create", "Check.CustomerCode", "Mã khách hàng không được trống!");
        }

        var customerName = StdStr(req.CustomerName);
        if (string.IsNullOrWhiteSpace(customerName))
        {
            return PortResult.Fail("Mst_Customer_Create", "Check.CustomerName", "Tên khách hàng không được trống!");
        }

        // 3. Kiểm tra trùng CustomerCode trong cùng OrgID (Customer_Check / Import)
        var exists = await _db.Set<Mst_Customer>().AnyAsync(x =>
            EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) &&
            EF.Functions.Like((x.CustomerCode ?? "").ToLower(), PortQuery.Equal(customerCode), PortQuery.Esc));
        if (exists)
        {
            return PortResult.Fail("Mst_Customer_Create", "Check.CustomerCode", "Mã khách hàng đã tồn tại trong hệ thống!");
        }

        var customerCodeSys = StdParam(req.CustomerCodeSys);
        if (string.IsNullOrEmpty(customerCodeSys))
        {
            customerCodeSys = $"CUS{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        }

        var dtimeNowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        // 4. Xử lý nhóm khách hàng (Mst_CustomerController.cs:1868-1884)
        var customerGroupsToSave = new List<Mst_CustomerInCustomerGroup>();
        var grpCodeStr = req.CustomerGrpCode?.Trim();
        if (!string.IsNullOrEmpty(grpCodeStr))
        {
            var grpCodes = grpCodeStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var gc in grpCodes)
            {
                customerGroupsToSave.Add(new Mst_CustomerInCustomerGroup
                {
                    OrgID = orgId,
                    CustomerCodeSys = customerCodeSys,
                    CustomerGrpCode = gc.Trim(),
                    NetworkID = networkId,
                    LogLUDTimeUTC = dtimeNowStr,
                    LogLUBy = userCode
                });
            }
        }

        // 5. Chuẩn bị entity Mst_Customer (Mst_CustomerController.cs:1811-1867)
        var cus = new Mst_Customer
        {
            OrgID = orgId,
            CustomerCodeSys = customerCodeSys,
            NetworkID = networkId,
            CustomerCode = customerCode,
            CustomerName = customerName,
            CustomerNameEN = StdStr(req.CustomerNameEN) ?? customerName,
            CustomerType = StdParam(req.CustomerType),
            CustomerSourceCode = StdParam(req.CustomerSourceCode),
            CustomerSourceNameActual = StdStr(req.CustomerSourceNameActual),
            CustomerPhoneNo = StdStr(req.CustomerPhoneNo),
            CustomerMobilePhone = StdStr(req.CustomerMobilePhone),
            CustomerAddress = StdStr(req.CustomerAddress),
            ProvinceCode = StdParam(req.ProvinceCode),
            DistrictCode = StdParam(req.DistrictCode),
            WardCode = StdParam(req.WardCode),
            AreaCode = StdParam(req.AreaCode),
            CustomerAvatarName = StdStr(req.CustomerAvatarName),
            CustomerAvatarSpec = StdStr(req.CustomerAvatarSpec),
            FlagCustomerAvatarPath = StdFlag(req.FlagCustomerAvatarPath),
            CustomerAvatarPath = StdStr(req.CustomerAvatarPath),
            CustomerGender = StdStr(req.CustomerGender),
            CustomerEmail = StdStr(req.CustomerEmail),
            CustomerDateOfBirth = StdStr(req.CustomerDateOfBirth),
            GovIDType = StdParam(req.GovIDType),
            GovIDCardNo = StdStr(req.GovIDCardNo),
            GovIDCardDate = StdStr(req.GovIDCardDate),
            GovIDCardPlace = StdStr(req.GovIDCardPlace),
            TaxCode = StdStr(req.TaxCode),
            BankCode = StdStr(req.BankCode ?? req.BankName),
            BankName = StdStr(req.BankName),
            BankAccountNo = StdStr(req.BankAccountNo),
            RepresentName = StdStr(req.RepresentName),
            RepresentPosition = StdStr(req.RepresentPosition),
            UserCodeOwner = StdParam(req.UserCodeOwner) ?? userCode,
            ContactName = StdStr(req.ContactName),
            ContactPhone = StdStr(req.ContactPhone),
            ContactEmail = StdStr(req.ContactEmail),
            Fax = StdStr(req.Fax),
            Facebook = StdStr(req.Facebook),
            InvoiceCustomerName = StdStr(req.InvoiceCustomerName),
            InvoiceOrgName = StdStr(req.InvoiceOrgName),
            MST = StdParam(req.MST),
            InvoiceCustomerAddress = StdStr(req.InvoiceCustomerAddress),
            InvoiceEmailSend = StdStr(req.InvoiceEmailSend),
            ListOfCustDynamicFieldValue = req.ListOfCustDynamicFieldValue,
            FlagDealer = StdFlag(req.FlagDealer),
            FlagSupplier = StdFlag(req.FlagSupplier),
            FlagShipper = StdFlag(req.FlagShipper),
            FlagEndUser = StdFlag(req.FlagEndUser),
            FlagBank = StdFlag(req.FlagBank),
            FlagInsurrance = StdFlag(req.FlagInsurrance),
            DTimeUsed = req.DTimeUsed,
            Remark = StdStr(req.Remark),
            CustomerGrpCode = "", // Gate line 1887: customer.CustomerGrpCode = ""
            FlagActive = !string.IsNullOrEmpty(req.FlagActive) ? StdFlag(req.FlagActive) : "1",
            CreateDTimeUTC = dtimeNowStr,
            CreateBy = userCode,
            LUDTimeUTC = dtimeNowStr,
            LUBy = userCode,
            LogLUDTimeUTC = dtimeNowStr,
            LogLUBy = userCode
        };

        // 6. Mở Transaction và lưu vào DB (Customer.cs:1650, 2697-2949)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Xoá bản ghi cũ nếu cùng OrgID + CustomerCodeSys (Customer.cs:2651-2690)
            var oldCus = await _db.Set<Mst_Customer>()
                .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) &&
                            EF.Functions.Like((x.CustomerCodeSys ?? "").ToLower(), PortQuery.Equal(customerCodeSys), PortQuery.Esc))
                .ToListAsync();
            if (oldCus.Count > 0)
            {
                _db.Set<Mst_Customer>().RemoveRange(oldCus);
            }

            var oldGrp = await _db.Set<Mst_CustomerInCustomerGroup>()
                .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) &&
                            EF.Functions.Like((x.CustomerCodeSys ?? "").ToLower(), PortQuery.Equal(customerCodeSys), PortQuery.Esc))
                .ToListAsync();
            if (oldGrp.Count > 0)
            {
                _db.Set<Mst_CustomerInCustomerGroup>().RemoveRange(oldGrp);
            }

            _db.Set<Mst_Customer>().Add(cus);
            if (customerGroupsToSave.Count > 0)
            {
                _db.Set<Mst_CustomerInCustomerGroup>().AddRange(customerGroupsToSave);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var redirectUrl = string.Equals(req.FlagRedirect, "1", StringComparison.OrdinalIgnoreCase)
                ? "/Mst_Customer/Create"
                : "/Mst_Customer/Index";

            return Ok(new
            {
                Success = true,
                Messages = new[] { "Tạo mới khách hàng thành công!" },
                Title = "Tạo mới khách hàng thành công!",
                RedirectUrl = redirectUrl,
                Data = cus
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Customer_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Kiểm tra trùng khách hàng theo số điện thoại, email hoặc mã số thuế trước khi lưu.
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_CustomerController.cs:2043-2166 Customer_Check(string model)
    ///       Mst_CustomerController.cs:3915-3937 strWhereClause_Mst_Customer_By_Or
    /// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstCustomerController.cs:92-162 WA_Mst_Customer_Get
    /// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Customer\Customer.cs:1464-1612 WAS_Mst_Customer_Get -> 996-1462 Mst_Customer_Get
    /// </summary>
    [HttpPost("customer_check")]
    public async Task<IActionResult> Customer_Check([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Customer_Customer_CheckReq? req)
    {
        // 1. Kiểm tra body rỗng (LESSON A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_Customer_Customer_Check", "Check.strJson", null);
        }

        var objMst_Customer_Check = new Mst_Customer_Check
        {
            Success = true,
            Action = "0"
        };

        try
        {
            // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_CustomerController.cs:2043, 2052)
            if (!string.IsNullOrWhiteSpace(req.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Customer_Customer_CheckReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        req.OrgID ??= modelObj.OrgID;
                        req.NetworkID ??= modelObj.NetworkID;
                        req.CustomerCode ??= modelObj.CustomerCode;
                        req.CustomerCodeSys ??= modelObj.CustomerCodeSys;
                        req.CustomerName ??= modelObj.CustomerName;
                        req.CustomerMobilePhone ??= modelObj.CustomerMobilePhone;
                        req.ContactEmail ??= modelObj.ContactEmail;
                        req.MST ??= modelObj.MST;
                    }
                }
                catch
                {
                    // Giữ nguyên req nếu chuỗi model không parse được
                }
            }

            var phone = req.CustomerMobilePhone?.Trim();
            var email = req.ContactEmail?.Trim();
            var mst = req.MST?.Trim();
            var customerCodeSys = req.CustomerCodeSys?.Trim();

            // Nếu cả 3 trường đều rỗng: strWhereClause_Mst_Customer_By_Or rỗng -> Action = "0" (Mst_CustomerController.cs:2065, 2151-2155)
            if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(mst))
            {
                objMst_Customer_Check.Action = "0";
                return Ok(objMst_Customer_Check);
            }

            var orgId = StdParam(req.OrgID) ?? Request?.Headers["OrgID"].FirstOrDefault() ?? _config?["Biz_OrgID"] ?? "7200000001";

            // Dựng truy vấn tìm kiếm Mst_Customer theo điều kiện OR giữa Phone, Email, MST (Mst_CustomerController.cs:3915-3937 và Customer.cs:1120-1137, 1344-1386)
            var query = _db.Set<Mst_Customer>().AsNoTracking();

            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            // Điều kiện OR: CustomerMobilePhone = @p_0 or ContactEmail = @p_1 or MST = @p_2
            query = query.Where(x =>
                (!string.IsNullOrEmpty(phone) && EF.Functions.Like((x.CustomerMobilePhone ?? "").ToLower(), PortQuery.Equal(phone), PortQuery.Esc)) ||
                (!string.IsNullOrEmpty(email) && EF.Functions.Like((x.ContactEmail ?? "").ToLower(), PortQuery.Equal(email), PortQuery.Esc)) ||
                (!string.IsNullOrEmpty(mst) && EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc))
            );

            // Sắp xếp CreateDTimeUTC desc theo SQL nguồn (Customer.cs:1136, 1250)
            var matchedCustomers = await query.OrderByDescending(x => x.CreateDTimeUTC).ToListAsync();

            if (matchedCustomers.Count > 0)
            {
                objMst_Customer_Check.Action = "1"; // hiển thị cảnh báo
                var messages = "";

                foreach (var item in matchedCustomers)
                {
                    // Bỏ qua kiểm tra trùng nếu là chính bản ghi đang sửa (CustomerCodeSys trùng nhau) (Mst_CustomerController.cs:2107-2114)
                    if (!string.IsNullOrEmpty(item.CustomerCodeSys) && !string.IsNullOrEmpty(customerCodeSys))
                    {
                        if (string.Equals(item.CustomerCodeSys, customerCodeSys, StringComparison.OrdinalIgnoreCase))
                        {
                            objMst_Customer_Check = new Mst_Customer_Check { Success = true, Action = "0" };
                            continue;
                        }
                    }

                    var customerMobilePhoneCur = item.CustomerMobilePhone?.Trim() ?? "";
                    var contactEmailCur = item.ContactEmail?.Trim() ?? "";
                    var mstCur = item.MST?.Trim() ?? "";

                    if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(customerMobilePhoneCur))
                    {
                        if (string.Equals(customerMobilePhoneCur, phone, StringComparison.OrdinalIgnoreCase))
                        {
                            messages = "Khách hàng có số điện thoại '" + phone + "' đã tồn tại. Bạn có muốn lấy thông tin KH hiện tại không?";
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(contactEmailCur))
                    {
                        if (string.Equals(contactEmailCur, email, StringComparison.OrdinalIgnoreCase))
                        {
                            messages = "Khách hàng có email '" + email + "' đã tồn tại. Bạn có muốn lấy thông tin KH hiện tại không?";
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(mst) && !string.IsNullOrEmpty(mstCur))
                    {
                        if (string.Equals(mstCur, mst, StringComparison.OrdinalIgnoreCase))
                        {
                            messages = "Khách hàng có MST '" + mst + "' đã tồn tại. Bạn có muốn lấy thông tin KH hiện tại không?";
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(messages))
                {
                    objMst_Customer_Check.Action = "1";
                }

                objMst_Customer_Check.CustomerCode = matchedCustomers[0].CustomerCode ?? "";
                objMst_Customer_Check.Messages = messages;
            }
            else
            {
                objMst_Customer_Check.Action = "0"; // gọi hàm lưu luôn (Mst_CustomerController.cs:2146-2149)
            }

            return Ok(objMst_Customer_Check);
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Customer_Customer_Check", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Lấy thông tin khách hàng phục vụ màn hình cập nhật (Update view).
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_CustomerController.cs:2170-2606 Update(string customercode, string customercodesys, string flagcustomerexsist)
    /// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstCustomerController.cs:92-162 WA_Mst_Customer_Get
    /// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Customer\Customer.cs:1464-1612 WAS_Mst_Customer_Get -> 996-1462 Mst_Customer_Get
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Customer_UpdateReq? req)
    {
        try
        {
            // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_CustomerController.cs:2170)
            if (!string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Customer_UpdateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        req.CustomerCode ??= modelObj.CustomerCode;
                        req.CustomerCodeSys ??= modelObj.CustomerCodeSys;
                        req.FlagCustomerExsist ??= modelObj.FlagCustomerExsist;
                        req.OrgID ??= modelObj.OrgID;
                        req.NetworkID ??= modelObj.NetworkID;
                    }
                }
                catch
                {
                    req.CustomerCode ??= req.Model;
                }
            }

            // 1. Chuẩn hoá tham số đầu vào (Mst_CustomerController.cs:2170, 2182-2192)
            var customerCode = StdParam(req?.CustomerCode);
            var customerCodeSys = StdParam(req?.CustomerCodeSys);
            var flagCustomerExsist = StdStr(req?.FlagCustomerExsist);
            var orgId = StdParam(req?.OrgID) ?? StdParam(Request?.Headers["OrgID"].FirstOrDefault()) ?? _config?["Biz_OrgID"] ?? "7200000001";
            var networkId = StdParam(req?.NetworkID) ?? StdParam(Request?.Headers["NetworkID"].FirstOrDefault()) ?? _config?["Biz_NetworkID"] ?? "1001";

            // 2. Nếu customerCode và customerCodeSys rỗng/null, gate trả về View(new Mst_Customer()) (Mst_CustomerController.cs:2182, 2605)
            if (string.IsNullOrEmpty(customerCode) && string.IsNullOrEmpty(customerCodeSys))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Customer_UpdateData?)null
                });
            }

            // 3. Dựng truy vấn tìm kiếm Mst_Customer (Gate: Mst_CustomerController.cs:2185-2221; BIZ: Customer.cs:1120-1137, 1344-1386)
            var query = _db.Set<Mst_Customer>().AsNoTracking();
            if (!string.IsNullOrEmpty(customerCode))
            {
                query = query.Where(x => EF.Functions.Like((x.CustomerCode ?? "").ToLower(), PortQuery.Equal(customerCode), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(customerCodeSys))
            {
                query = query.Where(x => EF.Functions.Like((x.CustomerCodeSys ?? "").ToLower(), PortQuery.Equal(customerCodeSys), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            var customer = await query.FirstOrDefaultAsync();
            if (customer == null)
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Customer_UpdateData?)null
                });
            }

            // 4. Lấy danh mục tham chiếu phụ trợ phục vụ Update view (Mst_CustomerController.cs:2224-2429)
            // Loại khách hàng (Mst_CustomerType: FlagActive = '1')
            var typeQuery = _db.Set<Mst_CustomerType>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));
            if (!string.IsNullOrEmpty(networkId))
            {
                typeQuery = typeQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            var listCustomerType = await typeQuery.ToListAsync();

            // Nhóm khách hàng (Mst_CustomerGroup: FlagActive = '1', OrgID, NetworkID)
            var groupQuery = _db.Set<Mst_CustomerGroup>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));
            if (!string.IsNullOrEmpty(orgId))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            var listCustomerGroup = await groupQuery.ToListAsync();

            // Nguồn khách hàng (Mst_CustomerSource: FlagActive = '1', OrgID, NetworkID)
            var sourceQuery = _db.Set<Mst_CustomerSource>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));
            if (!string.IsNullOrEmpty(orgId))
            {
                sourceQuery = sourceQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                sourceQuery = sourceQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            var listCustomerSource = await sourceQuery.ToListAsync();

            // Khu vực (Mst_Area: FlagActive = '1', OrgID, NetworkID)
            var areaQuery = _db.Set<Mst_Area>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));
            if (!string.IsNullOrEmpty(orgId))
            {
                areaQuery = areaQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                areaQuery = areaQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            var listArea = await areaQuery.ToListAsync();

            // Loại giấy tờ (Mst_GovIDType: FlagActive = '1', NetworkID)
            var govQuery = _db.Set<Mst_GovIDType>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));
            if (!string.IsNullOrEmpty(networkId))
            {
                govQuery = govQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            var listGovIDType = await govQuery.ToListAsync();

            // 5. Nối thông tin liên kết (CustomerGrpCode từ Mst_CustomerInCustomerGroup, tên nhóm, tên loại, khu vực...)
            await EnrichCustomersAsync(new List<Mst_Customer> { customer });

            // Chuẩn hoá đường dẫn ảnh (Mst_CustomerController.cs:2504-2512)
            if (!string.IsNullOrEmpty(customer.CustomerAvatarPath))
            {
                customer.CustomerAvatarPath = customer.CustomerAvatarPath.Replace("\\", "/");
            }

            // 6. Trả kết quả thành công với model khách hàng và các danh mục dropdown khớp gate MVC (Mst_CustomerController.cs:2475-2482, 2605)
            var updateData = new Mst_Customer_UpdateData(
                customer,
                flagCustomerExsist,
                listCustomerType,
                listCustomerGroup,
                listCustomerSource,
                listArea,
                listGovIDType);

            return Ok(new
            {
                Success = true,
                Data = updateData
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Customer_Get", "Exception", ex.Message);
        }
    }

    private class CustomerDynamicFieldParamType
    {
        public string Title { get; set; } = "";
        public string DefaultValue { get; set; } = "";
    }

    private async Task EnrichCustomersAsync(List<Mst_Customer> list)
    {
        if (list.Count == 0) return;

        var cusCodes = list.Select(x => x.CustomerCodeSys).Distinct().ToList();

        // Ghép nhóm khách hàng từ Mst_CustomerInCustomerGroup + Mst_CustomerGroup (Mst_CustomerController.cs:287-301)
        var cusGroups = await _db.Set<Mst_CustomerInCustomerGroup>().AsNoTracking()
            .Where(cg => cusCodes.Contains(cg.CustomerCodeSys))
            .ToListAsync();
        var allGroups = await _db.Set<Mst_CustomerGroup>().AsNoTracking().ToListAsync();
        var groupNameDict = allGroups.ToDictionary(g => $"{g.OrgID}_{g.CustomerGrpCode}", g => g.CustomerGrpName ?? "", StringComparer.OrdinalIgnoreCase);

        foreach (var cus in list)
        {
            var matching = cusGroups.Where(cg => string.Equals(cg.CustomerCodeSys, cus.CustomerCodeSys, StringComparison.OrdinalIgnoreCase)
                                              && string.Equals(cg.OrgID, cus.OrgID, StringComparison.OrdinalIgnoreCase)).ToList();
            var listCusGrpCode = matching.Select(m => m.CustomerGrpCode).Distinct().ToList();
            var listCusGrpName = matching.Select(m =>
            {
                groupNameDict.TryGetValue($"{m.OrgID}_{m.CustomerGrpCode}", out var name);
                return name ?? "";
            }).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();

            cus.CustomerGrpCode = string.Join(",", listCusGrpCode);
            cus.mcg_CustomerGrpName = string.Join(",", listCusGrpName);
        }

        // Ghép loại khách hàng từ Mst_CustomerType (Customer.cs:1187, 1234-1235)
        var customerTypes = await _db.Set<Mst_CustomerType>().AsNoTracking().ToListAsync();
        var typeDict = customerTypes.ToDictionary(t => t.CustomerType, t => t.CustomerTypeName ?? "", StringComparer.OrdinalIgnoreCase);
        foreach (var cus in list)
        {
            if (!string.IsNullOrEmpty(cus.CustomerType) && typeDict.TryGetValue(cus.CustomerType, out var typeName))
            {
                cus.mct_CustomerTypeName = typeName;
            }
        }

        // Ghép nguồn khách hàng từ Mst_CustomerSource (Customer.cs:1220, 1236-1238)
        var customerSources = await _db.Set<Mst_CustomerSource>().AsNoTracking().ToListAsync();
        var sourceDict = customerSources.ToDictionary(s => $"{s.OrgID}_{s.CustomerSourceCode}", s => s.CustomerSourceName ?? "", StringComparer.OrdinalIgnoreCase);
        foreach (var cus in list)
        {
            if (!string.IsNullOrEmpty(cus.CustomerSourceCode) && sourceDict.TryGetValue($"{cus.OrgID}_{cus.CustomerSourceCode}", out var srcName))
            {
                cus.mcs_CustomerSourceName = srcName;
            }
        }

        // Ghép khu vực từ Mst_Area (Customer.cs:1224, 1245-1247)
        var areas = await _db.Set<Mst_Area>().AsNoTracking().ToListAsync();
        var areaDict = areas.ToDictionary(a => $"{a.OrgID}_{a.AreaCode}", a => a.AreaName ?? "", StringComparer.OrdinalIgnoreCase);
        foreach (var cus in list)
        {
            if (!string.IsNullOrEmpty(cus.AreaCode) && areaDict.TryGetValue($"{cus.OrgID}_{cus.AreaCode}", out var areaName))
            {
                cus.ma_AreaName = areaName;
            }
        }

        // Ghép tổ chức / mạng lưới từ Mst_NNT (Customer.cs:1184-1185, 1230-1233)
        var nnts = await _db.Set<Mst_NNT>().AsNoTracking().ToListAsync();
        var nntByOrg = nnts.Where(n => !string.IsNullOrEmpty(n.OrgID))
                           .GroupBy(n => n.OrgID!)
                           .ToDictionary(g => g.Key, g => g.OrderBy(n => n.MST).First().NNTFullName ?? "", StringComparer.OrdinalIgnoreCase);
        foreach (var cus in list)
        {
            if (!string.IsNullOrEmpty(cus.OrgID) && nntByOrg.TryGetValue(cus.OrgID, out var orgName))
            {
                cus.Org_NNTFullName = orgName;
            }
            if (!string.IsNullOrEmpty(cus.NetworkID) && nntByOrg.TryGetValue(cus.NetworkID, out var netName))
            {
                cus.Network_NNTFullName = netName;
            }
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

public class Mst_Customer_IndexReq
{
    // Tham số từ Gate MVC Mst_CustomerController.cs:35-44
    public string? CustomerName { get; set; }
    public string[]? CustomerType { get; set; }
    public string[]? CustomerSourceCode { get; set; }
    public string[]? CustomerGrpCode { get; set; }
    public string[]? CustomerMobilePhone { get; set; }
    public string[]? ContactEmail { get; set; }
    public string? Init { get; set; } = "init";
    public int? Page { get; set; }
    public int? RecordCount { get; set; }

    // Các alias phân trang (LESSON A.12, A.14, PortQuery.PageInfo)
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    // Bộ lọc theo các cột của Mst_Customer (Customer.cs:1120-1137)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
    public string? KeyWord { get; set; }
}

public class Mst_Customer_DoSearchReq
{
    // Tham số từ Gate MVC Mst_CustomerController.cs:211-226
    public string? CustomerName { get; set; }
    public string[]? CustomerType { get; set; }
    public string[]? CustomerSourceCode { get; set; }
    public string? CustomerMobilePhone { get; set; }
    public string[]? CustomerGrpCode { get; set; }
    public string? ContactEmail { get; set; }
    public string? FlagDealer { get; set; }
    public string? FlagSupplier { get; set; }
    public string? FlagShipper { get; set; }
    public string? FlagEndUser { get; set; }
    public string? FlagBank { get; set; }
    public string? FlagInsurrance { get; set; }
    public string? Init { get; set; } = "init";
    public int? Page { get; set; }
    public int? RecordCount { get; set; }

    // Các alias phân trang (LESSON A.12, A.14, PortQuery.PageInfo)
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    // Bộ lọc theo ngữ cảnh / session (LESSON A.14, Customer.cs:1120-1137)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
}

public class Mst_Customer_ExportReq
{
    // Tham số từ Gate MVC Mst_CustomerController.cs:435-450
    public string? CustomerName { get; set; }
    public string[]? CustomerType { get; set; }
    public string[]? CustomerSourceCode { get; set; }
    public string? CustomerMobilePhone { get; set; }
    public string[]? CustomerGrpCode { get; set; }
    public string? ContactEmail { get; set; }
    public string? FlagDealer { get; set; }
    public string? FlagSupplier { get; set; }
    public string? FlagShipper { get; set; }
    public string? FlagEndUser { get; set; }
    public string? FlagBank { get; set; }
    public string? FlagInsurrance { get; set; }
    public string? Init { get; set; } = "init";
    public int? Page { get; set; }
    public int? RecordCount { get; set; }

    // Các alias phân trang (LESSON A.12, A.14, PortQuery.PageInfo)
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    // Bộ lọc theo ngữ cảnh / session (LESSON A.14, Customer.cs:1120-1137)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
}

public class Mst_Customer_ExportTemplateReq
{
}

public class Mst_Customer_DeleteReq
{
    // Tham số từ Gate MVC Mst_CustomerController.cs:1372 Delete(string listCustomers)
    public object? ListCustomers { get; set; }
    public List<Mst_Customer_DeleteItem>? Lst_Mst_Customer { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerCode { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}

public class Mst_Customer_DeleteItem
{
    public string? CustomerCodeSys { get; set; }
    public string? CustomerCode { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}

public class Mst_Customer_CreateReq
{
    // Hỗ trợ truyền chuỗi JSON model (Mst_CustomerController.cs:1803)
    public string? Model { get; set; }

    // Các tham số theo Mst_CustomerUI (Mst_CustomerController.cs:1811-1867)
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerType { get; set; }
    public string? CustomerGrpCode { get; set; }
    public string? CustomerSourceCode { get; set; }
    public string? CustomerSourceNameActual { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerNameEN { get; set; }
    public string? CustomerGender { get; set; }
    public string? CustomerPhoneNo { get; set; }
    public string? CustomerMobilePhone { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? WardCode { get; set; }
    public string? AreaCode { get; set; }
    public string? CustomerAvatarName { get; set; }
    public string? CustomerAvatarSpec { get; set; }
    public string? FlagCustomerAvatarPath { get; set; }
    public string? CustomerAvatarPath { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerDateOfBirth { get; set; }
    public string? GovIDType { get; set; }
    public string? GovIDCardNo { get; set; }
    public string? GovIDCardDate { get; set; }
    public string? GovIDCardPlace { get; set; }
    public string? TaxCode { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? RepresentName { get; set; }
    public string? RepresentPosition { get; set; }
    public string? UserCodeOwner { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Fax { get; set; }
    public string? Facebook { get; set; }
    public string? InvoiceCustomerName { get; set; }
    public string? InvoiceCustomerAddress { get; set; }
    public string? InvoiceOrgName { get; set; }
    public string? InvoiceEmailSend { get; set; }
    public string? MST { get; set; }
    public string? ListOfCustDynamicFieldValue { get; set; }
    public string? FlagDealer { get; set; }
    public string? FlagSupplier { get; set; }
    public string? FlagShipper { get; set; }
    public string? FlagEndUser { get; set; }
    public string? FlagBank { get; set; }
    public string? FlagInsurrance { get; set; }
    public string? DTimeUsed { get; set; }
    public string? Remark { get; set; }
    public string? FlagActive { get; set; }
    public string? FlagRedirect { get; set; }
}

public class Mst_Customer_Customer_CheckReq
{
    // Hỗ trợ truyền chuỗi JSON model (Mst_CustomerController.cs:2043, 2052)
    public string? Model { get; set; }

    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerMobilePhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? MST { get; set; }
}

public class Mst_Customer_Check
{
    public bool Success { get; set; } = true;
    public string? Messages { get; set; }
    public string Action { get; set; } = "0"; // 1: hiển thị cảnh báo; 0: gọi hàm lưu luôn
    public string? CustomerCode { get; set; }
}

public class Mst_Customer_UpdateReq
{
    // Tham số từ Gate MVC Mst_CustomerController.cs:2170 Update(string customercode, string customercodesys, string flagcustomerexsist)
    public string? CustomerCode { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? FlagCustomerExsist { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? Model { get; set; }
}

public class Mst_Customer_UpdateData : Mst_Customer
{
    public Mst_Customer? Mst_Customer { get; set; }
    public string? FlagCustomerExsist { get; set; }
    public List<Mst_CustomerType>? List_Mst_CustomerType { get; set; }
    public List<Mst_CustomerType>? ListMst_CustomerType { get; set; }
    public List<Mst_CustomerGroup>? ListMst_CustomerGroupExt { get; set; }
    public List<Mst_CustomerGroup>? List_Mst_CustomerGroup { get; set; }
    public List<Mst_CustomerSource>? ListMst_CustomerSourceExt { get; set; }
    public List<Mst_CustomerSource>? List_Mst_CustomerSource { get; set; }
    public List<Mst_Area>? ListMst_AreaExt { get; set; }
    public List<Mst_Area>? List_Mst_Area { get; set; }
    public List<Mst_GovIDType>? ListMst_GovIDType { get; set; }
    public List<Mst_GovIDType>? List_Mst_GovIDType { get; set; }

    public Mst_Customer_UpdateData() { }

    public Mst_Customer_UpdateData(
        Mst_Customer src,
        string? flagCustomerExsist = null,
        List<Mst_CustomerType>? listCustomerType = null,
        List<Mst_CustomerGroup>? listCustomerGroup = null,
        List<Mst_CustomerSource>? listCustomerSource = null,
        List<Mst_Area>? listArea = null,
        List<Mst_GovIDType>? listGovIDType = null)
    {
        OrgID = src.OrgID;
        CustomerCodeSys = src.CustomerCodeSys;
        NetworkID = src.NetworkID;
        CustomerCode = src.CustomerCode;
        CustomerType = src.CustomerType;
        CustomerGrpCode = src.CustomerGrpCode;
        CustomerSourceCode = src.CustomerSourceCode;
        CustomerSourceNameActual = src.CustomerSourceNameActual;
        CustomerName = src.CustomerName;
        CustomerNameEN = src.CustomerNameEN;
        CustomerGender = src.CustomerGender;
        CustomerPhoneNo = src.CustomerPhoneNo;
        CustomerMobilePhone = src.CustomerMobilePhone;
        ProvinceCode = src.ProvinceCode;
        DistrictCode = src.DistrictCode;
        WardCode = src.WardCode;
        AreaCode = src.AreaCode;
        CustomerAvatarName = src.CustomerAvatarName;
        CustomerAvatarSpec = src.CustomerAvatarSpec;
        FlagCustomerAvatarPath = src.FlagCustomerAvatarPath;
        CustomerAvatarPath = src.CustomerAvatarPath;
        CustomerAddress = src.CustomerAddress;
        CustomerEmail = src.CustomerEmail;
        CustomerDateOfBirth = src.CustomerDateOfBirth;
        GovIDType = src.GovIDType;
        GovIDCardNo = src.GovIDCardNo;
        GovIDCardDate = src.GovIDCardDate;
        GovIDCardPlace = src.GovIDCardPlace;
        TaxCode = src.TaxCode;
        BankCode = src.BankCode;
        BankName = src.BankName;
        BankAccountNo = src.BankAccountNo;
        RepresentName = src.RepresentName;
        RepresentPosition = src.RepresentPosition;
        UserCodeOwner = src.UserCodeOwner;
        ContactName = src.ContactName;
        ContactPhone = src.ContactPhone;
        ContactEmail = src.ContactEmail;
        Fax = src.Fax;
        Facebook = src.Facebook;
        InvoiceCustomerName = src.InvoiceCustomerName;
        InvoiceCustomerAddress = src.InvoiceCustomerAddress;
        InvoiceOrgName = src.InvoiceOrgName;
        InvoiceEmailSend = src.InvoiceEmailSend;
        MST = src.MST;
        ListOfCustDynamicFieldValue = src.ListOfCustDynamicFieldValue;
        FlagDealer = src.FlagDealer;
        FlagSupplier = src.FlagSupplier;
        FlagEndUser = src.FlagEndUser;
        FlagShipper = src.FlagShipper;
        FlagBank = src.FlagBank;
        FlagInsurrance = src.FlagInsurrance;
        DTimeUsed = src.DTimeUsed;
        CreateDTimeUTC = src.CreateDTimeUTC;
        CreateBy = src.CreateBy;
        LUDTimeUTC = src.LUDTimeUTC;
        LUBy = src.LUBy;
        FlagActive = src.FlagActive;
        GLN = src.GLN;
        Remark = src.Remark;
        Coordinates = src.Coordinates;
        LogLUDTimeUTC = src.LogLUDTimeUTC;
        LogLUBy = src.LogLUBy;

        Org_NNTFullName = src.Org_NNTFullName;
        Network_NNTFullName = src.Network_NNTFullName;
        mct_CustomerTypeName = src.mct_CustomerTypeName;
        mcg_CustomerGrpName = src.mcg_CustomerGrpName;
        mcs_CustomerSourceName = src.mcs_CustomerSourceName;
        mp_ProvinceName = src.mp_ProvinceName;
        md_DistrictName = src.md_DistrictName;
        mw_WardName = src.mw_WardName;
        ma_AreaName = src.ma_AreaName;
        mg_GovIDTypeName = src.mg_GovIDTypeName;

        Mst_Customer = src;
        FlagCustomerExsist = flagCustomerExsist;
        List_Mst_CustomerType = listCustomerType;
        ListMst_CustomerType = listCustomerType;
        ListMst_CustomerGroupExt = listCustomerGroup;
        List_Mst_CustomerGroup = listCustomerGroup;
        ListMst_CustomerSourceExt = listCustomerSource;
        List_Mst_CustomerSource = listCustomerSource;
        ListMst_AreaExt = listArea;
        List_Mst_Area = listArea;
        ListMst_GovIDType = listGovIDType;
        List_Mst_GovIDType = listGovIDType;
    }
}



