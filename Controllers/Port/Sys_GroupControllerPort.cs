using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Sys_GroupController.cs — Sys_Group
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:22-102 (Index)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:107-156 (GetDetailData)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:161-171 (Create GET)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:173-213 (Create POST)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:218-266 (Update)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:270-341 (Edit)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:346-394 (Detail)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:398-470 (Delete)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:477-567 (GetSysUser)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:1080-1091 (strWhereClause_SysGroup)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:1093-1104 (strWhereClause_SysGroupName)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:1119-1138 (strWhereClause_SysUser)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysGroupController.cs:20-91 (WA_Sys_Group_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysGroupController.cs:93-147 (WA_Sys_Group_Create)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysGroupController.cs:168-239 (WA_Sys_Group_Update)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysGroupController.cs:242-313 (WA_Sys_Group_Delete)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysUserController.cs:199-270 (WA_Sys_User_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:7011-7123 (WAS_Sys_User_Get) -> 6635-7009 (Sys_User_Get_New20191104)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:2283-2397 (WAS_Sys_Group_Get) -> 1966-2281 (Sys_Group_Get_New20191102)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:2710-2790 (WAS_Sys_Group_Create) -> zTemp.cs:1040-1235 (Sys_Group_Create_New20191102)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:3118-3201 (WAS_Sys_Group_Update) -> 2937-3116 / 2791-2935 (Sys_Group_Update)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:3469-3548 (WAS_Sys_Group_Delete) -> 3318-3467 (Sys_Group_Delete), 13768-13783 (Sys_UserInGroup_Delete_ByGroup)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:1341-1400 (Sys_Group_CheckDB)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:3552-3611 (Sys_User_CheckDB)
[ApiController]
[Route("api/sys_group")]
public class Sys_GroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    // Hằng số che mật khẩu từ D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Constants\Const.Main.BE.cs:13
    private const string Default_PasswordMask = "*********";

    public Sys_GroupControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Sys_GroupController.cs:22-102.
    /// Chuỗi gọi:
    /// 1. SysAccessCheckDeny(UserState, "MENU_QL_NHOM_NGUOI_DUNG") (gate dòng 24-28).
    /// 2. Gán init = "search" (gate dòng 33).
    /// 3. Khởi tạo PageInfo&lt;Sys_Group&gt; với itemCount = 0, pageCount = 0 (gate dòng 40-45).
    /// 4. Dựng strWhereClause theo Sys_Group.GroupName: strWhereClause_SysGroupName (gate dòng 49-55, 1093-1104).
    /// 5. Gọi Sys_GroupService.Instance.WA_Sys_Group_Get (gate dòng 79)
    ///    -> API WA_Sys_Group_Get (SysGroupController.cs:20-91)
    ///    -> BIZ WAS_Sys_Group_Get (System.cs:2283-2397)
    ///    -> Sys_Group_Get_New20191102 (System.cs:1966-2281).
    ///    SQL: select distinct sg.GroupCode into #tbl_Sys_Group_Filter_Draft from Sys_Group sg ... order by sg.GroupCode asc.
    ///    Phân trang: #tbl_Sys_Group_Filter với MyIdxSeq between nFilterRecordStart and nFilterRecordEnd.
    ///    Lấy dữ liệu: select t.MyIdxSeq, sg.* from #tbl_Sys_Group_Filter t inner join Sys_Group sg on t.GroupCode = sg.GroupCode order by t.MyIdxSeq asc.
    /// 6. Gán pageInfo.DataList, pageInfo.ItemCount, pageInfo.PageCount (gate dòng 80-88, 96-99).
    /// 7. Trả về View(pageInfo) (gate dòng 101).
    /// </summary>
    [HttpPost("index")]
    [HttpPost("search")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Sys_GroupController.cs:22, 69-70
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            // 2. Chuẩn hoá tham số tìm kiếm (LESSONS A.14, A.21, PORT_RULES §3)
            var groupName = StdStr(req?.GroupName);
            var groupCode = StdParam(req?.GroupCode);
            var flagActive = req?.FlagActive != null ? StdFlag(req.FlagActive) : null;
            var mst = StdParam(req?.MST);
            var keyWord = StdStr(req?.KeyWord);

            // 3. Dựng truy vấn cơ sở từ bảng Sys_Group (System.cs:2081, 2133)
            var query = _db.Set<Sys_Group>().AsNoTracking();

            // 4. Lọc theo GroupName (Gate dòng 49-54, 1093-1104: strWhereClause_SysGroupName)
            if (!string.IsNullOrEmpty(groupName))
            {
                query = query.Where(x => EF.Functions.Like((x.GroupName ?? "").ToLower(), PortQuery.Contains(groupName), PortQuery.Esc));
            }

            // Lọc theo GroupCode nếu có (System.cs:2181-2184 htSpCols)
            if (!string.IsNullOrEmpty(groupCode))
            {
                query = query.Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));
            }

            // Lọc theo FlagActive nếu có (System.cs:2181-2184 htSpCols)
            if (!string.IsNullOrEmpty(flagActive))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            // Lọc theo MST nếu có (System.cs:2083: inner join #tbl_Mst_NNT_ViewAbility on sg.MST = t_MstNNT_View.MST)
            if (!string.IsNullOrEmpty(mst))
            {
                query = query.Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
            }

            // Lọc theo KeyWord nếu có
            if (!string.IsNullOrEmpty(keyWord))
            {
                query = query.Where(x => EF.Functions.Like((x.GroupName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)
                                      || EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc));
            }

            // 5. Sắp xếp: order by sg.GroupCode asc (System.cs:2091)
            query = query.OrderBy(x => x.GroupCode);

            // 6. Tổng số dòng (Summary: select Count(0) MyCount from #tbl_Sys_Group_Filter_Draft, System.cs:2095)
            var itemCount = await query.CountAsync();

            // 7. Phân trang theo mẫu chuẩn LESSON A.12, A.7
            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Sys_Group>()
                : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                             .Take((int)Math.Min(recordCount, int.MaxValue))
                             .ToListAsync();

            // 8. Tính PageCount: khi rỗng hoặc itemCount == 0 thì PageCount = 0 (Gate dòng 45), có dòng thì tính theo cỡ trang (Gate dòng 87)
            var pageCount = 0;
            if (list.Count > 0 && pageSize > 0)
            {
                pageCount = (itemCount % pageSize == 0) ? (itemCount / pageSize) : (itemCount / pageSize + 1);
            }

            var pageInfo = new PageInfo<Sys_Group>(list, itemCount, pageCount, pageIndex, pageSize);

            return Ok(new
            {
                Success = true,
                Data = pageInfo
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Group_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action GetDetailData từ gate Sys_GroupController.cs:107-156.
    /// Chuỗi gọi:
    /// 1. Dựng điều kiện strWhereClause_SysGroup theo groupcode:
    ///    Sys_Group.GroupCode = groupcode (gate dòng 118-119, 1080-1091).
    /// 2. Đóng gói RQ_Sys_Group với Rt_Cols_Sys_Group = null, Rt_Cols_Sys_UserInGroup = "*" (gate dòng 121-141).
    /// 3. Gọi Sys_GroupService.Instance.WA_Sys_Group_Get (gate dòng 143)
    ///    -> API WA_Sys_Group_Get (SysGroupController.cs:20-91)
    ///    -> BIZ WAS_Sys_Group_Get (System.cs:2283-2397)
    ///    -> Sys_Group_Get_New20191102 (System.cs:1966-2281).
    ///    SQL: Lọc nhóm qua #tbl_Sys_Group_Filter_Draft và #tbl_Sys_Group_Filter (System.cs:2076-2106).
    ///    Lấy dữ liệu người dùng trong nhóm qua inner join Sys_UserInGroup suig on t.GroupCode = suig.GroupCode
    ///    left join Sys_User su on suig.UserCode = su.UserCode
    ///    left join Sys_Group sg on suig.GroupCode = sg.GroupCode (System.cs:2146-2167).
    /// 4. Nếu có kết quả thì listUserInGroup.AddRange(objRT_Sys_GroupCur.Lst_Sys_UserInGroup) (gate dòng 144-147).
    /// 5. Trả về JsonView("ShowDetailData", listUserInGroup) (gate dòng 148).
    /// 6. Bắt ngoại lệ catch (Exception ex) -> PortResult.Fail("Sys_Group_Get", "Exception", ex.Message) (gate dòng 150-155).
    /// </summary>
    [HttpPost("getdetaildata")]
    public async Task<IActionResult> GetDetailData([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_GetDetailDataReq? req)
    {
        try
        {
            var groupCode = StdParam(req?.GroupCode);
            if (string.IsNullOrEmpty(groupCode) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("groupcode", out var qGroupCode))
            {
                groupCode = StdParam(qGroupCode.ToString());
            }

            var mst = StdParam(req?.MST);
            if (string.IsNullOrEmpty(mst) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("mst", out var qMst))
            {
                mst = StdParam(qMst.ToString());
            }

            var groupQuery = _db.Set<Sys_Group>().AsNoTracking();
            if (!string.IsNullOrEmpty(groupCode))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(mst))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
            }

            var groups = groupQuery.OrderBy(x => x.GroupCode);

            var query = from t in groups
                        join suig in _db.Set<Sys_UserInGroup>().AsNoTracking() on t.GroupCode equals suig.GroupCode
                        join su in _db.Set<Sys_User>().AsNoTracking() on suig.UserCode equals su.UserCode into gUser
                        from su in gUser.DefaultIfEmpty()
                        join sg in _db.Set<Sys_Group>().AsNoTracking() on suig.GroupCode equals sg.GroupCode into gGroup
                        from sg in gGroup.DefaultIfEmpty()
                        orderby t.GroupCode, suig.UserCode
                        select new Sys_UserInGroup
                        {
                            GroupCode = suig.GroupCode,
                            UserCode = suig.UserCode,
                            MST = suig.MST ?? (sg != null ? sg.MST : null),
                            LogLUDTimeUTC = suig.LogLUDTimeUTC,
                            LogLUBy = suig.LogLUBy,
                            su_UserCode = su != null ? su.UserCode : suig.UserCode,
                            su_UserName = su != null ? su.UserName : null,
                            su_FlagSysAdmin = su != null ? su.FlagSysAdmin : null,
                            su_FlagActive = su != null ? su.FlagActive : null,
                            sg_GroupCode = sg != null ? sg.GroupCode : suig.GroupCode,
                            sg_GroupName = sg != null ? sg.GroupName : null,
                            sg_FlagActive = sg != null ? sg.FlagActive : null,
                        };

            var listUserInGroup = await query.ToListAsync();

            return Ok(new
            {
                Success = true,
                Data = listUserInGroup
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Group_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Create từ gate Sys_GroupController.cs:161-171 (GET), 173-213 (POST).
    /// Chuỗi gọi:
    /// 1. SysAccessCheckDeny(UserState, "MENU_QL_NHOM_NGUOI_DUNG_TAO_MOI") (gate dòng 163-167).
    /// 2. Đọc UserState, deserialize model -> Sys_Group, đóng gói RQ_Sys_Group (gate dòng 178-205).
    /// 3. Gọi Sys_GroupService.Instance.WA_Sys_Group_Create(objRQ_Sys_Group, addressAPIs) (gate dòng 207)
    ///    -> API WA_Sys_Group_Create (SysGroupController.cs:93-147)
    ///    -> BIZ WAS_Sys_Group_Create (System.cs:2710-2790)
    ///    -> Sys_Group_Create_New20191102 (zTemp.cs:1040-1235).
    /// 4. Transaction: _cf.db.BeginTransaction() (zTemp.cs:1078).
    /// 5. Chuẩn hoá tham số đầu vào:
    ///    strGroupCode = StdParam(objGroupCode) (zTemp.cs:1119).
    ///    strGroupName = string.Format("{0}", objGroupName).Trim() (zTemp.cs:1120).
    /// 6. Guards & kiểm tra dữ liệu:
    ///    - Nếu strGroupCode null hoặc rỗng -> ném Sys_Group_Create_InvalidGroupCode (zTemp.cs:1126-1136).
    ///    - Gọi Sys_Group_CheckDB(..., FlagExistToCheck = "0", ...) -> nếu đã có -> ném Sys_Group_CheckDB_GroupCodeExist (System.cs:1372-1382).
    ///    - Nếu strGroupName.Length < 1 -> ném Sys_Group_Create_InvalidGroupName (zTemp.cs:1145-1155).
    ///    - Gọi Sys_User_CheckDB(..., WAUserCode, FlagExist = "1", FlagActive = "1", ...) ->
    ///      nếu không thấy user -> ném Sys_User_CheckDB_UserCodeNotFound (System.cs:3572-3582).
    ///      nếu user FlagActive != "1" -> ném Sys_User_CheckDB_FlagActiveNotMatched (System.cs:3597-3609).
    ///      Lấy MST = StdParam(user.MST) (zTemp.cs:1167).
    /// 7. Thêm mới bản ghi Sys_Group (zTemp.cs:1178-1194):
    ///    GroupCode = strGroupCode,
    ///    GroupName = strGroupName,
    ///    MST = strMST,
    ///    FlagActive = "1" (TConst.Flag.Yes),
    ///    LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
    ///    LogLUBy = strWAUserCode.
    ///    Commit transaction (zTemp.cs:1198).
    /// 8. Trả kết quả thành công khớp gate MVC (Sys_GroupController.cs:209-212):
    ///    Success = true, Messages = ["Tạo nhóm người dùng thành công!"], RedirectUrl = Url.Action("Index").
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_CreateReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc null (LESSON A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Sys_Group_Create", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Sys_GroupController.cs:175, 183)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = System.Text.Json.JsonSerializer.Deserialize<Sys_Group_CreateReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.GroupCode ??= modelObj.GroupCode;
                    req.GroupName ??= modelObj.GroupName;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.MST ??= modelObj.MST;
                    req.UserCode ??= modelObj.UserCode;
                    req.WAUserCode ??= modelObj.WAUserCode;
                    req.NetworkID ??= modelObj.NetworkID;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.GroupCode) && string.IsNullOrWhiteSpace(req.GroupName) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Sys_Group_Create", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào (zTemp.cs:1119-1121)
        var groupCode = StdParam(req.GroupCode);
        if (string.IsNullOrEmpty(groupCode))
        {
            // zTemp.cs:1126-1136
            return PortResult.Fail("Sys_Group_Create_InvalidGroupCode", "Check.strGroupCode", req.GroupCode);
        }

        // Mở transaction (zTemp.cs:1078)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 3. Sys_Group_CheckDB: Kiểm tra nhóm đã tồn tại chưa (zTemp.cs:1137-1143, System.cs:1372-1382)
            var existingGroup = await _db.Set<Sys_Group>()
                .AnyAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));
            if (existingGroup)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_CheckDB_GroupCodeExist", "Check.GroupCodeExist", groupCode);
            }

            // 4. Kiểm tra tên nhóm không được rỗng (zTemp.cs:1145-1155)
            var rawGroupName = req.GroupName != null ? req.GroupName.Trim() : "";
            if (rawGroupName.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_Create_InvalidGroupName", "Check.strGroupName", req.GroupName ?? "");
            }
            var groupName = rawGroupName;

            // 5. Xác định người dùng thực hiện và Sys_User_CheckDB (zTemp.cs:1159-1168, System.cs:3572-3609)
            var userCode = Request?.Headers["WAUserCode"].FirstOrDefault() ?? req.WAUserCode ?? req.UserCode;
            if (string.IsNullOrWhiteSpace(userCode))
            {
                userCode = "U01";
            }

            var dbUser = await _db.Set<Sys_User>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), PortQuery.Equal(userCode), PortQuery.Esc));

            if (dbUser == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_CheckDB_UserCodeNotFound", "Check.UserCodeNotFound", userCode);
            }

            if (dbUser.FlagActive != "1")
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_CheckDB_FlagActiveNotMatched",
                    "Check.UserCodeError", userCode,
                    "Check.FlagActiveListToCheck", "1",
                    "Check.FlagActiveCurrent", dbUser.FlagActive);
            }

            var strMST = StdParam(dbUser.MST) ?? StdParam(req.MST);

            // 6. Lưu vào DB Sys_Group (zTemp.cs:1178-1194)
            var dtimeSys = DateTime.UtcNow;
            var entity = new Sys_Group
            {
                GroupCode = groupCode,
                GroupName = groupName,
                MST = strMST,
                FlagActive = "1",
                LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = userCode
            };

            _db.Set<Sys_Group>().Add(entity);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // 7. Trả kết quả thành công khớp gate MVC (Sys_GroupController.cs:209-212)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Tạo nhóm người dùng thành công!" },
                Title = "Tạo nhóm người dùng thành công!",
                RedirectUrl = "/Sys_Group",
                Data = entity
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_Group_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Update từ gate Sys_GroupController.cs:218-266.
    /// Chuỗi gọi:
    /// 1. SysAccessCheckDeny(UserState, "MENU_QL_NHOM_NGUOI_DUNG_SUA") (gate dòng 220-224).
    /// 2. Đọc UserState, AddressAPIs, WAUserCode, WAUserPassword (gate dòng 225-229).
    /// 3. Nếu groupcode rỗng/null, gate trả về View(new Sys_Group()) (gate dòng 231, 265).
    ///    Ở API Port trả về Success = true, Data = null.
    /// 4. Nếu groupcode có giá trị:
    ///    - Dựng strWhereClause_SysGroup(groupcode) (gate dòng 234, 1080-1091): Sys_Group.GroupCode = groupcode.
    ///    - Đóng gói RQ_Sys_Group với Rt_Cols_Sys_Group = "*", Rt_Cols_Sys_UserInGroup = null (gate dòng 236-257).
    ///    - Gọi Sys_GroupService.Instance.WA_Sys_Group_Get(objRQ_Sys_Group, addressAPIs) (gate dòng 259)
    ///      -> API WA_Sys_Group_Get (SysGroupController.cs:20-91)
    ///      -> BIZ WAS_Sys_Group_Get (System.cs:2283-2397)
    ///      -> Sys_Group_Get_New20191102 (System.cs:1966-2281).
    ///      SQL: lọc nhóm theo sg.GroupCode = @groupcode qua #tbl_Sys_Group_Filter_Draft và #tbl_Sys_Group_Filter (System.cs:2076-2106).
    ///      Lấy dữ liệu Sys_Group: select t.MyIdxSeq, sg.* from #tbl_Sys_Group_Filter t inner join Sys_Group sg on t.GroupCode = sg.GroupCode order by t.MyIdxSeq asc (System.cs:2127-2138).
    /// 5. Nếu tìm thấy (Lst_Sys_Group có phần tử): trả về View(objRT_Sys_GroupCur.Lst_Sys_Group[0]) (gate dòng 260-263).
    ///    Ở API Port trả về Success = true, Data = item.
    ///    Nếu không tìm thấy: trả về View(new Sys_Group()) (gate dòng 265).
    ///    Ở API Port trả về Success = true, Data = null.
    /// 6. Bắt ngoại lệ catch (Exception ex) -> PortResult.Fail("Sys_Group_Get", "Exception", ex.Message) (SysGroupController.cs:28, System.cs:1992).
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_UpdateReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào GroupCode (Gate: Sys_GroupController.cs:218, 231)
            var groupCode = StdParam(req?.GroupCode);
            if (string.IsNullOrEmpty(groupCode) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("groupcode", out var qGroupCode))
            {
                groupCode = StdParam(qGroupCode.ToString());
            }

            // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
            if (string.IsNullOrEmpty(groupCode) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = System.Text.Json.JsonSerializer.Deserialize<Sys_Group_UpdateReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    groupCode = StdParam(modelObj?.GroupCode);
                }
                catch
                {
                    groupCode = StdParam(req.Model);
                }
            }

            // 2. Nếu groupcode rỗng hoặc null, gate trả về View(new Sys_Group()) (Sys_GroupController.cs:231, 265)
            if (string.IsNullOrEmpty(groupCode))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Sys_Group?)null
                });
            }

            // 3. Chuẩn hoá MST nếu có truyền (System.cs:2083, 2181-2184)
            var mst = StdParam(req?.MST);
            if (string.IsNullOrEmpty(mst) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("mst", out var qMst))
            {
                mst = StdParam(qMst.ToString());
            }

            // 4. Truy vấn nhóm người dùng theo GroupCode (System.cs:2081, 2133: select * from Sys_Group where GroupCode = @groupcode)
            var query = _db.Set<Sys_Group>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));

            if (!string.IsNullOrEmpty(mst))
            {
                query = query.Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
            }

            var group = await query.OrderBy(x => x.GroupCode).FirstOrDefaultAsync();

            // 5. Nếu không tìm thấy, gate trả về View(new Sys_Group()) (Sys_GroupController.cs:265)
            //    Nếu tìm thấy, gate trả về View(Lst_Sys_Group[0]) (Sys_GroupController.cs:262)
            return Ok(new
            {
                Success = true,
                Data = group
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Group_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Edit từ gate Sys_GroupController.cs:270-341.
    /// Chuỗi gọi:
    /// 1. Deserialize model -> Sys_Group (gate dòng 278).
    /// 2. Kiểm tra if (model != null && !CUtils.IsNullOrEmpty(objSys_Group.GroupCode)) (gate dòng 280):
    ///    Nếu rỗng: trả về Success = true, AddMessage("Mã nhóm người dùng trống!") (gate dòng 336-337).
    /// 3. Dựng strWhereClause = strWhereClause_SysGroup(GroupCode) (gate dòng 283).
    ///    Gọi Sys_GroupService.Instance.WA_Sys_Group_Get(objRQ_Sys_Group, addressAPIs) (gate dòng 306).
    /// 4. Kiểm tra if (objRT_Sys_GroupCur.Lst_Sys_Group != null && objRT_Sys_GroupCur.Lst_Sys_Group.Count > 0) (gate dòng 307):
    ///    Nếu không thấy: trả về Success = true, AddMessage("Mã nhóm người dùng '" + objSys_Group.GroupCode + "' không có trong hệ thống!") (gate dòng 330-331).
    /// 5. Nếu tìm thấy:
    ///    Gán GroupName, FlagActive, thiết lập strFt_Cols_Upd (gate dòng 309-320).
    ///    Gọi Sys_GroupService.Instance.WA_Sys_Group_Update(objRQ_Sys_Group, addressAPIs) (gate dòng 322)
    ///    -> API WA_Sys_Group_Update (SysGroupController.cs:168-239)
    ///    -> BIZ WAS_Sys_Group_Update (System.cs:3118-3201)
    ///    -> Sys_Group_Update (System.cs:2937-3116 / 2791-2935).
    /// 6. BIZ Sys_Group_Update:
    ///    - Mở transaction: _cf.db.BeginTransaction() (System.cs:2981).
    ///    - Chuẩn hoá strGroupCode = StdParam(objGroupCode), strGroupName = string.Format("{0}", objGroupName).Trim(), strFlagActive = StdFlag(objFlagActive) (System.cs:3025-3027).
    ///    - Gọi Sys_Group_CheckDB(..., FlagExistToCheck = "1", ...) (System.cs:3035-3041).
    ///    - Nếu bUpd_GroupName && strGroupName.Length < 1 -> ném Sys_Group_Update_InvalidGroupName (System.cs:3043-3053).
    ///    - Cập nhật GroupName, FlagActive, LogLUDTimeUTC = dtimeSys, LogLUBy = strWAUserCode (System.cs:3059-3068).
    ///    - Commit transaction (System.cs:3075).
    /// 7. Trả về kết quả khớp gate MVC (Sys_GroupController.cs:324-326):
    ///    Success = true, Messages = ["Sửa nhóm người dùng thành công!"], RedirectUrl = Url.Action("Index").
    /// </summary>
    [HttpPost("edit")]
    [HttpPost("save")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_EditReq? req)
    {
        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Sys_GroupController.cs:270, 278)
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var modelObj = System.Text.Json.JsonSerializer.Deserialize<Sys_Group_EditReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.GroupCode ??= modelObj.GroupCode;
                    req.GroupName ??= modelObj.GroupName;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.MST ??= modelObj.MST;
                    req.UserCode ??= modelObj.UserCode;
                    req.WAUserCode ??= modelObj.WAUserCode;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.Ft_Cols_Upd ??= modelObj.Ft_Cols_Upd;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        // Gate guard 1: Kiểm tra model và GroupCode không được rỗng (Sys_GroupController.cs:280, 334-338)
        var rawGroupCode = req?.GroupCode;
        if (req == null || string.IsNullOrWhiteSpace(rawGroupCode))
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Mã nhóm người dùng trống!" },
                Title = "Mã nhóm người dùng trống!"
            });
        }

        var groupCode = StdParam(rawGroupCode);
        if (string.IsNullOrEmpty(groupCode))
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Mã nhóm người dùng trống!" },
                Title = "Mã nhóm người dùng trống!"
            });
        }

        // Gate guard 2: Tra cứu bản ghi nhóm hiện có trong DB qua WA_Sys_Group_Get (Sys_GroupController.cs:282-307, 328-332)
        var existing = await _db.Set<Sys_Group>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));

        if (existing == null)
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { $"Mã nhóm người dùng '{rawGroupCode}' không có trong hệ thống!" },
                Title = $"Mã nhóm người dùng '{rawGroupCode}' không có trong hệ thống!"
            });
        }

        // Xác định các cột cập nhật theo gate (Sys_GroupController.cs:312-315: GroupName, FlagActive)
        var strFt_Cols_Upd = (StdParam(req.Ft_Cols_Upd) ?? "SYS_GROUP.GROUPNAME,SYS_GROUP.FLAGACTIVE,").ToUpperInvariant();
        bool bUpd_GroupName = strFt_Cols_Upd.Contains("SYS_GROUP.GROUPNAME") || strFt_Cols_Upd.Contains("GROUPNAME");
        bool bUpd_FlagActive = strFt_Cols_Upd.Contains("SYS_GROUP.FLAGACTIVE") || strFt_Cols_Upd.Contains("FLAGACTIVE");

        // Chuẩn hoá dữ liệu cập nhật theo System.cs:3026-3027
        var strGroupName = string.Format("{0}", req.GroupName).Trim();
        var strFlagActive = req.FlagActive != null ? StdFlag(req.FlagActive) : (existing.FlagActive ?? "1");

        // Mở transaction cập nhật (System.cs:2981)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Biz guard: Nếu cập nhật GroupName mà strGroupName.Length < 1 -> ném Sys_Group_Update_InvalidGroupName (System.cs:3043-3053)
            if (bUpd_GroupName && strGroupName.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_Update_InvalidGroupName", "Check.strGroupName", req.GroupName ?? "");
            }

            if (bUpd_GroupName) existing.GroupName = strGroupName;
            if (bUpd_FlagActive) existing.FlagActive = strFlagActive;

            var dtimeSys = DateTime.UtcNow;
            var userCode = Request?.Headers["WAUserCode"].FirstOrDefault() ?? req.WAUserCode ?? req.UserCode;
            if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

            existing.LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
            existing.LogLUBy = userCode;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Trả kết quả thành công khớp gate MVC (Sys_GroupController.cs:324-326)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Sửa nhóm người dùng thành công!" },
                Title = "Sửa nhóm người dùng thành công!",
                RedirectUrl = "/Sys_Group",
                Data = existing
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_Group_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Detail từ gate Sys_GroupController.cs:346-394.
    /// Chuỗi gọi:
    /// 1. SysAccessCheckDeny(UserState, "MENU_QL_NHOM_NGUOI_DUNG_CHI_TIET") (gate dòng 348-352).
    /// 2. Đọc UserState, AddressAPIs, WAUserCode, WAUserPassword (gate dòng 353-357).
    /// 3. Nếu groupcode rỗng/null, gate trả về View(new Sys_Group()) (gate dòng 359, 393).
    ///    Ở API Port trả về Success = true, Data = null.
    /// 4. Nếu groupcode có giá trị:
    ///    - Dựng strWhereClause_SysGroup(groupcode) (gate dòng 362, 1080-1091): Sys_Group.GroupCode = groupcode.
    ///    - Đóng gói RQ_Sys_Group với Rt_Cols_Sys_Group = "*", Rt_Cols_Sys_UserInGroup = null (gate dòng 364-385).
    ///    - Gọi Sys_GroupService.Instance.WA_Sys_Group_Get(objRQ_Sys_Group, addressAPIs) (gate dòng 387)
    ///      -> API WA_Sys_Group_Get (SysGroupController.cs:20-91)
    ///      -> BIZ WAS_Sys_Group_Get (System.cs:2283-2397)
    ///      -> Sys_Group_Get_New20191102 (System.cs:1966-2281).
    ///      SQL: lọc nhóm theo sg.GroupCode = @groupcode qua #tbl_Sys_Group_Filter_Draft và #tbl_Sys_Group_Filter (System.cs:2076-2106).
    ///      Lấy dữ liệu Sys_Group: select t.MyIdxSeq, sg.* from #tbl_Sys_Group_Filter t inner join Sys_Group sg on t.GroupCode = sg.GroupCode order by t.MyIdxSeq asc (System.cs:2127-2138).
    /// 5. Nếu tìm thấy (Lst_Sys_Group có phần tử): trả về View(objRT_Sys_GroupCur.Lst_Sys_Group[0]) (gate dòng 388-391).
    ///    Ở API Port trả về Success = true, Data = item.
    ///    Nếu không tìm thấy: trả về View(new Sys_Group()) (gate dòng 393).
    ///    Ở API Port trả về Success = true, Data = null.
    /// 6. Bắt ngoại lệ catch (Exception ex) -> PortResult.Fail("Sys_Group_Get", "Exception", ex.Message) (SysGroupController.cs:28, System.cs:1992).
    /// </summary>
    [HttpPost("detail")]
    public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_DetailReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào GroupCode (Gate: Sys_GroupController.cs:346, 359)
            var groupCode = StdParam(req?.GroupCode);
            if (string.IsNullOrEmpty(groupCode) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("groupcode", out var qGroupCode))
            {
                groupCode = StdParam(qGroupCode.ToString());
            }

            // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
            if (string.IsNullOrEmpty(groupCode) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = System.Text.Json.JsonSerializer.Deserialize<Sys_Group_DetailReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    groupCode = StdParam(modelObj?.GroupCode);
                }
                catch
                {
                    groupCode = StdParam(req.Model);
                }
            }

            // 2. Nếu groupcode rỗng hoặc null, gate trả về View(new Sys_Group()) (Sys_GroupController.cs:359, 393)
            if (string.IsNullOrEmpty(groupCode))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Sys_Group?)null
                });
            }

            // 3. Chuẩn hoá MST nếu có truyền (System.cs:2083, 2181-2184)
            var mst = StdParam(req?.MST);
            if (string.IsNullOrEmpty(mst) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("mst", out var qMst))
            {
                mst = StdParam(qMst.ToString());
            }

            // 4. Truy vấn nhóm người dùng theo GroupCode (System.cs:2081, 2133: select * from Sys_Group where GroupCode = @groupcode)
            var query = _db.Set<Sys_Group>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));

            if (!string.IsNullOrEmpty(mst))
            {
                query = query.Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
            }

            var group = await query.OrderBy(x => x.GroupCode).FirstOrDefaultAsync();

            // 5. Nếu không tìm thấy, gate trả về View(new Sys_Group()) (Sys_GroupController.cs:393)
            //    Nếu tìm thấy, gate trả về View(Lst_Sys_Group[0]) (Sys_GroupController.cs:390)
            return Ok(new
            {
                Success = true,
                Data = group
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Group_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Delete từ gate Sys_GroupController.cs:398-470.
    /// Chuỗi gọi:
    /// 1. Nhận groupcode (gate dòng 398).
    /// 2. Khởi tạo JsonResultEntry { Success = false } (gate dòng 400).
    /// 3. Kiểm tra if (!CUtils.IsNullOrEmpty(groupcode)) (gate dòng 409):
    ///    Nếu rỗng: title = "Mã nhóm người dùng trống!", resultEntry.AddMessage(title), trả về JsonResultEntry với Success = false (gate dòng 458-461).
    /// 4. Nếu groupcode có giá trị:
    ///    - Dựng strWhereClause = strWhereClause_SysGroup(groupcode) (gate dòng 412, 1080-1091): Sys_Group.GroupCode = groupcode.
    ///    - Đóng gói RQ_Sys_Group với Rt_Cols_Sys_Group = "*", Rt_Cols_Sys_UserInGroup = null (gate dòng 414-435).
    ///    - Gọi Sys_GroupService.Instance.WA_Sys_Group_Get(objRQ_Sys_Group, addressAPIs) (gate dòng 437)
    ///      -> API WA_Sys_Group_Get (SysGroupController.cs:20-91)
    ///      -> BIZ WAS_Sys_Group_Get (System.cs:2283-2397)
    ///      -> Sys_Group_Get_New20191102 (System.cs:1966-2281).
    /// 5. Kiểm tra if (objRT_Sys_GroupCur.Lst_Sys_Group != null && objRT_Sys_GroupCur.Lst_Sys_Group.Count > 0) (gate dòng 438):
    ///    Nếu không thấy: title = "Mã nhóm người dùng '" + groupcode + "' không có trong hệ thống!", resultEntry.AddMessage(title), trả về JsonResultEntry với Success = false (gate dòng 452-455).
    /// 6. Nếu tìm thấy:
    ///    - Gán objRQ_Sys_Group.Sys_Group = objRT_Sys_GroupCur.Lst_Sys_Group[0] (gate dòng 443).
    ///    - Gọi Sys_GroupService.Instance.WA_Sys_Group_Delete(objRQ_Sys_Group, addressAPIs) (gate dòng 444)
    ///      -> API WA_Sys_Group_Delete (SysGroupController.cs:242-313)
    ///      -> BIZ WAS_Sys_Group_Delete (System.cs:3469-3548)
    ///      -> Sys_Group_Delete (System.cs:3318-3467).
    /// 7. BIZ Sys_Group_Delete (System.cs:3318-3467):
    ///    - Mở transaction: _cf.db.BeginTransaction() (System.cs:3355).
    ///    - Chuẩn hoá strGroupCode = StdParam(objGroupCode) (System.cs:3396).
    ///    - Gọi Sys_Group_CheckDB(..., strGroupCode, FlagExistToCheck = "1", "", out dtDB_Sys_Group) (System.cs:3401-3407).
    ///    - Xoá người dùng trong nhóm: gọi Sys_UserInGroup_Delete_ByGroup(strGroupCode) (System.cs:3409-3411, 13768-13783).
    ///      SQL: delete from Sys_UserInGroup where GroupCode = @strGroupCode.
    ///    - Xoá bản ghi nhóm Sys_Group: dtDB_Sys_Group.Rows[0].Delete(), SaveData("Sys_Group") (System.cs:3419-3426).
    ///    - Commit transaction (System.cs:3430).
    /// 8. Trả về kết quả khớp gate MVC (Sys_GroupController.cs:445-450):
    ///    Success = true, Messages = ["Xóa nhóm người dùng '" + groupcode + "' thành công!"], RedirectUrl = Url.Action("Index").
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_DeleteReq? req)
    {
        try
        {
            // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
            if (!string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = System.Text.Json.JsonSerializer.Deserialize<Sys_Group_DeleteReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        req.GroupCode ??= modelObj.GroupCode;
                    }
                }
                catch
                {
                    req.GroupCode ??= req.Model;
                }
            }

            // 1. Chuẩn hoá tham số đầu vào GroupCode (Gate: Sys_GroupController.cs:398, 409; BIZ: System.cs:3396)
            var rawGroupCode = req?.GroupCode;
            var groupCode = StdParam(rawGroupCode);
            if (string.IsNullOrEmpty(groupCode) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("groupcode", out var qGroupCode))
            {
                rawGroupCode = qGroupCode.ToString();
                groupCode = StdParam(rawGroupCode);
            }

            // Gate guard 1: Kiểm tra GroupCode không được rỗng (Sys_GroupController.cs:409, 458-461)
            if (string.IsNullOrEmpty(groupCode))
            {
                return Ok(new
                {
                    Success = false,
                    Messages = new[] { "Mã nhóm người dùng trống!" },
                    Title = "Mã nhóm người dùng trống!"
                });
            }

            // Gate guard 2: Tra cứu bản ghi nhóm hiện có trong DB qua WA_Sys_Group_Get (Sys_GroupController.cs:412-438, 452-455)
            var existing = await _db.Set<Sys_Group>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));

            if (existing == null)
            {
                return Ok(new
                {
                    Success = false,
                    Messages = new[] { $"Mã nhóm người dùng '{rawGroupCode}' không có trong hệ thống!" },
                    Title = $"Mã nhóm người dùng '{rawGroupCode}' không có trong hệ thống!"
                });
            }

            // Mở transaction xoá dữ liệu (System.cs:3355)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // BIZ: Sys_UserInGroup_Delete_ByGroup (System.cs:3409-3411, 13768-13783)
                // Xoá tất cả người dùng thuộc nhóm trong Sys_UserInGroup
                var usersInGroup = await _db.Set<Sys_UserInGroup>()
                    .Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc))
                    .ToListAsync();
                if (usersInGroup.Count > 0)
                {
                    _db.Set<Sys_UserInGroup>().RemoveRange(usersInGroup);
                }

                // Xoá bản ghi Sys_Group (System.cs:3419-3426)
                _db.Set<Sys_Group>().Remove(existing);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Trả về kết quả thành công khớp gate MVC (Sys_GroupController.cs:445-450)
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { $"Xóa nhóm người dùng '{rawGroupCode}' thành công!" },
                    Title = $"Xóa nhóm người dùng '{rawGroupCode}' thành công!",
                    RedirectUrl = "/Sys_Group",
                    Data = existing
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_Group_Delete", "Exception", ex.Message);
            }
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Group_Delete", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action GetSysUser từ gate Sys_GroupController.cs:477-567.
    /// Chuỗi gọi:
    /// 1. Khởi tạo danh sách listSysUser và listUserInGroup (gate dòng 487-488).
    /// 2. Danh sách người dùng trạng thái Active:
    ///    - sysUser = new Sys_User { FlagActive = Client_Flag.Active ("1") } (gate dòng 490-493).
    ///    - Dựng strWhereClause_SysUser (gate dòng 494, 1119-1138): Sys_User.FlagActive = '1'.
    ///    - Đóng gói RQ_Sys_User với Rt_Cols_Sys_User = "*", Rt_Cols_Sys_UserInGroup = "" (gate dòng 495-516).
    ///    - Gọi Sys_UserService.Instance.WA_Sys_User_Get (gate dòng 517)
    ///      -> API WA_Sys_User_Get (SysUserController.cs:199-270)
    ///      -> BIZ WAS_Sys_User_Get (System.cs:7011-7123)
    ///      -> Sys_User_Get_New20191104 (System.cs:6635-7009).
    ///      SQL: Lọc người dùng active, order by su.UserCode asc, che mật khẩu Default_PasswordMask ("*********").
    ///    - Nếu có kết quả thì listSysUser.AddRange(objRT_Sys_User.Lst_Sys_User) (gate dòng 518-521).
    /// 3. Danh sách người dùng trong nhóm:
    ///    - Dựng strWhereClause_SysGroup(groupcode) (gate dòng 525-526, 1080-1091): Sys_Group.GroupCode = groupcode.
    ///    - Đóng gói RQ_Sys_Group với Rt_Cols_Sys_Group = null, Rt_Cols_Sys_UserInGroup = "*" (gate dòng 528-549).
    ///    - Gọi Sys_GroupService.Instance.WA_Sys_Group_Get (gate dòng 551)
    ///      -> API WA_Sys_Group_Get (SysGroupController.cs:20-91)
    ///      -> BIZ WAS_Sys_Group_Get (System.cs:2283-2397)
    ///      -> Sys_Group_Get_New20191102 (System.cs:1966-2281).
    ///      SQL: Lọc nhóm qua #tbl_Sys_Group_Filter_Draft và #tbl_Sys_Group_Filter (System.cs:2076-2106).
    ///      Lấy dữ liệu người dùng trong nhóm qua inner join Sys_UserInGroup suig on t.GroupCode = suig.GroupCode
    ///      left join Sys_User su on suig.UserCode = su.UserCode
    ///      left join Sys_Group sg on suig.GroupCode = sg.GroupCode (System.cs:2146-2167).
    ///    - Nếu có kết quả thì listUserInGroup.AddRange(objRT_Sys_GroupCur.Lst_Sys_UserInGroup) (gate dòng 552-555).
    /// 4. Gán ViewBag.GroupCode = groupcode, ViewBag.ListUserInGroup = listUserInGroup (gate dòng 557-558).
    /// 5. Trả về JsonView("GetSysUser", listSysUser) (gate dòng 559).
    /// 6. Bắt ngoại lệ catch (Exception ex) -> PortResult.Fail("Sys_Group_Get", "Exception", ex.Message) (gate dòng 561-566, System.cs:1992, TError.ErridQContract.Sys_Group_Get).
    /// </summary>
    [HttpPost("getsysuser")]
    public async Task<IActionResult> GetSysUser([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_GetSysUserReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào groupcode (Gate Sys_GroupController.cs:477)
            var groupCode = StdParam(req?.GroupCode);
            if (string.IsNullOrEmpty(groupCode) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("groupcode", out var qGroupCode))
            {
                groupCode = StdParam(qGroupCode.ToString());
            }

            // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
            if (string.IsNullOrEmpty(groupCode) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = System.Text.Json.JsonSerializer.Deserialize<Sys_Group_GetSysUserReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    groupCode = StdParam(modelObj?.GroupCode);
                }
                catch
                {
                    groupCode = StdParam(req.Model);
                }
            }

            // 2. Danh sách người dùng trạng thái Active (Gate Sys_GroupController.cs:489-522)
            // sysUser.FlagActive = Client_Flag.Active ("1")
            var flagActive = req?.FlagActive != null ? StdFlag(req.FlagActive) : "1";
            var mst = StdParam(req?.MST);
            var orgId = StdParam(req?.OrgID);

            var userQuery = _db.Set<Sys_User>().AsNoTracking();
            if (!string.IsNullOrEmpty(flagActive))
            {
                userQuery = userQuery.Where(u => EF.Functions.Like((u.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(mst))
            {
                userQuery = userQuery.Where(u => EF.Functions.Like((u.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(orgId))
            {
                userQuery = userQuery.Where(u => EF.Functions.Like((u.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            var listSysUser = await userQuery.OrderBy(u => u.UserCode).ToListAsync();
            foreach (var u in listSysUser)
            {
                u.UserPassword = Default_PasswordMask;
            }

            // 3. Danh sách người dùng trong nhóm (Gate Sys_GroupController.cs:524-556)
            // Lọc theo groupcode nếu có (System.cs:2081, 2146-2167)
            var groupQuery = _db.Set<Sys_Group>().AsNoTracking();
            if (!string.IsNullOrEmpty(groupCode))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(mst))
            {
                groupQuery = groupQuery.Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
            }

            var groups = groupQuery.OrderBy(x => x.GroupCode);

            var mappingQuery = from t in groups
                               join suig in _db.Set<Sys_UserInGroup>().AsNoTracking() on t.GroupCode equals suig.GroupCode
                               join su in _db.Set<Sys_User>().AsNoTracking() on suig.UserCode equals su.UserCode into gUser
                               from su in gUser.DefaultIfEmpty()
                               join sg in _db.Set<Sys_Group>().AsNoTracking() on suig.GroupCode equals sg.GroupCode into gGroup
                               from sg in gGroup.DefaultIfEmpty()
                               orderby t.GroupCode, suig.UserCode
                               select new Sys_UserInGroup
                               {
                                   GroupCode = suig.GroupCode,
                                   UserCode = suig.UserCode,
                                   MST = suig.MST ?? (sg != null ? sg.MST : null),
                                   LogLUDTimeUTC = suig.LogLUDTimeUTC,
                                   LogLUBy = suig.LogLUBy,
                                   su_UserCode = su != null ? su.UserCode : suig.UserCode,
                                   su_UserName = su != null ? su.UserName : null,
                                   su_FlagSysAdmin = su != null ? su.FlagSysAdmin : null,
                                   su_FlagActive = su != null ? su.FlagActive : null,
                                   sg_GroupCode = sg != null ? sg.GroupCode : suig.GroupCode,
                                   sg_GroupName = sg != null ? sg.GroupName : null,
                                   sg_FlagActive = sg != null ? sg.FlagActive : null,
                               };

            var listUserInGroup = await mappingQuery.ToListAsync();

            // 4. Trả về kết quả khớp Model và ViewBag của gate MVC (Sys_GroupController.cs:557-559)
            // Model: listSysUser; ViewBag: GroupCode, ListUserInGroup
            var data = new Sys_Group_GetSysUserData
            {
                GroupCode = groupCode,
                ListSysUser = listSysUser,
                Lst_Sys_User = listSysUser,
                ListUserInGroup = listUserInGroup,
                Lst_Sys_UserInGroup = listUserInGroup
            };

            return Ok(new
            {
                Success = true,
                Data = data,
                ListSysUser = listSysUser,
                ListUserInGroup = listUserInGroup,
                GroupCode = groupCode
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Group_Get", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpperInvariant();
    private static string? StdStr(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string StdFlag(string? v) => (v?.Trim() == "1") ? "1" : "0";
}

/// <summary>
/// DTO yêu cầu cho action Index của Sys_GroupController.
/// </summary>
public class Sys_Group_IndexReq
{
    // Tham số từ Gate Sys_GroupController.cs:22 Index(string groupname, string init = "init", int recordcount = 10, int page = 0)
    public string? GroupName { get; set; }

    public string? Init { get; set; }

    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }

    public int? Page { get; set; }
    public int? PageIndex { get; set; }

    // Tham số phân trang tầng BIZ System.cs:1978-1979
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    // Tham số lọc bổ sung theo BIZ htSpCols (System.cs:2181-2184)
    public string? GroupCode { get; set; }
    public string? FlagActive { get; set; }
    public string? MST { get; set; }
    public string? KeyWord { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action GetDetailData của Sys_GroupController.
/// Nguồn: Sys_GroupController.cs:107 GetDetailData(string groupcode)
/// </summary>
public class Sys_Group_GetDetailDataReq
{
    public string? GroupCode { get; set; }
    public string? MST { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action Create của Sys_GroupController.
/// Nguồn: Sys_GroupController.cs:175 Create(string model)
/// </summary>
public class Sys_Group_CreateReq
{
    public string? Model { get; set; }
    public string? GroupCode { get; set; }
    public string? GroupName { get; set; }
    public string? FlagActive { get; set; }
    public string? MST { get; set; }
    public string? UserCode { get; set; }
    public string? WAUserCode { get; set; }
    public string? NetworkID { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action Update của Sys_GroupController.
/// Nguồn: Sys_GroupController.cs:218 Update(string groupcode)
/// </summary>
public class Sys_Group_UpdateReq
{
    public string? GroupCode { get; set; }
    public string? MST { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action Edit của Sys_GroupController.
/// Nguồn: Sys_GroupController.cs:270 Edit(string model)
/// </summary>
public class Sys_Group_EditReq
{
    public string? Model { get; set; }
    public string? GroupCode { get; set; }
    public string? GroupName { get; set; }
    public string? FlagActive { get; set; }
    public string? MST { get; set; }
    public string? UserCode { get; set; }
    public string? WAUserCode { get; set; }
    public string? NetworkID { get; set; }
    public string? Ft_Cols_Upd { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action Detail của Sys_GroupController.
/// Nguồn: Sys_GroupController.cs:346 Detail(string groupcode)
/// </summary>
public class Sys_Group_DetailReq
{
    public string? GroupCode { get; set; }
    public string? MST { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action Delete của Sys_GroupController.
/// Nguồn: Sys_GroupController.cs:398 Delete(string groupcode = "")
/// </summary>
public class Sys_Group_DeleteReq
{
    public string? GroupCode { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action GetSysUser của Sys_GroupController.
/// Nguồn: Sys_GroupController.cs:477 GetSysUser(string groupcode = "")
/// </summary>
public class Sys_Group_GetSysUserReq
{
    public string? GroupCode { get; set; }
    public string? MST { get; set; }
    public string? OrgID { get; set; }
    public string? FlagActive { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// DTO cấu trúc dữ liệu trả về cho action GetSysUser của Sys_GroupController.
/// Khớp với Model Sys_User (danh sách người dùng active) và ViewBag (GroupCode, ListUserInGroup).
/// </summary>
public class Sys_Group_GetSysUserData
{
    public string? GroupCode { get; set; }
    public List<Sys_User> ListSysUser { get; set; } = new();
    public List<Sys_User> Lst_Sys_User { get; set; } = new();
    public List<Sys_UserInGroup> ListUserInGroup { get; set; } = new();
    public List<Sys_UserInGroup> Lst_Sys_UserInGroup { get; set; } = new();
}
