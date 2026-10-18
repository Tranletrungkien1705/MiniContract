using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_SubmissionFormController.cs — Mst_SubmissionForm
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_SubmissionFormController.cs:23-140 (Index), 142-209 (Create), 212-369 (Detail), 372-549 (CreateCopy), 667-731 (Save), 854-873 (strWhereClauseSearchBy), 875-888 (strWhereClause_Mst_SubmissionForm)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web.DEV\Controllers\MstSubmissionFormController.cs:21-89 (WA_Mst_SubmissionForm_Get, WA_Mst_SubmissionForm_Save), MstChannelTypeController.cs:91-140 (WA_Mst_Channel_Get), MstParamContractController.cs:19-75 (WA_Mst_ParamContractSubmissForm_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\MasterData.cs:13845-14341 (Mst_SubmissionForm_GetX), 14361-14478 (WAS_Mst_SubmissionForm_Save), 14479-15187 (Mst_SubmissionForm_Upd, Mst_SubmissionForm_UpdX), 15188-15359 (Mst_SubmissionForm_Save), 15361-15868 (Mst_SubmissionForm_SaveX), 18384-18780 (Mst_Channel_GetX), 16417-16796 (Mst_ParamContractSubmissForm_GetX)
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

    /// <summary>
    /// Nhân bản cấu hình mẫu nội dung gửi.
    /// Gate: Mst_SubmissionFormController.cs:372-549 CreateCopy(string model)
    /// API:  WA_Mst_SubmissionForm_Upd, WA_Mst_Channel_Get, WA_Mst_ParamContractSubmissForm_Get
    /// BIZ:
    ///   - Mst_SubmissionForm_UpdX (MasterData.cs:14688-15187): Kiểm tra SubFormCode, CheckDB Mst_SubmissionForm, CheckDB Mst_ChannelType, CheckDB Mst_BulletinType, cập nhật Mst_SubmissionForm, xoá và chèn lại Mst_SubmissionFormZNS và Mst_SubmissionFormMessage.
    ///   - Lấy thông tin kênh gửi Mst_Channel và tham số Mst_ParamContractSubmissForm.
    ///   - Reset SubFormCode = "" để sẵn sàng nhân bản.
    /// </summary>
    [HttpPost("createcopy")]
    public async Task<IActionResult> CreateCopy([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_SubmissionForm_CreateCopyReq? req)
    {
        try
        {
            var form = req?.Mst_SubmissionForm;
            var listZns = req?.Lst_Mst_SubmissionFormZNS ?? new List<Mst_SubmissionFormZNS>();
            var listMsg = req?.Lst_Mst_SubmissionFormMessage ?? new List<Mst_SubmissionFormMessage>();

            // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_SubmissionFormController.cs:372, 386)
            if (!string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = System.Text.Json.JsonSerializer.Deserialize<Mst_SubmissionForm_CreateCopyReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        if (modelObj.Mst_SubmissionForm != null)
                        {
                            form = modelObj.Mst_SubmissionForm;
                        }
                        if (modelObj.Lst_Mst_SubmissionFormZNS != null && modelObj.Lst_Mst_SubmissionFormZNS.Count > 0)
                        {
                            listZns = modelObj.Lst_Mst_SubmissionFormZNS;
                        }
                        if (modelObj.Lst_Mst_SubmissionFormMessage != null && modelObj.Lst_Mst_SubmissionFormMessage.Count > 0)
                        {
                            listMsg = modelObj.Lst_Mst_SubmissionFormMessage;
                        }
                        req.OrgID ??= modelObj.OrgID;
                        req.NetworkID ??= modelObj.NetworkID;
                    }
                }
                catch
                {
                    // Bỏ qua lỗi parse JSON thô
                }
            }

            // 1. Kiểm tra strSubFormCode (MasterData.cs:14806, 14819-14828)
            var strSubFormCode = StdParam(form?.SubFormCode);
            if (string.IsNullOrEmpty(strSubFormCode))
            {
                return PortResult.Fail("Mst_SubmissionForm_SaveX_InvalidSubFormCode", "Check.strSubFormCode", strSubFormCode);
            }

            // 2. Kiểm tra tồn tại trong DB (MasterData.cs:14831-14837, 13805-13814: Mst_SubmissionForm_CheckDB)
            var dbForm = await _db.Set<Mst_SubmissionForm>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(strSubFormCode), PortQuery.Esc));
            if (dbForm == null)
            {
                return PortResult.Fail("Mst_SubmissionForm_CheckDB_SubFormCodeNotFound", "Check.SubFormCode", strSubFormCode);
            }

            // 3. Kiểm tra loại kênh (MasterData.cs:14843-14849, 17883-17915: Mst_ChannelType_CheckDB)
            var strChannelType = StdParam(form?.ChannelType);
            if (string.IsNullOrEmpty(strChannelType))
            {
                return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelType);
            }
            var channelTypeRow = await _db.Set<Mst_ChannelType>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ChannelType ?? "").ToLower(), PortQuery.Equal(strChannelType), PortQuery.Esc));
            if (channelTypeRow == null)
            {
                return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelType);
            }
            if (channelTypeRow.FlagActive != "1")
            {
                return PortResult.Fail("Mst_ChannelType_CheckDB_FlagActiveNotMatched", "Check.ChannelType", strChannelType);
            }

            // 4. Kiểm tra loại bản tin (MasterData.cs:14853-14859, 17815-17852: Mst_BulletinType_CheckDB)
            var strBulletinType = StdParam(form?.BulletinType);
            if (string.IsNullOrEmpty(strBulletinType))
            {
                return PortResult.Fail("Mst_BulletinType_CheckDB_BulletinTypeNotFound", "Check.BulletinType", strBulletinType);
            }
            var bulletinTypeRow = await _db.Set<Mst_BulletinType>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.BulletinType ?? "").ToLower(), PortQuery.Equal(strBulletinType), PortQuery.Esc));
            if (bulletinTypeRow == null)
            {
                return PortResult.Fail("Mst_BulletinType_CheckDB_BulletinTypeNotFound", "Check.BulletinType", strBulletinType);
            }
            if (bulletinTypeRow.FlagActive != "1")
            {
                return PortResult.Fail("Mst_BulletinType_CheckDB_FlagActiveNotMatched", "Check.BulletinType", strBulletinType);
            }

            // 5. Cập nhật dữ liệu trong Transaction (MasterData.cs:14659, 15038-15161)
            var orgId = StdParam(form?.OrgID) ?? StdParam(req?.OrgID) ?? (dbForm.OrgID ?? "7200000001");
            var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            using (var tx = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    // Update Mst_SubmissionForm (MasterData.cs:15066-15089)
                    dbForm.SubFormName = form?.SubFormName ?? dbForm.SubFormName;
                    dbForm.ChannelType = strChannelType;
                    dbForm.BulletinType = strBulletinType;
                    dbForm.IDZNS = StdParam(form?.IDZNS);
                    dbForm.FlagActive = StdFlag(form?.FlagActive);
                    dbForm.LogLUDTimeUTC = nowStr;
                    dbForm.LogLUBy = "port";

                    // Clear and insert Mst_SubmissionFormZNS (MasterData.cs:15041-15048, 15091-15113)
                    var oldZns = await _db.Set<Mst_SubmissionFormZNS>()
                        .Where(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(strSubFormCode), PortQuery.Esc))
                        .ToListAsync();
                    if (oldZns.Count > 0)
                    {
                        _db.Set<Mst_SubmissionFormZNS>().RemoveRange(oldZns);
                    }

                    if (listZns.Count > 0)
                    {
                        var newZns = listZns.Select(z => new Mst_SubmissionFormZNS
                        {
                            SubFormCode = strSubFormCode,
                            ParamContractCodeZNS = StdStr(z.ParamContractCodeZNS) ?? "",
                            SourceDataType = StdParam(z.SourceDataType),
                            ParamContractCode = StdParam(z.ParamContractCode),
                            ParamValue = z.ParamValue,
                            FlagActive = "1",
                            LogLUDTimeUTC = nowStr,
                            LogLUBy = "port"
                        }).ToList();
                        await _db.Set<Mst_SubmissionFormZNS>().AddRangeAsync(newZns);
                    }

                    // Clear and insert Mst_SubmissionFormMessage (MasterData.cs:15050-15057, 15115-15135)
                    var oldMsg = await _db.Set<Mst_SubmissionFormMessage>()
                        .Where(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(strSubFormCode), PortQuery.Esc))
                        .ToListAsync();
                    if (oldMsg.Count > 0)
                    {
                        _db.Set<Mst_SubmissionFormMessage>().RemoveRange(oldMsg);
                    }

                    if (listMsg.Count > 0)
                    {
                        var newMsg = listMsg.Select(m => new Mst_SubmissionFormMessage
                        {
                            SubFormCode = strSubFormCode,
                            Message = m.Message,
                            FlagActive = "1",
                            LogLUDTimeUTC = nowStr,
                            LogLUBy = "port"
                        }).ToList();
                        await _db.Set<Mst_SubmissionFormMessage>().AddRangeAsync(newMsg);
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception)
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }

            // 6. Chuẩn bị đối tượng RT_Mst_SubmissionForm và xoá SubFormCode cho CreateCopy
            // Gate: Mst_SubmissionFormController.cs:430-441:
            // objRT_Mst_SubmissionForm.Lst_Mst_SubmissionForm[0].SubFormCode = "";
            var copiedForm = new Mst_SubmissionForm
            {
                SubFormCode = "", // Xoá mã để làm mẫu mới
                SubFormName = form?.SubFormName ?? dbForm.SubFormName,
                OrgID = orgId,
                ChannelType = strChannelType,
                BulletinType = strBulletinType,
                IDZNS = StdParam(form?.IDZNS),
                FlagActive = StdFlag(form?.FlagActive),
                LogLUDTimeUTC = nowStr,
                LogLUBy = "port"
            };

            // 7. Lấy danh sách kênh Mst_Channel theo OrgID và FlagActive = '1' (Gate: Mst_SubmissionFormController.cs:444-476)
            var flagActive = "1";
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

            // 8. Lấy danh sách tham số hợp đồng Mst_ParamContractSubmissForm với FlagActive = '1' (Gate: Mst_SubmissionFormController.cs:509-537)
            var paramQuery = _db.Set<Mst_ParamContractSubmissForm>()
                .AsNoTracking()
                .Where(x => x.FlagActive == flagActive);
            var lstParam = await paramQuery.OrderBy(x => x.LogLUDTimeUTC).ToListAsync();

            // 9. Dựng dữ liệu trả về (Gate: Mst_SubmissionFormController.cs:541: JsonView("Create", objRT_Mst_SubmissionForm))
            var data = new Mst_SubmissionForm_CreateCopyData
            {
                RT_Mst_SubmissionForm = new RT_Mst_SubmissionFormDtl
                {
                    Lst_Mst_SubmissionForm = new List<Mst_SubmissionForm> { copiedForm },
                    Lst_Mst_SubmissionFormMessage = listMsg,
                    Lst_Mst_SubmissionFormZNS = listZns
                },
                RT_Mst_Channel = rtChannel,
                Lst_Mst_ParamContractSubmissForm = lstParam,
                RT_Mst_ParamContractSubmissForm = new RT_Mst_ParamContractSubmissForm
                {
                    Lst_Mst_ParamContractSubmissForm = lstParam
                },
                RT_Zalo_TemplateDtl = null, // Dịch vụ ngoài -> GAP
                ViewType = "createcopy"
            };

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_SubmissionForm_CreateCopy", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Lưu mới / xoá cấu hình mẫu nội dung gửi.
    /// Gate: Mst_SubmissionFormController.cs:667-731 Save(string model)
    /// API:  WA_Mst_SubmissionForm_Save
    /// BIZ:
    ///   - WAS_Mst_SubmissionForm_Save (MasterData.cs:14361-14478): Đóng gói Lst_Mst_SubmissionFormZNS và Lst_Mst_SubmissionFormMessage vào dsData.
    ///   - Mst_SubmissionForm_Save (MasterData.cs:15188-15359): Transaction, CheckAuthorize, gọi Mst_SubmissionForm_SaveX.
    ///   - Mst_SubmissionForm_SaveX (MasterData.cs:15361-15868): Kiểm tra SubFormCode, CheckDB Mst_SubmissionForm (chặn trùng khi tạo mới), CheckDB Mst_ChannelType, CheckDB Mst_BulletinType, CheckDB Mst_ParamContractSubmissForm cho từng mục ZNS, xoá dữ liệu cũ và chèn mới vào Mst_SubmissionForm, Mst_SubmissionFormZNS, Mst_SubmissionFormMessage.
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_SubmissionForm_SaveReq? req)
    {
        try
        {
            var form = req?.Mst_SubmissionForm;
            var listZns = req?.Lst_Mst_SubmissionFormZNS ?? new List<Mst_SubmissionFormZNS>();
            var listMsg = req?.Lst_Mst_SubmissionFormMessage ?? new List<Mst_SubmissionFormMessage>();
            var flagIsDelete = req?.FlagIsDelete;

            // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_SubmissionFormController.cs:667, 679)
            if (!string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = System.Text.Json.JsonSerializer.Deserialize<Mst_SubmissionForm_SaveReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        if (modelObj.Mst_SubmissionForm != null)
                        {
                            form = modelObj.Mst_SubmissionForm;
                        }
                        if (modelObj.Lst_Mst_SubmissionFormZNS != null && modelObj.Lst_Mst_SubmissionFormZNS.Count > 0)
                        {
                            listZns = modelObj.Lst_Mst_SubmissionFormZNS;
                        }
                        if (modelObj.Lst_Mst_SubmissionFormMessage != null && modelObj.Lst_Mst_SubmissionFormMessage.Count > 0)
                        {
                            listMsg = modelObj.Lst_Mst_SubmissionFormMessage;
                        }
                        flagIsDelete ??= modelObj.FlagIsDelete;
                        req.OrgID ??= modelObj.OrgID;
                        req.NetworkID ??= modelObj.NetworkID;
                    }
                }
                catch
                {
                    // Bỏ qua lỗi parse JSON thô
                }
            }

            // Hỗ trợ client gửi các trường phẳng thay vì lồng trong Mst_SubmissionForm
            if (form == null && (!string.IsNullOrEmpty(req?.SubFormCode) || !string.IsNullOrEmpty(req?.SubFormName)))
            {
                form = new Mst_SubmissionForm
                {
                    SubFormCode = req?.SubFormCode ?? "",
                    SubFormName = req?.SubFormName,
                    OrgID = req?.OrgID,
                    ChannelType = req?.ChannelType,
                    BulletinType = req?.BulletinType,
                    IDZNS = req?.IDZNS,
                    FlagActive = req?.FlagActive
                };
            }

            var bIsDelete = string.Equals(flagIsDelete?.Trim(), "1", StringComparison.OrdinalIgnoreCase);

            // 1. Kiểm tra strSubFormCode (MasterData.cs:15435-15445)
            var strSubFormCode = StdParam(form?.SubFormCode);
            if (string.IsNullOrEmpty(strSubFormCode))
            {
                return PortResult.Fail("Mst_SubmissionForm_SaveX_InvalidSubFormCode", "Check.strSubFormCode", strSubFormCode);
            }

            // 2. Kiểm tra tồn tại trong DB (MasterData.cs:15447-15486)
            var dbForm = await _db.Set<Mst_SubmissionForm>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(strSubFormCode), PortQuery.Esc));

            if (dbForm == null)
            {
                if (bIsDelete)
                {
                    // Thoát sớm thành công nếu xóa bản ghi không tồn tại (MasterData.cs:15457-15460)
                    return Ok(new
                    {
                        Success = true,
                        RedirectUrl = "/Mst_SubmissionForm",
                        Messages = new[] { "Lưu mẫu gửi thành công!" },
                        Title = "Lưu mẫu gửi thành công!"
                    });
                }
            }
            else
            {
                if (!bIsDelete)
                {
                    // Nếu đã tồn tại mà không phải xoá -> quăng lỗi SubFormCodeExisted (MasterData.cs:15468-15478)
                    return PortResult.Fail("Mst_SubmissionForm_SaveX_SubFormCodeExisted", "Check.DB.SubFormCode", strSubFormCode);
                }
            }

            var strChannelType = StdParam(form?.ChannelType);
            var strBulletinType = StdParam(form?.BulletinType);
            var strOrgID = StdParam(form?.OrgID) ?? StdParam(req?.OrgID) ?? (dbForm?.OrgID ?? "7200000001");

            if (!bIsDelete)
            {
                // 3. Kiểm tra loại kênh (MasterData.cs:15494-15500, 17857-17919: Mst_ChannelType_CheckDB)
                if (string.IsNullOrEmpty(strChannelType))
                {
                    return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelType);
                }
                var channelTypeRow = await _db.Set<Mst_ChannelType>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.ChannelType ?? "").ToLower(), PortQuery.Equal(strChannelType), PortQuery.Esc));
                if (channelTypeRow == null)
                {
                    return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelType);
                }
                if (channelTypeRow.FlagActive != "1")
                {
                    return PortResult.Fail("Mst_ChannelType_CheckDB_FlagActiveNotMatched", "Check.ChannelType", strChannelType, "Check.FlagActiveListToCheck", "1", "DB.FlagActive", channelTypeRow.FlagActive);
                }

                // 4. Kiểm tra loại bản tin (MasterData.cs:15504-15510, 17789-17852: Mst_BulletinType_CheckDB)
                if (string.IsNullOrEmpty(strBulletinType))
                {
                    return PortResult.Fail("Mst_BulletinType_CheckDB_BulletinTypeNotFound", "Check.BulletinType", strBulletinType);
                }
                var bulletinTypeRow = await _db.Set<Mst_BulletinType>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.BulletinType ?? "").ToLower(), PortQuery.Equal(strBulletinType), PortQuery.Esc));
                if (bulletinTypeRow == null)
                {
                    return PortResult.Fail("Mst_BulletinType_CheckDB_BulletinTypeNotFound", "Check.BulletinType", strBulletinType);
                }
                if (bulletinTypeRow.FlagActive != "1")
                {
                    return PortResult.Fail("Mst_BulletinType_CheckDB_FlagActiveNotMatched", "Check.BulletinType", strBulletinType, "Check.FlagActiveListToCheck", "1", "DB.FlagActive", bulletinTypeRow.FlagActive);
                }

                // 5. Kiểm tra các dòng ZNS (MasterData.cs:15551-15609, 16352-16415: Mst_ParamContractSubmissForm_CheckDB)
                foreach (var z in listZns)
                {
                    var paramCode = StdParam(z.ParamContractCode);
                    if (string.IsNullOrEmpty(paramCode))
                    {
                        return PortResult.Fail("Mst_ParamContractSubmissForm_CheckDB_ParamContractCodeNotFound", "Check.ParamContractCode", paramCode);
                    }
                    var paramRow = await _db.Set<Mst_ParamContractSubmissForm>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => EF.Functions.Like((x.ParamContractCode ?? "").ToLower(), PortQuery.Equal(paramCode), PortQuery.Esc));
                    if (paramRow == null)
                    {
                        return PortResult.Fail("Mst_ParamContractSubmissForm_CheckDB_ParamContractCodeNotFound", "Check.ParamContractCode", paramCode);
                    }
                    if (paramRow.FlagActive != "1")
                    {
                        return PortResult.Fail("Mst_ParamContractSubmissForm_CheckDB_FlagActiveNotMatched", "Check.ParamContractCode", paramCode, "Check.FlagActiveListToCheck", "1", "DB.FlagActive", paramRow.FlagActive);
                    }
                }
            }

            // 6. Cập nhật dữ liệu trong Transaction (MasterData.cs:15239, 15701-15842)
            var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            using (var tx = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    // Xóa dữ liệu cũ (MasterData.cs:15705-15734)
                    var oldMsg = await _db.Set<Mst_SubmissionFormMessage>()
                        .Where(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(strSubFormCode), PortQuery.Esc))
                        .ToListAsync();
                    if (oldMsg.Count > 0)
                    {
                        _db.Set<Mst_SubmissionFormMessage>().RemoveRange(oldMsg);
                    }

                    var oldZns = await _db.Set<Mst_SubmissionFormZNS>()
                        .Where(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(strSubFormCode), PortQuery.Esc))
                        .ToListAsync();
                    if (oldZns.Count > 0)
                    {
                        _db.Set<Mst_SubmissionFormZNS>().RemoveRange(oldZns);
                    }

                    var oldForm = await _db.Set<Mst_SubmissionForm>()
                        .Where(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(strSubFormCode), PortQuery.Esc))
                        .ToListAsync();
                    if (oldForm.Count > 0)
                    {
                        _db.Set<Mst_SubmissionForm>().RemoveRange(oldForm);
                    }

                    // Chèn dữ liệu mới nếu !bIsDelete (MasterData.cs:15743-15817)
                    if (!bIsDelete)
                    {
                        var newForm = new Mst_SubmissionForm
                        {
                            SubFormCode = strSubFormCode,
                            SubFormName = form?.SubFormName ?? "",
                            OrgID = strOrgID,
                            ChannelType = strChannelType,
                            BulletinType = strBulletinType,
                            IDZNS = StdParam(form?.IDZNS),
                            FlagActive = StdFlag(form?.FlagActive),
                            LogLUDTimeUTC = nowStr,
                            LogLUBy = "port"
                        };
                        await _db.Set<Mst_SubmissionForm>().AddAsync(newForm);

                        if (listZns.Count > 0)
                        {
                            var newZns = listZns.Select(z => new Mst_SubmissionFormZNS
                            {
                                SubFormCode = strSubFormCode,
                                ParamContractCodeZNS = StdStr(z.ParamContractCodeZNS) ?? "",
                                SourceDataType = StdParam(z.SourceDataType),
                                ParamContractCode = StdParam(z.ParamContractCode),
                                ParamValue = z.ParamValue,
                                FlagActive = "1",
                                LogLUDTimeUTC = nowStr,
                                LogLUBy = "port"
                            }).ToList();
                            await _db.Set<Mst_SubmissionFormZNS>().AddRangeAsync(newZns);
                        }

                        if (listMsg.Count > 0)
                        {
                            var newMsg = listMsg.Select(m => new Mst_SubmissionFormMessage
                            {
                                SubFormCode = strSubFormCode,
                                Message = m.Message,
                                FlagActive = "1",
                                LogLUDTimeUTC = nowStr,
                                LogLUBy = "port"
                            }).ToList();
                            await _db.Set<Mst_SubmissionFormMessage>().AddRangeAsync(newMsg);
                        }
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception)
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }

            // 7. Trả kết quả thành công (Gate: Mst_SubmissionFormController.cs:722-724)
            return Ok(new
            {
                Success = true,
                RedirectUrl = "/Mst_SubmissionForm",
                Messages = new[] { "Lưu mẫu gửi thành công!" },
                Title = "Lưu mẫu gửi thành công!"
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_SubmissionForm_Save", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? value)
    {
        var s = value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpper();
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

public class Mst_SubmissionForm_CreateCopyReq
{
    public string? Model { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }

    public Mst_SubmissionForm? Mst_SubmissionForm { get; set; }
    public List<Mst_SubmissionFormZNS>? Lst_Mst_SubmissionFormZNS { get; set; }
    public List<Mst_SubmissionFormMessage>? Lst_Mst_SubmissionFormMessage { get; set; }
}

public class Mst_SubmissionForm_CreateCopyData
{
    public RT_Mst_SubmissionFormDtl RT_Mst_SubmissionForm { get; set; } = new();
    public List<Mst_SubmissionForm> Lst_Mst_SubmissionForm => RT_Mst_SubmissionForm.Lst_Mst_SubmissionForm;
    public List<Mst_SubmissionFormMessage> Lst_Mst_SubmissionFormMessage => RT_Mst_SubmissionForm.Lst_Mst_SubmissionFormMessage;
    public List<Mst_SubmissionFormZNS> Lst_Mst_SubmissionFormZNS => RT_Mst_SubmissionForm.Lst_Mst_SubmissionFormZNS;
    public RT_Mst_Channel? RT_Mst_Channel { get; set; }
    public List<Mst_ParamContractSubmissForm> Lst_Mst_ParamContractSubmissForm { get; set; } = new();
    public RT_Mst_ParamContractSubmissForm? RT_Mst_ParamContractSubmissForm { get; set; }
    public object? RT_Zalo_TemplateDtl { get; set; }
    public string ViewType { get; set; } = "createcopy";
}

public class Mst_SubmissionForm_SaveReq
{
    public string? Model { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
    public string? FlagIsDelete { get; set; }

    public string? SubFormCode { get; set; }
    public string? SubFormName { get; set; }
    public string? ChannelType { get; set; }
    public string? BulletinType { get; set; }
    public string? IDZNS { get; set; }

    public Mst_SubmissionForm? Mst_SubmissionForm { get; set; }
    public List<Mst_SubmissionFormZNS>? Lst_Mst_SubmissionFormZNS { get; set; }
    public List<Mst_SubmissionFormMessage>? Lst_Mst_SubmissionFormMessage { get; set; }
}
