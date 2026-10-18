using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_SubmissionFormController.cs — Mst_SubmissionForm
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_SubmissionFormController.cs:23-140 (Index), 142-209 (Create), 212-369 (Detail), 854-873 (strWhereClauseSearchBy), 875-888 (strWhereClause_Mst_SubmissionForm)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web.DEV\Controllers\MstSubmissionFormController.cs:21-89 (WA_Mst_SubmissionForm_Get), MstChannelTypeController.cs:91-140 (WA_Mst_Channel_Get), MstParamContractController.cs:19-75 (WA_Mst_ParamContractSubmissForm_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\MasterData.cs:13845-14341 (Mst_SubmissionForm_GetX), 18384-18780 (Mst_Channel_GetX), 16417-16796 (Mst_ParamContractSubmissForm_GetX)
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

    /// <summary>
    /// Khởi tạo dữ liệu tạo mới mẫu nội dung gửi.
    /// Gate: Mst_SubmissionFormController.cs:142-209 Create()
    /// BIZ:
    ///   - Mst_Channel_GetX (MasterData.cs:18384-18780): Đọc cấu hình kênh gửi Mst_Channel, Mst_ChannelEmail, Mst_ChannelSMS, Mst_ChannelZalo theo OrgID và FlagActive = '1'
    ///   - Mst_ParamContractSubmissForm_GetX (MasterData.cs:16417-16796): Đọc danh sách tham số hợp đồng Mst_ParamContractSubmissForm với FlagActive = '1'
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_SubmissionForm_CreateReq? req)
    {
        try
        {
            // 1. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_SubmissionFormController.cs:142)
            if (!string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = System.Text.Json.JsonSerializer.Deserialize<Mst_SubmissionForm_CreateReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        req.OrgID ??= modelObj.OrgID;
                        req.NetworkID ??= modelObj.NetworkID;
                        req.FlagActive ??= modelObj.FlagActive;
                    }
                }
                catch
                {
                    // Bỏ qua lỗi parse JSON thô
                }
            }

            // 2. Chuẩn hoá tham số đầu vào (Gate: Mst_SubmissionFormController.cs:150-171, 179-198)
            var orgId = StdParam(req?.OrgID) ?? "7200000001";
            var flagActive = !string.IsNullOrWhiteSpace(req?.FlagActive) ? req.FlagActive.Trim() : "1";

            // 3. Lấy thông tin kênh gửi Mst_Channel theo OrgID và FlagActive = '1'
            // Gate: Mst_SubmissionFormController.cs:165 Ft_WhereClause = "Mst_Channel.OrgID = " + UserState.Mst_NNT.OrgID + " and Mst_Channel.FlagActive = '1'"
            // BIZ: MasterData.cs:18441-18506
            var channelQuery = _db.Set<Mst_Channel>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);

            if (!string.IsNullOrEmpty(orgId))
            {
                channelQuery = channelQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            var lstChannel = await channelQuery.OrderBy(x => x.LogLUDTimeUTC).ToListAsync();

            // 4. Lấy chi tiết cấu hình Email, SMS, Zalo theo OrgID
            // Gate: Mst_SubmissionFormController.cs:167-170
            // BIZ: MasterData.cs:18511-18570
            var emailQuery = _db.Set<Mst_ChannelEmail>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);
            if (!string.IsNullOrEmpty(orgId))
            {
                emailQuery = emailQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            var lstEmail = await emailQuery.OrderBy(x => x.OrgID).ToListAsync();

            var smsQuery = _db.Set<Mst_ChannelSMS>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);
            if (!string.IsNullOrEmpty(orgId))
            {
                smsQuery = smsQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            var lstSMS = await smsQuery.OrderBy(x => x.OrgID).ToListAsync();

            var zaloQuery = _db.Set<Mst_ChannelZalo>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);
            if (!string.IsNullOrEmpty(orgId))
            {
                zaloQuery = zaloQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            var lstZalo = await zaloQuery.OrderBy(x => x.OrgID).ToListAsync();

            // 5. Lấy danh sách tham số mẫu gửi Mst_ParamContractSubmissForm với FlagActive = '1'
            // Gate: Mst_SubmissionFormController.cs:194 Ft_WhereClause = "Mst_ParamContractSubmissForm.FlagActive = '1'"
            // BIZ: MasterData.cs:16486 order by mpc.LogLUDTimeUTC asc
            var paramQuery = _db.Set<Mst_ParamContractSubmissForm>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);

            var lstParam = await paramQuery.OrderBy(x => x.LogLUDTimeUTC).ToListAsync();

            // 6. Dựng đối tượng RT_Mst_Channel và kết quả trả về
            // Gate: Mst_SubmissionFormController.cs:176 ViewBag.RT_Mst_Channel, :202 ViewBag.Lst_Mst_ParamContractSubmissForm, :208 return View(new RT_Mst_SubmissionForm())
            var rtChannel = new RT_Mst_Channel
            {
                Lst_Mst_Channel = lstChannel,
                Lst_Mst_ChannelEmail = lstEmail,
                Lst_Mst_ChannelSMS = lstSMS,
                Lst_Mst_ChannelZalo = lstZalo
            };

            var data = new Mst_SubmissionForm_CreateData
            {
                RT_Mst_Channel = rtChannel,
                Lst_Mst_ParamContractSubmissForm = lstParam,
                RT_Mst_ParamContractSubmissForm = new RT_Mst_ParamContractSubmissForm
                {
                    Lst_Mst_ParamContractSubmissForm = lstParam
                },
                ViewType = "create",
                RT_Mst_SubmissionForm = new RT_Mst_SubmissionForm()
            };

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_SubmissionForm_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Lấy chi tiết mẫu nội dung gửi theo SubFormCode.
    /// Gate: Mst_SubmissionFormController.cs:212-369 Detail(string subformcode)
    /// API:  MstSubmissionFormController.cs:21-89 WA_Mst_SubmissionForm_Get
    /// BIZ:
    ///   - Mst_SubmissionForm_GetX (MasterData.cs:13845-14341): Đọc Mst_SubmissionForm theo SubFormCode, kèm Mst_SubmissionFormMessage và Mst_SubmissionFormZNS (join Mst_ParamContractSubmissForm)
    ///   - Mst_Channel_GetX (MasterData.cs:18384-18780): Đọc cấu hình kênh gửi Mst_Channel, Mst_ChannelEmail, Mst_ChannelSMS, Mst_ChannelZalo theo OrgID và FlagActive = '1'
    ///   - Mst_ParamContractSubmissForm_GetX (MasterData.cs:16417-16796): Đọc danh sách tham số hợp đồng Mst_ParamContractSubmissForm với FlagActive = '1'
    ///   - Dịch vụ ngoài (GAP): ZaloTemplateService.Instance.WA_Zalo_TemplateDtl_Get và MstSv_Mst_OAService.Instance.WA_MstSv_Mst_OA_GetByOAID (bỏ qua theo quy ước P3)
    /// </summary>
    [HttpPost("detail")]
    public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_SubmissionForm_DetailReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào (Gate: Mst_SubmissionFormController.cs:212, 224-227)
            var subformcode = StdParam(req?.SubFormCode);

            var rtSubmissionForm = new RT_Mst_SubmissionFormDtl();

            // Nếu không có subformcode -> trả về form rỗng (Gate: Mst_SubmissionFormController.cs:224-227)
            if (string.IsNullOrEmpty(subformcode))
            {
                return Ok(new
                {
                    Success = true,
                    Data = new Mst_SubmissionForm_DetailData
                    {
                        RT_Mst_SubmissionForm = rtSubmissionForm,
                        RT_Mst_Channel = null,
                        Lst_Mst_ParamContractSubmissForm = new List<Mst_ParamContractSubmissForm>(),
                        RT_Mst_ParamContractSubmissForm = null,
                        RT_Zalo_TemplateDtl = null,
                        ViewType = "update"
                    }
                });
            }

            // 2. Truy vấn Mst_SubmissionForm theo SubFormCode (Gate: Mst_SubmissionFormController.cs:224-253; BIZ: MasterData.cs:14176-14185, 14226-14233)
            var formList = await _db.Set<Mst_SubmissionForm>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(subformcode), PortQuery.Esc))
                .Take(1)
                .ToListAsync();

            rtSubmissionForm.Lst_Mst_SubmissionForm = formList;

            // Nếu không tìm thấy mẫu nào -> trả về kết quả rỗng (Gate: Mst_SubmissionFormController.cs:257)
            if (formList.Count == 0)
            {
                return Ok(new
                {
                    Success = true,
                    Data = new Mst_SubmissionForm_DetailData
                    {
                        RT_Mst_SubmissionForm = rtSubmissionForm,
                        RT_Mst_Channel = null,
                        Lst_Mst_ParamContractSubmissForm = new List<Mst_ParamContractSubmissForm>(),
                        RT_Mst_ParamContractSubmissForm = null,
                        RT_Zalo_TemplateDtl = null,
                        ViewType = "update"
                    }
                });
            }

            var currentForm = formList[0];

            // 3. Truy vấn chi tiết nội dung tin nhắn Mst_SubmissionFormMessage (BIZ: MasterData.cs:14266-14276)
            var messageList = await _db.Set<Mst_SubmissionFormMessage>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(subformcode), PortQuery.Esc))
                .ToListAsync();
            rtSubmissionForm.Lst_Mst_SubmissionFormMessage = messageList;

            // 4. Truy vấn cấu hình ZNS Mst_SubmissionFormZNS kết hợp Mst_ParamContractSubmissForm (BIZ: MasterData.cs:14243-14259)
            var znsQuery = from zns in _db.Set<Mst_SubmissionFormZNS>().AsNoTracking()
                           where EF.Functions.Like((zns.SubFormCode ?? "").ToLower(), PortQuery.Equal(subformcode), PortQuery.Esc)
                           join param in _db.Set<Mst_ParamContractSubmissForm>().AsNoTracking()
                             on zns.ParamContractCode equals param.ParamContractCode into pGroup
                           from p in pGroup.DefaultIfEmpty()
                           select new Mst_SubmissionFormZNS
                           {
                               SubFormCode = zns.SubFormCode,
                               ParamContractCodeZNS = zns.ParamContractCodeZNS,
                               SourceDataType = zns.SourceDataType,
                               ParamContractCode = zns.ParamContractCode,
                               ParamValue = zns.ParamValue,
                               FlagActive = zns.FlagActive,
                               LogLUDTimeUTC = zns.LogLUDTimeUTC,
                               LogLUBy = zns.LogLUBy,
                               ParamContractName = p != null ? p.ParamContractName : null,
                               ParamContractType = p != null ? p.ParamContractType : null
                           };
            var znsList = await znsQuery.ToListAsync();
            rtSubmissionForm.Lst_Mst_SubmissionFormZNS = znsList;

            // 5. Lấy thông tin kênh gửi Mst_Channel theo OrgID và FlagActive = '1' (Gate: Mst_SubmissionFormController.cs:257-293)
            var orgId = StdParam(req?.OrgID) ?? (currentForm.OrgID ?? "7200000001");
            var flagActive = !string.IsNullOrWhiteSpace(req?.FlagActive) ? req.FlagActive.Trim() : "1";

            var channelQuery = _db.Set<Mst_Channel>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);

            if (!string.IsNullOrEmpty(orgId))
            {
                channelQuery = channelQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            var lstChannel = await channelQuery.OrderBy(x => x.LogLUDTimeUTC).ToListAsync();

            var emailQuery = _db.Set<Mst_ChannelEmail>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);
            if (!string.IsNullOrEmpty(orgId))
            {
                emailQuery = emailQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            var lstEmail = await emailQuery.OrderBy(x => x.OrgID).ToListAsync();

            var smsQuery = _db.Set<Mst_ChannelSMS>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);
            if (!string.IsNullOrEmpty(orgId))
            {
                smsQuery = smsQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            var lstSMS = await smsQuery.OrderBy(x => x.OrgID).ToListAsync();

            var zaloQuery = _db.Set<Mst_ChannelZalo>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);
            if (!string.IsNullOrEmpty(orgId))
            {
                zaloQuery = zaloQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            var lstZalo = await zaloQuery.OrderBy(x => x.OrgID).ToListAsync();

            var rtChannel = new RT_Mst_Channel
            {
                Lst_Mst_Channel = lstChannel,
                Lst_Mst_ChannelEmail = lstEmail,
                Lst_Mst_ChannelSMS = lstSMS,
                Lst_Mst_ChannelZalo = lstZalo
            };

            // 6. Lấy danh sách tham số mẫu gửi Mst_ParamContractSubmissForm với FlagActive = '1' (Gate: Mst_SubmissionFormController.cs:326-354)
            var paramQuery = _db.Set<Mst_ParamContractSubmissForm>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);
            var lstParam = await paramQuery.OrderBy(x => x.LogLUDTimeUTC).ToListAsync();

            // 7. Dựng dữ liệu trả về (Gate: Mst_SubmissionFormController.cs:360)
            var data = new Mst_SubmissionForm_DetailData
            {
                RT_Mst_SubmissionForm = rtSubmissionForm,
                RT_Mst_Channel = rtChannel,
                Lst_Mst_ParamContractSubmissForm = lstParam,
                RT_Mst_ParamContractSubmissForm = new RT_Mst_ParamContractSubmissForm
                {
                    Lst_Mst_ParamContractSubmissForm = lstParam
                },
                RT_Zalo_TemplateDtl = null, // Dịch vụ ngoài ZaloTemplateService.WA_Zalo_TemplateDtl_Get -> GAP
                ViewType = "update"
            };

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_SubmissionForm_Detail", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? value)
    {
        var s = value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpper();
    }
}

