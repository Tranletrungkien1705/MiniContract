using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_SubmissionFormController.cs — Mst_SubmissionForm
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_SubmissionFormController.cs:23-140 (Index), 854-873 (strWhereClauseSearchBy)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web.DEV\Controllers\MstSubmissionFormController.cs:21-89 (WA_Mst_SubmissionForm_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\MasterData.cs:13845-13958 (WAS_Mst_SubmissionForm_Get), 13983-14121 (Mst_SubmissionForm_Get), 14122-14341 (Mst_SubmissionForm_GetX)
[ApiController]
[Route("api/mst_submissionform")]
public class Mst_SubmissionFormControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_SubmissionFormControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tìm kiếm và phân trang danh sách mẫu nội dung gửi.
    /// Gate: Mst_SubmissionFormController.cs:23-140 Index(string init = "init", int recordcount = 10, int page = 0, string checkedChannelType = "", string checkedBulletinType = "", string checkedStatusUse = "")
    /// API:  MstSubmissionFormController.cs:21-89 WA_Mst_SubmissionForm_Get
    /// BIZ:  MasterData.cs:13845-13958 WAS_Mst_SubmissionForm_Get -> 13983-14121 Mst_SubmissionForm_Get -> 14122-14341 Mst_SubmissionForm_GetX
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_SubmissionForm_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_SubmissionFormController.cs:24-26
            var recordcount = req?.RecordCount ?? req?.Ft_RecordCount ?? req?.Ft_PageSize ?? req?.PageSize ?? 10;
            var page = req?.Page ?? req?.Ft_PageIndex ?? req?.PageIndex ?? 0;

            // 2. Dựng truy vấn cơ sở từ bảng Mst_SubmissionForm (MasterData.cs:14176)
            var query = _db.Set<Mst_SubmissionForm>().AsNoTracking();

            // 3. Xử lý điều kiện lọc khi init != "init" hoặc có truyền tham số tìm kiếm (Gate: Mst_SubmissionFormController.cs:53-88, 854-873)
            var isInitialLoad = string.Equals(req?.Init, "init", StringComparison.OrdinalIgnoreCase)
                                && string.IsNullOrWhiteSpace(req?.CheckedChannelType)
                                && string.IsNullOrWhiteSpace(req?.ChannelType)
                                && string.IsNullOrWhiteSpace(req?.CheckedBulletinType)
                                && string.IsNullOrWhiteSpace(req?.BulletinType)
                                && string.IsNullOrWhiteSpace(req?.CheckedStatusUse)
                                && string.IsNullOrWhiteSpace(req?.Status)
                                && string.IsNullOrWhiteSpace(req?.FlagActive)
                                && string.IsNullOrWhiteSpace(req?.OrgID)
                                && string.IsNullOrWhiteSpace(req?.SubFormCode)
                                && string.IsNullOrWhiteSpace(req?.SubFormName)
                                && string.IsNullOrWhiteSpace(req?.KeyWord);

            if (!isInitialLoad)
            {
                // Lọc theo loại kênh (ChannelType in (...)) (Gate: Mst_SubmissionFormController.cs:59-66, 859-862)
                var rawChannelType = req?.CheckedChannelType ?? req?.ChannelType;
                if (!string.IsNullOrWhiteSpace(rawChannelType))
                {
                    var channelTypes = rawChannelType
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim().ToLower())
                        .Where(x => x.Length > 0)
                        .ToList();

                    if (channelTypes.Count > 0)
                    {
                        query = query.Where(x => channelTypes.Contains((x.ChannelType ?? "").ToLower()));
                    }
                }

                // Lọc theo loại bản tin (BulletinType in (...)) (Gate: Mst_SubmissionFormController.cs:68-75, 863-866)
                var rawBulletinType = req?.CheckedBulletinType ?? req?.BulletinType;
                if (!string.IsNullOrWhiteSpace(rawBulletinType))
                {
                    var bulletinTypes = rawBulletinType
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim().ToLower())
                        .Where(x => x.Length > 0)
                        .ToList();

                    if (bulletinTypes.Count > 0)
                    {
                        query = query.Where(x => bulletinTypes.Contains((x.BulletinType ?? "").ToLower()));
                    }
                }

                // Lọc theo trạng thái sử dụng (FlagActive in (...)) (Gate: Mst_SubmissionFormController.cs:77-84, 867-870)
                var rawStatus = req?.CheckedStatusUse ?? req?.Status ?? req?.FlagActive;
                if (!string.IsNullOrWhiteSpace(rawStatus))
                {
                    var statuses = rawStatus
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim().ToLower())
                        .Where(x => x.Length > 0)
                        .ToList();

                    if (statuses.Count > 0)
                    {
                        query = query.Where(x => statuses.Contains((x.FlagActive ?? "").ToLower()));
                    }
                }

                // Lọc bổ sung theo OrgID nếu có (MasterData.cs:14181, 14289-14322)
                var orgId = req?.OrgID?.Trim();
                if (!string.IsNullOrEmpty(orgId))
                {
                    query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
                }

                // Lọc bổ sung theo mã mẫu SubFormCode nếu có
                var subFormCode = req?.SubFormCode?.Trim();
                if (!string.IsNullOrEmpty(subFormCode))
                {
                    query = query.Where(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(subFormCode), PortQuery.Esc));
                }

                // Lọc bổ sung theo tên mẫu hoặc KeyWord nếu có
                var keyWord = req?.KeyWord?.Trim() ?? req?.SubFormName?.Trim();
                if (!string.IsNullOrEmpty(keyWord))
                {
                    query = query.Where(x => EF.Functions.Like((x.SubFormName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc));
                }
            }

            // 4. Đếm tổng số bản ghi phù hợp (MasterData.cs:14188: select Count(0) MyCount from ...)
            var itemCount = await query.CountAsync();

            // 5. Sắp xếp: order by msf.LogLUDTimeUTC desc, msf.SubFormCode asc (MasterData.cs:14184, 14232)
            var ordered = query.OrderByDescending(x => x.LogLUDTimeUTC).ThenBy(x => x.SubFormCode);

            // 6. Tính toán vị trí phân trang (Gate: Mst_SubmissionFormController.cs:104-105; BIZ: MasterData.cs:14151-14152)
            long recordStart;
            if (req != null && req.Ft_RecordStart != null && req.Page == null && req.Ft_PageIndex == null && req.PageIndex == null)
            {
                recordStart = req.Ft_RecordStart.Value;
                page = recordcount > 0 ? (int)(recordStart / recordcount) : 0;
            }
            else
            {
                recordStart = (long)page * recordcount;
            }

            // 7. Lấy dữ liệu trang (LESSON A.7, A.12 chống tràn int và xử lý chỉ số âm/kích thước 0)
            var list = (recordcount <= 0 || recordStart < 0)
                ? new List<Mst_SubmissionForm>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordcount, int.MaxValue))
                               .ToListAsync();

            // 8. Tính số trang theo cấu hình (Gate: Mst_SubmissionFormController.cs:121: (itemCount % PageSizeConfig == 0) ? ... : ... + 1)
            const int pageSizeConfig = 10;
            var ps = recordcount > 0 ? recordcount : pageSizeConfig;
            var pageCount = list.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

            // 9. Trả về cấu trúc PageInfo bọc trong Success/Data (Data/PortQuery.cs:31, LESSON A.5, A.11)
            return Ok(new
            {
                Success = true,
                Data = new PageInfo<Mst_SubmissionForm>(list, itemCount, pageCount, page, recordcount)
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_SubmissionForm_Index", "Exception", ex.Message);
        }
    }
}

public class Mst_SubmissionForm_IndexReq
{
    public string? Init { get; set; } = "init";
    public int? RecordCount { get; set; }
    public int? Ft_RecordCount { get; set; }
    public int? Ft_PageSize { get; set; }
    public int? PageSize { get; set; }
    public int? Page { get; set; }
    public int? Ft_PageIndex { get; set; }
    public int? PageIndex { get; set; }
    public long? Ft_RecordStart { get; set; }
    public string? CheckedChannelType { get; set; }
    public string? ChannelType { get; set; }
    public string? CheckedBulletinType { get; set; }
    public string? BulletinType { get; set; }
    public string? CheckedStatusUse { get; set; }
    public string? Status { get; set; }
    public string? FlagActive { get; set; }
    public string? OrgID { get; set; }
    public string? SubFormCode { get; set; }
    public string? SubFormName { get; set; }
    public string? KeyWord { get; set; }
}
