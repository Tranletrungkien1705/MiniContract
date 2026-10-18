using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_OrganizationController.cs — Mst_NNT
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_OrganizationController.cs:25-109, 363-431, 435-683
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstNNTController.cs:19-89, 1109-1179
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:12001-12120, 12121-12218, 25188-25368, 32350-32449, 33301-33849, 34791-35208; MasterData.Org.cs:516-635
[ApiController]
[Route("api/mst_organization")]
public class Mst_OrganizationControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Mst_OrganizationControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
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

    /// <summary>
    /// Tạo mới tổ chức / chi nhánh (Mst_NNT và Mst_Org).
    /// Gate: Mst_OrganizationController.cs:363-431 [HttpPost] Create(string model)
    /// API:  MstNNTController.cs:1109-1179 WA_Mst_NNT_CreateForNetwork
    /// BIZ:  Master.cs:32350-32449 WAS_Mst_NNT_CreateForNetwork_New20200417 -> Mst_NNT_CreateForNetwork_New20200417 (33301-33849)
    ///       -> Mst_NNT_CreateForNetworkX_New20200208 (34791-35208)
    ///       MasterData.Org.cs:516-635 Mst_Org_Create_X
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Organization_CreateReq? req)
    {
        // 1. Kiểm tra body rỗng (Lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_NNT_Create", "Check.strJson", null);
        }

        // 2. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_OrganizationController.cs:365, 386)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_Organization_CreateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.MST ??= modelObj.MST;
                    req.OrgID ??= modelObj.OrgID;
                    req.NNTFullName ??= modelObj.NNTFullName;
                    req.NNTShortName ??= modelObj.NNTShortName;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.MSTParent ??= modelObj.MSTParent;
                    req.ProvinceCode ??= modelObj.ProvinceCode;
                    req.DistrictCode ??= modelObj.DistrictCode;
                    req.DLCode ??= modelObj.DLCode;
                    req.NNTAddress ??= modelObj.NNTAddress;
                    req.NNTMobile ??= modelObj.NNTMobile;
                    req.NNTPhone ??= modelObj.NNTPhone;
                    req.NNTFax ??= modelObj.NNTFax;
                    req.PresentBy ??= modelObj.PresentBy;
                    req.BusinessRegNo ??= modelObj.BusinessRegNo;
                    req.NNTPosition ??= modelObj.NNTPosition;
                    req.PresentIDNo ??= modelObj.PresentIDNo;
                    req.PresentIDType ??= modelObj.PresentIDType;
                    req.GovTaxID ??= modelObj.GovTaxID;
                    req.ContactName ??= modelObj.ContactName;
                    req.ContactPhone ??= modelObj.ContactPhone;
                    req.ContactEmail ??= modelObj.ContactEmail;
                    req.Website ??= modelObj.Website;
                    req.CANumber ??= modelObj.CANumber;
                    req.CAOrg ??= modelObj.CAOrg;
                    req.CAEffDTimeUTCStart ??= modelObj.CAEffDTimeUTCStart;
                    req.CAEffDTimeUTCEnd ??= modelObj.CAEffDTimeUTCEnd;
                    req.PackageCode ??= modelObj.PackageCode;
                    req.CreatedDate ??= modelObj.CreatedDate;
                    req.AccNo ??= modelObj.AccNo;
                    req.AccHolder ??= modelObj.AccHolder;
                    req.BankName ??= modelObj.BankName;
                    req.BizType ??= modelObj.BizType;
                    req.BizFieldCode ??= modelObj.BizFieldCode;
                    req.BizSizeCode ??= modelObj.BizSizeCode;
                    req.DealerType ??= modelObj.DealerType;
                    req.AreaCode ??= modelObj.AreaCode;
                    req.Remark ??= modelObj.Remark;
                    req.MSTToken ??= modelObj.MSTToken;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.MST) && string.IsNullOrWhiteSpace(req.NNTFullName) && string.IsNullOrWhiteSpace(req.OrgID) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_NNT_Create", "Check.strJson", null);
        }

        // 3. Gate guard: kiểm tra MST trong chứng thư số và mã số thuế chi nhánh (Mst_OrganizationController.cs:388-396)
        if (!string.IsNullOrEmpty(req.CANumber))
        {
            if (!string.IsNullOrEmpty(req.MSTToken) && !string.Equals(req.MST, req.MSTToken, StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    Success = true,
                    Message = "MST trong chứng thư số không trùng với MST của chi nhánh!"
                });
            }
        }

        // 4. Chuẩn hoá tham số đầu vào (Master.cs:34887-34923, MasterData.Org.cs:552-556)
        var dtimeSys = DateTime.UtcNow;
        var dtimeSysStr = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";
        var networkId = StdParam(req.NetworkID) ?? _config?["Biz_NetworkID"] ?? "1001";
        var strMST = StdParam(req.MST);
        var strNNTFullName = StdStr(req.NNTFullName);
        var strOrgID = StdParam(req.OrgID);

        // 5. Kiểm tra tính hợp lệ cơ bản
        if (string.IsNullOrEmpty(strMST))
        {
            return PortResult.Fail("Mst_NNT_Create_InvalidMST", "Check.strMST", req.MST);
        }

        if (string.IsNullOrEmpty(strNNTFullName))
        {
            return PortResult.Fail("Mst_NNT_Create_InvalidNNTFullName", "Check.strNNTFullName", req.NNTFullName);
        }

        if (string.IsNullOrEmpty(strOrgID))
        {
            return PortResult.Fail("Mst_Org_Create_InvalidOrgID", "Check.strOrgID", req.OrgID);
        }

        // 6. Kiểm tra trùng mã MST trong Mst_NNT (Master.cs:34942-34950, Master.cs:12039-12050)
        var mstExist = await _db.Set<Mst_NNT>().AnyAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));
        if (mstExist)
        {
            return PortResult.Fail("Mst_NNT_CheckDB_NNTExist", "Check.MST", strMST);
        }

        // 7. Kiểm tra trùng mã OrgID trong Mst_Org (MasterData.Org.cs:573-580, MasterData.Org.cs:67-78)
        var orgExist = await _db.Set<Mst_Org>().AnyAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc));
        if (orgExist)
        {
            return PortResult.Fail("Mst_Org_CheckDB_OrgExist", "Check.OrgID", strOrgID);
        }

        // 8. Xác định MSTParent và kiểm tra tổ chức cha (Mst_OrganizationController.cs:369-376, Master.cs:34953-34965, 12028-12065)
        var strMSTParent = StdParam(req.MSTParent);
        if (string.IsNullOrEmpty(strMSTParent))
        {
            var rootNNT = await _db.Set<Mst_NNT>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.MSTParent ?? "").ToLower(), PortQuery.Equal("ALL"), PortQuery.Esc));
            if (rootNNT != null)
            {
                strMSTParent = rootNNT.MST;
            }
        }

        Mst_NNT? parentNNT = null;
        if (!string.IsNullOrEmpty(strMSTParent))
        {
            parentNNT = await _db.Set<Mst_NNT>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMSTParent), PortQuery.Esc));
            if (parentNNT == null)
            {
                return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", strMSTParent);
            }
            if (parentNNT.FlagActive != "1")
            {
                return PortResult.Fail("Mst_NNT_CheckDB_FlagActiveNotMatched",
                    "Check.MST", strMSTParent,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", parentNNT.FlagActive);
            }
        }

        // 9. Thực thi transaction ghi CSDL (Master.cs:33402)
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // (a) Tạo Mst_Org và tính phân cấp OrgBU (Master.cs:33514-33528, MasterData.Org.cs:604-635)
            var parentOrg = parentNNT != null && !string.IsNullOrEmpty(parentNNT.OrgID)
                ? await _db.Set<Mst_Org>().AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == parentNNT.OrgID)
                : null;

            var orgLevel = 1;
            var orgBUCode = strOrgID;
            if (parentOrg != null)
            {
                if (int.TryParse(parentOrg.OrgLevel, out var pl)) orgLevel = pl + 1;
                orgBUCode = (string.IsNullOrEmpty(parentOrg.OrgBUCode) ? "" : parentOrg.OrgBUCode + ".") + strOrgID;
            }

            var org = new Mst_Org
            {
                OrgID = strOrgID,
                NetworkID = networkId,
                OrgParent = parentOrg?.OrgID ?? "0",
                OrgBUCode = orgBUCode,
                OrgBUPattern = orgBUCode + "%",
                OrgLevel = orgLevel.ToString(),
                Remark = StdStr(req.Remark) ?? "",
                OrgIDSln = null,
                FlagActive = "1",
                LogLUDTimeUTC = dtimeSysStr,
                LogLUBy = userCode,
                MST = strMST
            };
            _db.Set<Mst_Org>().Add(org);
            await _db.SaveChangesAsync();

            // (b) Tạo Mst_NNT và tính phân cấp MSTBU (Master.cs:35104-35173, Master.cs:12084-12120)
            var nntLevel = 1;
            var nntBUCode = strMST;
            if (parentNNT != null)
            {
                if (int.TryParse(parentNNT.MSTLevel, out var nl)) nntLevel = nl + 1;
                nntBUCode = (string.IsNullOrEmpty(parentNNT.MSTBUCode) ? "" : parentNNT.MSTBUCode + ".") + strMST;
            }

            var nnt = new Mst_NNT
            {
                MST = strMST,
                OrgID = strOrgID,
                NNTFullName = strNNTFullName,
                NNTShortName = StdStr(req.NNTShortName),
                NetworkID = networkId,
                MSTParent = strMSTParent,
                MSTBUCode = nntBUCode,
                MSTBUPattern = nntBUCode + "%",
                MSTLevel = nntLevel.ToString(),
                ProvinceCode = StdParam(req.ProvinceCode),
                DistrictCode = StdParam(req.DistrictCode),
                DLCode = StdParam(req.DLCode),
                NNTAddress = StdStr(req.NNTAddress),
                NNTMobile = StdStr(req.NNTMobile),
                NNTPhone = StdStr(req.NNTPhone),
                NNTFax = StdStr(req.NNTFax),
                PresentBy = StdStr(req.PresentBy),
                BusinessRegNo = StdStr(req.BusinessRegNo),
                NNTPosition = StdStr(req.NNTPosition),
                PresentIDNo = StdStr(req.PresentIDNo),
                PresentIDType = StdStr(req.PresentIDType),
                GovTaxID = StdParam(req.GovTaxID),
                ContactName = StdStr(req.ContactName),
                ContactPhone = StdStr(req.ContactPhone),
                ContactEmail = StdStr(req.ContactEmail),
                Website = StdStr(req.Website),
                CANumber = StdStr(req.CANumber),
                CAOrg = StdStr(req.CAOrg),
                CAEffDTimeUTCStart = StdStr(req.CAEffDTimeUTCStart),
                CAEffDTimeUTCEnd = StdStr(req.CAEffDTimeUTCEnd),
                PackageCode = StdStr(req.PackageCode),
                CreatedDate = StdStr(req.CreatedDate),
                AccNo = StdStr(req.AccNo),
                AccHolder = StdStr(req.AccHolder),
                BankName = StdStr(req.BankName),
                BizType = StdParam(req.BizType),
                BizFieldCode = StdParam(req.BizFieldCode),
                BizSizeCode = StdParam(req.BizSizeCode),
                DealerType = StdParam(req.DealerType),
                AreaCode = StdParam(req.AreaCode),
                RegisterStatus = "Finish",
                FlagActive = "1",
                TCTStatus = "1",
                CreateDTime = dtimeSysStr,
                CreateBy = userCode,
                LogLUDTimeUTC = dtimeSysStr,
                LogLUBy = userCode
            };
            _db.Set<Mst_NNT>().Add(nnt);
            await _db.SaveChangesAsync();

            // (c) Hậu kiểm TotalQtyNetWork (Master.cs:35177-35205):
            var totalQty = await _db.Set<Mst_NNT>()
                .CountAsync(x => x.MSTLevel == "1" && x.MST != "ALL" && string.IsNullOrEmpty(x.MSTParent));
            if (totalQty > 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_Create_InvalidTotalNetworkID", "Check.TotalQtyNetWork", totalQty, "Check.TotalQtyNetWork", totalQty);
            }

            // (d) Khởi tạo giá trị mặc định từ OrgID '0' (Master.cs:33596-33775)
            var rootAreas = await _db.Set<Mst_Area>().AsNoTracking().Where(x => x.OrgID == "0").ToListAsync();
            foreach (var a in rootAreas)
            {
                _db.Set<Mst_Area>().Add(new Mst_Area
                {
                    OrgID = strOrgID,
                    AreaCode = a.AreaCode,
                    NetworkID = networkId,
                    AreaCodeParent = a.AreaCodeParent,
                    AreaBUCode = a.AreaBUCode,
                    AreaBUPattern = a.AreaBUPattern,
                    AreaLevel = a.AreaLevel,
                    AreaName = a.AreaName,
                    AreaDesc = a.AreaDesc,
                    FlagActive = a.FlagActive,
                    LogLUDTimeUTC = dtimeSysStr,
                    LogLUBy = userCode
                });
            }
            if (rootAreas.Count > 0)
            {
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();

            return Ok(new
            {
                Success = true,
                Message = "Tạo chi nhánh thành công!",
                Data = nnt
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_NNT_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Lấy thông tin tổ chức / chi nhánh để cập nhật (Update view).
    /// Gate: Mst_OrganizationController.cs:435-683 Update(string mst)
    /// API:  MstNNTController.cs:19-89 WA_Mst_NNT_Get
    /// BIZ:  Master.cs:25188-25368 WAS_Mst_NNT_Get -> Mst_NNT_GetX
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Organization_UpdateReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào MST (Gate: Mst_OrganizationController.cs:435, 461)
            var strMST = StdParam(req?.MST);
            if (string.IsNullOrEmpty(strMST) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Organization_UpdateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    strMST = StdParam(modelObj?.MST);
                }
                catch
                {
                    strMST = StdParam(req.Model);
                }
            }

            // 2. Nếu mst rỗng, gate trả về View(new Mst_NNT()) (Mst_OrganizationController.cs:461, 682)
            if (string.IsNullOrEmpty(strMST))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Organization_UpdateData?)null
                });
            }

            // 3. Truy vấn Mst_NNT theo MST (Master.cs:25244-25256)
            var nnt = await _db.Set<Mst_NNT>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));

            if (nnt == null)
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Organization_UpdateData?)null
                });
            }

            // 4. Lấy danh sách cấu hình chữ ký số của tổ chức (Mst_OrganizationController.cs:538-567: Lst_Mst_OrgSignConfig)
            List<Mst_OrgSignConfig>? orgSignConfigs = null;
            if (!string.IsNullOrEmpty(nnt.OrgID))
            {
                orgSignConfigs = await _db.Set<Mst_OrgSignConfig>().AsNoTracking()
                    .Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(nnt.OrgID), PortQuery.Esc))
                    .ToListAsync();
            }

            // 5. Lấy danh sách vùng thị trường active (Mst_OrganizationController.cs:519-526: List_Mst_Area)
            var areas = await _db.Set<Mst_Area>().AsNoTracking()
                .Where(x => x.FlagActive == "1")
                .ToListAsync();

            // Lấy tên vùng thị trường của chi nhánh nếu có (Master.cs:25311-25313: left join Mst_Area)
            string? areaName = null;
            if (!string.IsNullOrEmpty(nnt.AreaCode))
            {
                var matchedArea = areas.FirstOrDefault(a => string.Equals(a.AreaCode, nnt.AreaCode, StringComparison.OrdinalIgnoreCase));
                areaName = matchedArea?.AreaName;
            }

            // 6. Đóng gói kết quả trả về
            var resultData = new Mst_Organization_UpdateData(nnt)
            {
                ma_AreaName = areaName,
                Lst_Mst_OrgSignConfig = orgSignConfigs,
                List_Mst_Area = areas
            };

            return Ok(new
            {
                Success = true,
                Data = resultData
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Organization_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Cập nhật thông tin chi nhánh / tổ chức (Mst_NNT).
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_OrganizationController.cs:687-816 Edit(string model)
    /// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstNNTController.cs:604-673 WA_Mst_NNT_Update
    /// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:29088-29179 WAS_Mst_NNT_Update_New20190822
    ///       -> Mst_NNT_Update_New20200208 (29783-30027) -> Mst_NNT_UpdateX_New20200208 (30391-30764)
    /// </summary>
    [HttpPost("edit")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Organization_EditReq? req)
    {
        // 1. Kiểm tra body rỗng hoàn toàn (Lesson A.15, A.21; Master.cs:29105, 29842)
        if (req == null)
        {
            return PortResult.Fail("Mst_NNT_Update", "Check.strJson", null);
        }

        // 2. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_OrganizationController.cs:687, 695)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_Organization_EditReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.MST ??= modelObj.MST;
                    req.OrgID ??= modelObj.OrgID;
                    req.NNTFullName ??= modelObj.NNTFullName;
                    req.NNTShortName ??= modelObj.NNTShortName;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.PresentBy ??= modelObj.PresentBy;
                    req.NNTPosition ??= modelObj.NNTPosition;
                    req.ProvinceCode ??= modelObj.ProvinceCode;
                    req.DistrictCode ??= modelObj.DistrictCode;
                    req.NNTAddress ??= modelObj.NNTAddress;
                    req.ContactName ??= modelObj.ContactName;
                    req.ContactPhone ??= modelObj.ContactPhone;
                    req.ContactEmail ??= modelObj.ContactEmail;
                    req.PresentIDNo ??= modelObj.PresentIDNo;
                    req.PresentIDType ??= modelObj.PresentIDType;
                    req.NNTMobile ??= modelObj.NNTMobile;
                    req.NNTPhone ??= modelObj.NNTPhone;
                    req.NNTFax ??= modelObj.NNTFax;
                    req.Website ??= modelObj.Website;
                    req.AccNo ??= modelObj.AccNo;
                    req.AccHolder ??= modelObj.AccHolder;
                    req.BankName ??= modelObj.BankName;
                    req.CANumber ??= modelObj.CANumber;
                    req.CAOrg ??= modelObj.CAOrg;
                    req.CAEffDTimeUTCEnd ??= modelObj.CAEffDTimeUTCEnd;
                    req.CAEffDTimeUTCStart ??= modelObj.CAEffDTimeUTCStart;
                    req.BizType ??= modelObj.BizType;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.SignType ??= modelObj.SignType;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.MST) && string.IsNullOrWhiteSpace(req.NNTFullName) && string.IsNullOrWhiteSpace(req.OrgID) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_NNT_Update", "Check.strJson", null);
        }

        // 3. Gate guard: Kiểm tra MST rỗng (Gate: Mst_OrganizationController.cs:697, 809-813)
        var strMST = StdParam(req.MST);
        if (string.IsNullOrEmpty(strMST))
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Mã chi nhánh trống!" },
                Title = "Mã chi nhánh trống!"
            });
        }

        // 4. Tra cứu bản ghi hiện tại trong DB theo MST (Gate: Mst_OrganizationController.cs:699-723; Biz: Master.cs:30574-30581)
        var existing = await _db.Set<Mst_NNT>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));

        if (existing == null)
        {
            // Gate: Mst_OrganizationController.cs:805-806
            return Ok(new
            {
                Success = true,
                Messages = new[] { $"Mã chi nhánh '{req.OrgID}' không có trong hệ thống!" },
                Title = $"Mã chi nhánh '{req.OrgID}' không có trong hệ thống!"
            });
        }

        // 5. Biz guard: Kiểm tra NNTFullName không được rỗng (Master.cs:30583-30593)
        var strNNTFullName = req.NNTFullName?.Trim();
        if (req.NNTFullName != null && string.IsNullOrEmpty(strNNTFullName))
        {
            return PortResult.Fail("Mst_NNT_Update_InvalidNNTFullName", "Check.strNNTFullName", req.NNTFullName);
        }

        // 6. Chuẩn hoá tham số và cập nhật các cột (Gate: Mst_OrganizationController.cs:725-786; BIZ: Master.cs:30492-30568, 30718-30755)
        var dtimeSys = DateTime.UtcNow;
        var dtimeSysStr = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (req.NNTFullName != null) existing.NNTFullName = strNNTFullName;
            if (req.NNTShortName != null) existing.NNTShortName = StdStr(req.NNTShortName);
            if (req.PresentBy != null) existing.PresentBy = StdStr(req.PresentBy);
            if (req.NNTPosition != null) existing.NNTPosition = StdStr(req.NNTPosition);
            if (req.ProvinceCode != null) existing.ProvinceCode = StdParam(req.ProvinceCode);
            if (req.DistrictCode != null) existing.DistrictCode = StdParam(req.DistrictCode);
            if (req.NNTAddress != null) existing.NNTAddress = StdStr(req.NNTAddress);
            if (req.ContactName != null) existing.ContactName = StdStr(req.ContactName);
            if (req.ContactPhone != null) existing.ContactPhone = StdStr(req.ContactPhone);
            if (req.ContactEmail != null) existing.ContactEmail = StdStr(req.ContactEmail);
            if (req.PresentIDNo != null) existing.PresentIDNo = StdStr(req.PresentIDNo);
            if (req.PresentIDType != null) existing.PresentIDType = StdStr(req.PresentIDType);
            if (req.NNTMobile != null) existing.NNTMobile = StdStr(req.NNTMobile);
            if (req.NNTPhone != null) existing.NNTPhone = StdStr(req.NNTPhone);
            if (req.NNTFax != null) existing.NNTFax = StdStr(req.NNTFax);
            if (req.Website != null) existing.Website = StdStr(req.Website);
            if (req.AccNo != null) existing.AccNo = StdStr(req.AccNo);
            if (req.AccHolder != null) existing.AccHolder = StdStr(req.AccHolder);
            if (req.BankName != null) existing.BankName = StdStr(req.BankName);
            if (req.CANumber != null) existing.CANumber = StdStr(req.CANumber);
            if (req.CAOrg != null) existing.CAOrg = StdStr(req.CAOrg);
            if (req.CAEffDTimeUTCEnd != null) existing.CAEffDTimeUTCEnd = StdStr(req.CAEffDTimeUTCEnd);
            if (req.CAEffDTimeUTCStart != null) existing.CAEffDTimeUTCStart = StdStr(req.CAEffDTimeUTCStart);
            if (req.BizType != null) existing.BizType = StdParam(req.BizType);
            if (req.FlagActive != null) existing.FlagActive = StdFlag(req.FlagActive);
            if (req.SignType != null) existing.SignType = StdParam(req.SignType);

            existing.LogLUDTimeUTC = dtimeSysStr;
            existing.LogLUBy = userCode;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // 7. Trả kết quả thành công khớp gate (Mst_OrganizationController.cs:799-801)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Sửa chi nhánh thành công!" },
                Title = "Sửa chi nhánh thành công!",
                RedirectUrl = "/Mst_Organization",
                Data = existing
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_NNT_Update", "Exception", ex.Message);
        }
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
    public string? SignType { get; set; }
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
        SignType = src.SignType;
        Remark = src.Remark;
        LogLUDTimeUTC = src.LogLUDTimeUTC;
        LogLUBy = src.LogLUBy;
    }
}

