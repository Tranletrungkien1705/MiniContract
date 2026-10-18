using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_CustomerController.cs — Mst_Customer
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_CustomerController.cs:35-207
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstCustomerController.cs:92-162 (WA_Mst_Customer_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Customer\Customer.cs:1464-1612 (WAS_Mst_Customer_Get), 996-1462 (Mst_Customer_Get)
[ApiController]
[Route("api/mst_customer")]
public class Mst_CustomerControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_CustomerControllerPort(AppDbContext db)
    {
        _db = db;
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