public class Mst_SubmissionForm_CreateReq
{
    public string? Model { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
}

public class Mst_SubmissionForm_CreateData
{
    public RT_Mst_Channel? RT_Mst_Channel { get; set; }
    public List<Mst_ParamContractSubmissForm> Lst_Mst_ParamContractSubmissForm { get; set; } = new();
    public RT_Mst_ParamContractSubmissForm? RT_Mst_ParamContractSubmissForm { get; set; }
    public string ViewType { get; set; } = "create";
    public RT_Mst_SubmissionForm RT_Mst_SubmissionForm { get; set; } = new();
}

public class RT_Mst_ParamContractSubmissForm
{
    public List<Mst_ParamContractSubmissForm> Lst_Mst_ParamContractSubmissForm { get; set; } = new();
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

public class Mst_SubmissionForm_DetailReq
{
    public string? SubFormCode { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
}

public class Mst_SubmissionForm_DetailData
{
    public RT_Mst_SubmissionFormDtl RT_Mst_SubmissionForm { get; set; } = new();
    public List<Mst_SubmissionForm> Lst_Mst_SubmissionForm => RT_Mst_SubmissionForm.Lst_Mst_SubmissionForm;
    public List<Mst_SubmissionFormMessage> Lst_Mst_SubmissionFormMessage => RT_Mst_SubmissionForm.Lst_Mst_SubmissionFormMessage;
    public List<Mst_SubmissionFormZNS> Lst_Mst_SubmissionFormZNS => RT_Mst_SubmissionForm.Lst_Mst_SubmissionFormZNS;
    public RT_Mst_Channel? RT_Mst_Channel { get; set; }
    public List<Mst_ParamContractSubmissForm> Lst_Mst_ParamContractSubmissForm { get; set; } = new();
    public RT_Mst_ParamContractSubmissForm? RT_Mst_ParamContractSubmissForm { get; set; }
    public object? RT_Zalo_TemplateDtl { get; set; }
    public string ViewType { get; set; } = "update";
}

public class RT_Mst_SubmissionFormDtl : RT_Mst_SubmissionForm
{
    public List<Mst_SubmissionFormMessage> Lst_Mst_SubmissionFormMessage { get; set; } = new();
    public List<Mst_SubmissionFormZNS> Lst_Mst_SubmissionFormZNS { get; set; } = new();
}