public class Mst_Organization_CreateReq
{
    public string? Model { get; set; }
    public string? MST { get; set; }
    public string? OrgID { get; set; }
    public string? NNTFullName { get; set; }
    public string? NNTShortName { get; set; }
    public string? NetworkID { get; set; }
    public string? MSTParent { get; set; }
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
    public string? AccNo { get; set; }
    public string? AccHolder { get; set; }
    public string? BankName { get; set; }
    public string? BizType { get; set; }
    public string? BizFieldCode { get; set; }
    public string? BizSizeCode { get; set; }
    public string? DealerType { get; set; }
    public string? AreaCode { get; set; }
    public string? Remark { get; set; }
    public string? MSTToken { get; set; }
}

public class Mst_Organization_UpdateReq
{
    public string? MST { get; set; }
    public string? Model { get; set; }
}

public class Mst_Organization_UpdateData : Mst_NNT
{
    public Mst_NNT? Mst_NNT { get; set; }
    public List<Mst_OrgSignConfig>? Lst_Mst_OrgSignConfig { get; set; }
    public List<Mst_Area>? List_Mst_Area { get; set; }
    public string? ma_AreaName { get; set; }

    public Mst_Organization_UpdateData() { }

    public Mst_Organization_UpdateData(Mst_NNT src)
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
        SignType = src.SignType;
        Remark = src.Remark;
        LogLUDTimeUTC = src.LogLUDTimeUTC;
        LogLUBy = src.LogLUBy;

        Mst_NNT = src;
    }
}

public class Mst_Organization_EditReq
{
    public string? Model { get; set; }
    public string? MST { get; set; }
    public string? OrgID { get; set; }
    public string? NNTFullName { get; set; }
    public string? NNTShortName { get; set; }
    public string? NetworkID { get; set; }
    public string? PresentBy { get; set; }
    public string? NNTPosition { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? NNTAddress { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? PresentIDNo { get; set; }
    public string? PresentIDType { get; set; }
    public string? NNTMobile { get; set; }
    public string? NNTPhone { get; set; }
    public string? NNTFax { get; set; }
    public string? Website { get; set; }
    public string? AccNo { get; set; }
    public string? AccHolder { get; set; }
    public string? BankName { get; set; }
    public string? CANumber { get; set; }
    public string? CAOrg { get; set; }
    public string? CAEffDTimeUTCEnd { get; set; }
    public string? CAEffDTimeUTCStart { get; set; }
    public string? BizType { get; set; }
    public string? FlagActive { get; set; }
    public string? SignType { get; set; }
}
