using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

public class Sys_UserControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Sys_UserControllerPort _controller;

    public Sys_UserControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Seed Mst_Org
        var orgSeeder = new Mst_OrgSeeder();
        orgSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Mst_NNT
        var nntSeeder = new Mst_NNTSeeder();
        nntSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Mst_Department
        var deptSeeder = new Mst_DepartmentSeeder();
        deptSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Sys_User
        var userSeeder = new Sys_UserSeeder();
        userSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Mst_Customer
        var custSeeder = new Mst_CustomerSeeder();
        custSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Sys_UserControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_NullReq_ReturnsAllUsersWithMaskedPassword()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());

        // Kiểm tra UserCode sắp xếp asc: U01, U02, U03
        var u01 = list[0];
        Assert.Equal("U01", u01.GetProperty("UserCode").GetString());
        Assert.Equal("admin@solution.hn", u01.GetProperty("UserName").GetString());
        Assert.Equal("admin@solution.hn", u01.GetProperty("EMail").GetString());
        Assert.Equal("0901234567", u01.GetProperty("PhoneNo").GetString());
        Assert.Equal("0100000001", u01.GetProperty("MST").GetString());
        // Mật khẩu phải được che bằng Default_PasswordMask ("*********") theo System.cs:6816
        Assert.Equal("*********", u01.GetProperty("UserPassword").GetString());
        // Cột nối từ Mst_Department theo System.cs:6837-6838
        Assert.Equal("DEPT_BGD", u01.GetProperty("mdept_DepartmentCode").GetString());
        Assert.Equal("Ban Giám Đốc", u01.GetProperty("mdept_DepartmentName").GetString());
        // OrgID nối từ Mst_NNT theo System.cs:6839
        Assert.Equal("7200000001", u01.GetProperty("OrgID").GetString());

        var u02 = list[1];
        Assert.Equal("U02", u02.GetProperty("UserCode").GetString());
        Assert.Equal("*********", u02.GetProperty("UserPassword").GetString());
        Assert.Equal("DEPT_KT", u02.GetProperty("mdept_DepartmentCode").GetString());
        Assert.Equal("Phòng Kế Toán", u02.GetProperty("mdept_DepartmentName").GetString());

        var u03 = list[2];
        Assert.Equal("U03", u03.GetProperty("UserCode").GetString());
        Assert.Equal("*********", u03.GetProperty("UserPassword").GetString());
    }

    [Fact]
    public async Task Index_EmptyReq_ReturnsAllUsers()
    {
        var req = new Sys_User_IndexReq();
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByUserNameKeyword_Returns1Record()
    {
        // Bộ lọc 1: username = "admin" -> 1 dòng (U01)
        var req = new Sys_User_IndexReq { UserName = "admin" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("U01", list[0].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByCommonDomainKeyword_Returns3Records()
    {
        // Bộ lọc 2: username = "solution.hn" -> 3 dòng (U01, U02, U03) khác số dòng với bộ lọc 1 (1 dòng)
        var req = new Sys_User_IndexReq { UserName = "solution.hn" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByPhoneNo_Returns1Record()
    {
        // Tìm kiếm theo số điện thoại: strWhereClause_SysUserName tìm trên UserName/PhoneNo/EMail
        var req = new Sys_User_IndexReq { UserName = "0902345678" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("U02", list[0].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByMST_ReturnsDifferentCounts()
    {
        // Lọc theo MST: 0100000001 gồm U01, U02 -> 2 bản ghi
        var req1 = new Sys_User_IndexReq { MST = "0100000001" };
        var result1 = await _controller.Index(req1);
        var okResult1 = Assert.IsType<OkObjectResult>(result1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(okResult1.Value));
        Assert.Equal(2, doc1.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

        // Lọc theo MST: 0100000002 gồm U03 -> 1 bản ghi
        var req2 = new Sys_User_IndexReq { MST = "0100000002" };
        var result2 = await _controller.Index(req2);
        var okResult2 = Assert.IsType<OkObjectResult>(result2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(okResult2.Value));
        Assert.Equal(1, doc2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Index_Pagination_SkipAndTake()
    {
        // Trang 0, kích thước 1 -> lấy U01, tổng 3, số trang 3
        var reqPage0 = new Sys_User_IndexReq { Page = 0, PageSize = 1 };
        var result0 = await _controller.Index(reqPage0);
        var okResult0 = Assert.IsType<OkObjectResult>(result0);
        var doc0 = JsonDocument.Parse(JsonSerializer.Serialize(okResult0.Value));
        var data0 = doc0.RootElement.GetProperty("Data");
        Assert.Equal(3, data0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data0.GetProperty("PageCount").GetInt32());
        var list0 = data0.GetProperty("DataList");
        Assert.Equal(1, list0.GetArrayLength());
        Assert.Equal("U01", list0[0].GetProperty("UserCode").GetString());

        // Trang 1, kích thước 1 -> lấy U02
        var reqPage1 = new Sys_User_IndexReq { Page = 1, PageSize = 1 };
        var result1 = await _controller.Index(reqPage1);
        var okResult1 = Assert.IsType<OkObjectResult>(result1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(okResult1.Value));
        var data1 = doc1.RootElement.GetProperty("Data");
        var list1 = data1.GetProperty("DataList");
        Assert.Equal(1, list1.GetArrayLength());
        Assert.Equal("U02", list1[0].GetProperty("UserCode").GetString());

        // Kích thước 0 -> DataList rỗng, ItemCount = 3, PageCount = 0 (LESSONS_AGENTS A.7)
        var reqPageZero = new Sys_User_IndexReq { PageSize = 0 };
        var resultZero = await _controller.Index(reqPageZero);
        var okResultZero = Assert.IsType<OkObjectResult>(resultZero);
        var docZero = JsonDocument.Parse(JsonSerializer.Serialize(okResultZero.Value));
        var dataZero = docZero.RootElement.GetProperty("Data");
        Assert.Equal(3, dataZero.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_Guard_InvalidWAUserCode_ReturnsSys_User_BizInvalidUserAbility()
    {
        // Guard kiểm tra quyền xem của người dùng: UserCode không tồn tại
        // ném TError.ErridQContract.Sys_User_BizInvalidUserAbility (System.Mix.cs:74)
        var req = new Sys_User_IndexReq { WAUserCode = "INVALID_USER" };
        var result = await _controller.Index(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_BizInvalidUserAbility", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Index_ValidWAUserCode_ReturnsVisibleUsers()
    {
        // Người dùng hợp lệ U01 có MST 0100000001 -> xem được người dùng thuộc MST của mình (U01, U02)
        var req = new Sys_User_IndexReq { WAUserCode = "U01" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("U01", list[0].GetProperty("UserCode").GetString());
        Assert.Equal("U02", list[1].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task Create_NullReq_ReturnsInitDataWithHierarchicalDepartments()
    {
        // Khởi tạo màn hình Create với req rỗng
        var result = await _controller.Create(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        // IsParent mặc định: NetworkID (1001) != OrgID (7200000001) => false
        Assert.False(data.GetProperty("IsParent").GetBoolean());

        // Mst_NNT: người dùng thông thường (isSysadmin=0) chỉ thấy NNT của đơn vị mình (0100000001) -> 1 bản ghi
        var nntList = data.GetProperty("ListMst_NNTCur");
        Assert.Equal(1, nntList.GetArrayLength());
        Assert.Equal("0100000001", nntList[0].GetProperty("MST").GetString());

        // Mst_Department: lấy các phòng ban FlagActive = '1' và tổ chức cây phân cấp thụt lề HLevel
        // DEPT_BGD (Level 0, HlevelTitle="Ban Giám Đốc", HlevelCode="DEPT_BGD")
        // DEPT_KT (Level 1, HlevelTitle="|--Phòng Kế Toán", HlevelCode="|--DEPT_KT")
        // DEPT_KD_INACT (FlagActive = '0') bị loại bỏ
        var deptList = data.GetProperty("ListMst_Department");
        Assert.Equal(2, deptList.GetArrayLength());

        var bgd = deptList[0];
        Assert.Equal("DEPT_BGD", bgd.GetProperty("DepartmentCode").GetString());
        Assert.Equal(0, bgd.GetProperty("HLevel").GetInt32());
        Assert.Equal("Ban Giám Đốc", bgd.GetProperty("HlevelTitle").GetString());
        Assert.Equal("DEPT_BGD", bgd.GetProperty("HlevelCode").GetString());

        var kt = deptList[1];
        Assert.Equal("DEPT_KT", kt.GetProperty("DepartmentCode").GetString());
        Assert.Equal(1, kt.GetProperty("HLevel").GetInt32());
        Assert.Equal("|--Phòng Kế Toán", kt.GetProperty("HlevelTitle").GetString());
        Assert.Equal("|--DEPT_KT", kt.GetProperty("HlevelCode").GetString());
    }

    [Fact]
    public async Task Create_Init_SysAdmin_ReturnsAllActiveNNT()
    {
        // Bộ lọc 1: Người dùng thường (IsSysAdmin = "0") -> 1 NNT (0100000001)
        var reqNormal = new Sys_User_CreateReq { IsSysAdmin = "0" };
        var resNormal = await _controller.Create(reqNormal);
        var okNormal = Assert.IsType<OkObjectResult>(resNormal);
        var docNormal = JsonDocument.Parse(JsonSerializer.Serialize(okNormal.Value));
        Assert.Equal(1, docNormal.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());

        // Bộ lọc 2: SysAdmin (IsSysAdmin = "1") -> 2 NNT (0100000001, 0100000002) - khác số dòng
        var reqAdmin = new Sys_User_CreateReq { IsSysAdmin = "1" };
        var resAdmin = await _controller.Create(reqAdmin);
        var okAdmin = Assert.IsType<OkObjectResult>(resAdmin);
        var docAdmin = JsonDocument.Parse(JsonSerializer.Serialize(okAdmin.Value));
        var nntList = docAdmin.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur");
        Assert.Equal(2, nntList.GetArrayLength());
        Assert.Equal("0100000001", nntList[0].GetProperty("MST").GetString());
        Assert.Equal("0100000002", nntList[1].GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Create_Init_IsParent_TrueWhenNetworkEqualsOrgID()
    {
        // Khi NetworkID == OrgID thì IsParent = true (Sys_UserController.cs:129-134)
        var req = new Sys_User_CreateReq
        {
            NetworkID = "7200000001",
            OrgID = "7200000001"
        };
        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Data").GetProperty("IsParent").GetBoolean());
    }

    [Fact]
    public async Task Create_Save_Guard_EmptyUserCode_ReturnsInvalidUserCode()
    {
        // Guard 1: UserCode rỗng -> ném TError.ErridQContract.Sys_User_Create_InvalidUserCode (System.cs:8855)
        var req = new Sys_User_CreateReq
        {
            Save = "1",
            UserCode = ""
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_Create_InvalidUserCode", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_Save_Guard_DuplicateUserCode_ReturnsUserExist()
    {
        // Guard 2: Trùng UserCode đã tồn tại (U01) -> ném TError.ErridQContract.Sys_User_CheckDB_UserExist (System.cs:8880)
        var req = new Sys_User_CreateReq
        {
            Save = "1",
            UserCode = "u01", // Test không phân biệt hoa thường
            MST = "0100000001"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_UserExist", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_Save_Guard_NotFoundMST_ReturnsNNTNotFound()
    {
        // Guard 3: MST không tồn tại trong Mst_NNT -> ném TError.ErridQContract.Mst_NNT_CheckDB_NNTNotFound (System.cs:8915)
        var req = new Sys_User_CreateReq
        {
            Save = "1",
            UserCode = "U_NEW_FAIL",
            MST = "9999999999"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_NNTNotFound", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_Save_SuccessRoundTrip()
    {
        // Ca ghi thành công round-trip: lưu người dùng mới và kiểm tra lưu thành công trong cơ sở dữ liệu
        var req = new Sys_User_CreateReq
        {
            Save = "1",
            UserCode = "u_tester_01",
            UserName = "Nguyễn Văn Tester",
            UserPassword = "CustomPassword123",
            PhoneNo = "0988776655",
            EMail = "tester01@solution.hn",
            MST = "0100000001",
            DepartmentCode = "DEPT_KT"
        };

        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("/Sys_User", root.GetProperty("RedirectUrl").GetString());

        var messages = root.GetProperty("Messages");
        Assert.Equal("Tạo tài khoản thành công!", messages[0].GetString());

        // Kiểm tra đối tượng tạo mới trả về
        var userRet = root.GetProperty("Data");
        Assert.Equal("U_TESTER_01", userRet.GetProperty("UserCode").GetString()); // Chuẩn hoá UPPER
        Assert.Equal("Nguyễn Văn Tester", userRet.GetProperty("UserName").GetString());

        // Round-trip kiểm tra trực tiếp trong DB
        var userInDb = await _db.Set<Sys_User>().AsNoTracking().FirstOrDefaultAsync(u => u.UserCode == "U_TESTER_01");
        Assert.NotNull(userInDb);
        Assert.Equal("U_TESTER_01", userInDb.UserCode);
        Assert.Equal("Nguyễn Văn Tester", userInDb.UserName);
        Assert.Equal("CustomPassword123", userInDb.UserPassword);
        Assert.Equal("DEPT_KT", userInDb.DepartmentCode);
        Assert.Equal("0100000001", userInDb.MST);
        Assert.Equal("1", userInDb.FlagActive);
        Assert.Equal("0", userInDb.FlagSysAdmin);
        Assert.Equal("0", userInDb.FlagDLAdmin);
    }

    [Fact]
    public async Task Create_Save_ViaModelJsonString_SuccessRoundTrip()
    {
        // Ca ghi thành công khi client truyền JSON chuỗi qua tham số 'model' (Sys_UserController.cs:200, 213)
        var modelJson = JsonSerializer.Serialize(new
        {
            UserCode = "u_json_01",
            UserName = "Người Dùng JSON",
            UserPassword = "Password456",
            MST = "0100000001",
            DepartmentCode = "DEPT_BGD"
        });

        var req = new Sys_User_CreateReq
        {
            Model = modelJson
        };

        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var userInDb = await _db.Set<Sys_User>().AsNoTracking().FirstOrDefaultAsync(u => u.UserCode == "U_JSON_01");
        Assert.NotNull(userInDb);
        Assert.Equal("U_JSON_01", userInDb.UserCode);
        Assert.Equal("Người Dùng JSON", userInDb.UserName);
        Assert.Equal("DEPT_BGD", userInDb.DepartmentCode);
    }

    [Fact]
    public async Task LoadMstCustomer_NullReq_ReturnsEmptyList()
    {
        // Khi req null (body rỗng): trả về danh sách rỗng Data = []
        var result = await _controller.LoadMstCustomer(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task LoadMstCustomer_EmptyReq_ReturnsEmptyList()
    {
        // Khi req không có MST: trả về danh sách rỗng Data = []
        var req = new Sys_User_LoadMstCustomerReq();
        var result = await _controller.LoadMstCustomer(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task LoadMstCustomer_NonExistentMST_ReturnsEmptyList()
    {
        // Khi MST không tồn tại trong Mst_NNT: trả về danh sách rỗng Data = []
        var req = new Sys_User_LoadMstCustomerReq { MST = "9999999999" };
        var result = await _controller.LoadMstCustomer(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task LoadMstCustomer_ValidMST_ReturnsDealerCustomers()
    {
        // MST 0100000001 tương ứng OrgID 7200000001
        // Bảng Mst_Customer có 5 bản ghi:
        // - CUS001: OrgID=7200000001, FlagDealer=1, FlagActive=1 -> KHỚP (1 dòng)
        // - CUS002: OrgID=7200000001, FlagDealer=0, FlagActive=1 -> Không khớp FlagDealer
        // - CUS003: OrgID=7200000001, FlagDealer=0, FlagActive=1 -> Không khớp FlagDealer
        // - CUS004: OrgID=7200000001, FlagDealer=0, FlagActive=0 -> Không khớp FlagActive & FlagDealer
        // - CUS005: OrgID=7200000002, FlagDealer=1, FlagActive=1 -> Không khớp OrgID
        var req = new Sys_User_LoadMstCustomerReq
        {
            MST = "0100000001",
            OrgID = "7200000001"
        };
        var result = await _controller.LoadMstCustomer(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetArrayLength());

        var customer = data[0];
        Assert.Equal("CUS001", customer.GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("KH001", customer.GetProperty("CustomerCode").GetString());
        Assert.Equal("Công ty Cổ phần Alpha", customer.GetProperty("CustomerName").GetString());
        Assert.Equal("7200000001", customer.GetProperty("OrgID").GetString());
        Assert.Equal("1", customer.GetProperty("FlagDealer").GetString());
        Assert.Equal("1", customer.GetProperty("FlagActive").GetString());
    }

    [Fact]
    public async Task LoadMstCustomer_FilterComparison_DifferentRecordCounts()
    {
        // Bộ lọc 1: Người dùng thường (IsSysAdmin = 0) với OrgID = 7200000002 gọi MST 0100000001
        // NNT 0100000001 thuộc OrgID 7200000001 nên bị loại trừ ở bước lọc NNT -> trả về 0 dòng
        var reqNormal = new Sys_User_LoadMstCustomerReq
        {
            MST = "0100000001",
            OrgID = "7200000002",
            IsSysAdmin = "0"
        };
        var resNormal = await _controller.LoadMstCustomer(reqNormal);
        var okNormal = Assert.IsType<OkObjectResult>(resNormal);
        var docNormal = JsonDocument.Parse(JsonSerializer.Serialize(okNormal.Value));
        Assert.Equal(0, docNormal.RootElement.GetProperty("Data").GetArrayLength());

        // Bộ lọc 2: SysAdmin (IsSysAdmin = 1) với OrgID = 7200000002 gọi MST 0100000001
        // Sysadmin không bị lọc OrgID ở bước NNT, tìm thấy NNT 0100000001 (OrgID 7200000001)
        // và tải được 1 khách hàng đại lý CUS001 -> trả về 1 dòng (khác số dòng với bộ lọc 1)
        var reqAdmin = new Sys_User_LoadMstCustomerReq
        {
            MST = "0100000001",
            OrgID = "7200000002",
            IsSysAdmin = "1"
        };
        var resAdmin = await _controller.LoadMstCustomer(reqAdmin);
        var okAdmin = Assert.IsType<OkObjectResult>(resAdmin);
        var docAdmin = JsonDocument.Parse(JsonSerializer.Serialize(okAdmin.Value));
        Assert.Equal(1, docAdmin.RootElement.GetProperty("Data").GetArrayLength());
        Assert.Equal("CUS001", docAdmin.RootElement.GetProperty("Data")[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public void StopService_NullReq_ReturnsSuccessTrueAndNullData()
    {
        var result = _controller.StopService(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public void StopService_WithReq_ReturnsSuccessTrueAndNullData()
    {
        var result = _controller.StopService(new Sys_User_StopServiceReq());
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_NullReq_ReturnsSuccessTrueAndNullData()
    {
        // Khi req null (body rỗng): trả về Data = null (gate: View(new Sys_User()))
        var result = await _controller.Update(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_EmptyReq_ReturnsSuccessTrueAndNullData()
    {
        // Khi req rỗng (không có UserCode): trả về Data = null (gate: View(new Sys_User()))
        var req = new Sys_User_UpdateReq();
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_Guard_InvalidWAUserCode_ReturnsSys_User_BizInvalidUserAbility()
    {
        // Guard kiểm tra quyền xem của người dùng: WAUserCode không tồn tại
        // ném TError.ErridQContract.Sys_User_BizInvalidUserAbility (System.Mix.cs:74)
        var req = new Sys_User_UpdateReq
        {
            UserCode = "U01",
            WAUserCode = "INVALID_USER"
        };
        var result = await _controller.Update(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_BizInvalidUserAbility", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Update_NonExistentUserCode_ReturnsSuccessTrueAndNullData()
    {
        // Khi UserCode không tồn tại trong DB: trả về Data = null (gate: View(new Sys_User()))
        var req = new Sys_User_UpdateReq { UserCode = "NON_EXISTENT_USER" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_ValidUserCode_ReturnsUserDataWithMaskedPasswordAndLists()
    {
        // Tìm thấy UserCode u01 (kiểm tra không phân biệt hoa thường)
        var req = new Sys_User_UpdateReq { UserCode = "u01" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal("U01", data.GetProperty("UserCode").GetString());
        Assert.Equal("admin@solution.hn", data.GetProperty("UserName").GetString());
        // Mật khẩu che bằng Default_PasswordMask ("*********") (System.cs:6816)
        Assert.Equal("*********", data.GetProperty("UserPassword").GetString());
        Assert.Equal("0901234567", data.GetProperty("PhoneNo").GetString());
        Assert.Equal("admin@solution.hn", data.GetProperty("EMail").GetString());
        Assert.Equal("DEPT_BGD", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("DEPT_BGD", data.GetProperty("mdept_DepartmentCode").GetString());
        Assert.Equal("Ban Giám Đốc", data.GetProperty("mdept_DepartmentName").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.False(data.GetProperty("IsParent").GetBoolean());

        // Kiểm tra đối tượng Sys_User lồng bên trong
        var nestedUser = data.GetProperty("Sys_User");
        Assert.Equal("U01", nestedUser.GetProperty("UserCode").GetString());

        // Danh sách NNT: isSysadmin = "0" mặc định -> 1 NNT (0100000001)
        var nntList = data.GetProperty("ListMst_NNTCur");
        Assert.Equal(1, nntList.GetArrayLength());
        Assert.Equal("0100000001", nntList[0].GetProperty("MST").GetString());

        // Danh sách phòng ban: 2 phòng ban active được thụt lề cây HLevel
        var deptList = data.GetProperty("ListMst_Department");
        Assert.Equal(2, deptList.GetArrayLength());
        Assert.Equal("DEPT_BGD", deptList[0].GetProperty("DepartmentCode").GetString());
        Assert.Equal(0, deptList[0].GetProperty("HLevel").GetInt32());
        Assert.Equal("Ban Giám Đốc", deptList[0].GetProperty("HlevelTitle").GetString());
        Assert.Equal("DEPT_KT", deptList[1].GetProperty("DepartmentCode").GetString());
        Assert.Equal(1, deptList[1].GetProperty("HLevel").GetInt32());
        Assert.Equal("|--Phòng Kế Toán", deptList[1].GetProperty("HlevelTitle").GetString());

        // Danh sách khách hàng đại lý: CUS001 thuộc OrgID 7200000001, FlagDealer=1, FlagActive=1
        var custList = data.GetProperty("ListMst_Customer");
        Assert.Equal(1, custList.GetArrayLength());
        Assert.Equal("CUS001", custList[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("KH001", custList[0].GetProperty("CustomerCode").GetString());
        Assert.Equal("Công ty Cổ phần Alpha", custList[0].GetProperty("CustomerName").GetString());
        Assert.Equal("1", custList[0].GetProperty("FlagDealer").GetString());
    }

    [Fact]
    public async Task Update_SysAdmin_FilterComparison_ReturnsDifferentNNTCounts()
    {
        // Bộ lọc 1: Người dùng thông thường (IsSysAdmin = "0") -> 1 NNT (0100000001)
        var reqNormal = new Sys_User_UpdateReq
        {
            UserCode = "U01",
            IsSysAdmin = "0"
        };
        var resNormal = await _controller.Update(reqNormal);
        var okNormal = Assert.IsType<OkObjectResult>(resNormal);
        var docNormal = JsonDocument.Parse(JsonSerializer.Serialize(okNormal.Value));
        Assert.Equal(1, docNormal.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());

        // Bộ lọc 2: SysAdmin (IsSysAdmin = "1") -> 2 NNT (0100000001, 0100000002) - khác số dòng với bộ lọc 1
        var reqAdmin = new Sys_User_UpdateReq
        {
            UserCode = "U01",
            IsSysAdmin = "1"
        };
        var resAdmin = await _controller.Update(reqAdmin);
        var okAdmin = Assert.IsType<OkObjectResult>(resAdmin);
        var docAdmin = JsonDocument.Parse(JsonSerializer.Serialize(okAdmin.Value));
        Assert.Equal(2, docAdmin.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());
    }

    [Fact]
    public async Task Update_ViaModelJsonString_Success()
    {
        // Hỗ trợ client truyền model qua chuỗi JSON (Gate MVC)
        var modelJson = JsonSerializer.Serialize(new
        {
            UserCode = "u02"
        });

        var req = new Sys_User_UpdateReq
        {
            Model = modelJson
        };

        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("U02", data.GetProperty("UserCode").GetString());
        Assert.Equal("tester@solution.hn", data.GetProperty("UserName").GetString());
        Assert.Equal("DEPT_KT", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Phòng Kế Toán", data.GetProperty("mdept_DepartmentName").GetString());
    }

    [Fact]
    public async Task Edit_NullReq_ReturnsSuccessTrueWithMessageEmptyUserCode()
    {
        // Khi req null (body rỗng): trả về Success = true, Messages = ["Mã người dùng trống!"] (gate: Sys_UserController.cs:649-650, 720-721)
        var result = await _controller.Edit(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã người dùng trống!", root.GetProperty("Title").GetString());
        Assert.Equal("Mã người dùng trống!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Edit_EmptyUserCode_ReturnsSuccessTrueWithMessageEmptyUserCode()
    {
        // Khi UserCode rỗng: trả về Success = true, Messages = ["Mã người dùng trống!"] (gate: Sys_UserController.cs:649-650, 720-721)
        var req = new Sys_User_EditReq { UserCode = "   " };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã người dùng trống!", root.GetProperty("Title").GetString());
        Assert.Equal("Mã người dùng trống!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Edit_NonExistentUserCode_ReturnsSuccessTrueWithMessageNotFound()
    {
        // Khi UserCode không tồn tại trong hệ thống: trả về Success = true, Messages = ["Mã người dùng '<UserCode>' không có trong hệ thống!"] (gate: Sys_UserController.cs:676, 714-716)
        var req = new Sys_User_EditReq { UserCode = "NON_EXISTENT" };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã người dùng 'NON_EXISTENT' không có trong hệ thống!", root.GetProperty("Title").GetString());
        Assert.Equal("Mã người dùng 'NON_EXISTENT' không có trong hệ thống!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Edit_BizGuard_EmptyUserName_ReturnsSys_User_Update_InvalidUserName()
    {
        // Biz guard: Nếu cập nhật UserName mà strUserName.Length < 1 -> ném TError.ErridQContract.Sys_User_Update_InvalidUserName (System.cs:11356-11365)
        var req = new Sys_User_EditReq
        {
            UserCode = "U01",
            UserName = "   ",
            Ft_Cols_Upd = "SYS_USER.USERNAME,"
        };
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_Update_InvalidUserName", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_ValidRoundTrip_UpdatesDatabaseAndReturnsSuccess()
    {
        // Gán mật khẩu trước khi sửa để kiểm tra mật khẩu trong DB không bị ghi đè
        var u03 = await _db.Set<Sys_User>().FirstAsync(u => u.UserCode == "U03");
        u03.UserPassword = "Password123";
        await _db.SaveChangesAsync();
        _db.Entry(u03).State = EntityState.Detached;

        // Cập nhật thành công thông tin người dùng U03
        var req = new Sys_User_EditReq
        {
            UserCode = "u03",
            UserName = "Kiem Thu Vien 03 Cap Nhat",
            PhoneNo = "0988776655",
            Position = "Chuyen Vien Test",
            DepartmentCode = "DEPT_KT",
            FlagActive = "1",
            FlagSysAdmin = "0",
            FlagNNTAdmin = "1"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Sửa người dùng thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("Sửa người dùng thành công!", root.GetProperty("Messages")[0].GetString());
        Assert.Equal("/Sys_User", root.GetProperty("RedirectUrl").GetString());

        var data = root.GetProperty("Data");
        Assert.Equal("U03", data.GetProperty("UserCode").GetString());
        Assert.Equal("Kiem Thu Vien 03 Cap Nhat", data.GetProperty("UserName").GetString());
        Assert.Equal("0988776655", data.GetProperty("PhoneNo").GetString());
        Assert.Equal("Chuyen Vien Test", data.GetProperty("Position").GetString());
        Assert.Equal("DEPT_KT", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("*********", data.GetProperty("UserPassword").GetString());

        // Kiểm tra trong database: các cột cập nhật thành công, mật khẩu trong DB không bị sửa
        var dbUser = await _db.Set<Sys_User>().AsNoTracking().FirstOrDefaultAsync(u => u.UserCode == "U03");
        Assert.NotNull(dbUser);
        Assert.Equal("Kiem Thu Vien 03 Cap Nhat", dbUser.UserName);
        Assert.Equal("0988776655", dbUser.PhoneNo);
        Assert.Equal("Chuyen Vien Test", dbUser.Position);
        Assert.Equal("DEPT_KT", dbUser.DepartmentCode);
        Assert.Equal("1", dbUser.FlagNNTAdmin);
        // Mật khẩu thật trong DB vẫn giữ nguyên
        Assert.Equal("Password123", dbUser.UserPassword);
    }

    [Fact]
    public async Task Edit_ViaModelJsonString_Success()
    {
        // Hỗ trợ client truyền model qua chuỗi JSON (Gate MVC: Sys_UserController.cs:636, 648)
        var modelJson = JsonSerializer.Serialize(new
        {
            UserCode = "u02",
            UserName = "Tester 02 Json Updated",
            PhoneNo = "0912345678"
        });

        var req = new Sys_User_EditReq
        {
            Model = modelJson
        };

        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Sửa người dùng thành công!", root.GetProperty("Messages")[0].GetString());

        var dbUser = await _db.Set<Sys_User>().AsNoTracking().FirstOrDefaultAsync(u => u.UserCode == "U02");
        Assert.NotNull(dbUser);
        Assert.Equal("Tester 02 Json Updated", dbUser.UserName);
        Assert.Equal("0912345678", dbUser.PhoneNo);
    }

    [Fact]
    public async Task Detail_NullReq_ReturnsSuccessTrueAndNullData()
    {
        // Khi req null (body rỗng): trả về Data = null (gate: View(new Sys_User()))
        var result = await _controller.Detail(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_EmptyReq_ReturnsSuccessTrueAndNullData()
    {
        // Khi req rỗng (không có UserCode): trả về Data = null (gate: View(new Sys_User()))
        var req = new Sys_User_DetailReq();
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_Guard_InvalidWAUserCode_ReturnsSys_User_BizInvalidUserAbility()
    {
        // Guard kiểm tra quyền xem của người dùng: WAUserCode không tồn tại
        // ném TError.ErridQContract.Sys_User_BizInvalidUserAbility (System.Mix.cs:74)
        var req = new Sys_User_DetailReq
        {
            UserCode = "U01",
            WAUserCode = "INVALID_USER"
        };
        var result = await _controller.Detail(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_BizInvalidUserAbility", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Detail_NonExistentUserCode_ReturnsSuccessTrueAndNullData()
    {
        // Khi UserCode không tồn tại trong DB: trả về Data = null (gate: View(new Sys_User()))
        var req = new Sys_User_DetailReq { UserCode = "NON_EXISTENT_USER" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_ValidUserCode_ReturnsUserDataWithMaskedPasswordAndLists()
    {
        // Tìm thấy UserCode u01 (kiểm tra không phân biệt hoa thường)
        var req = new Sys_User_DetailReq { UserCode = "u01" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal("U01", data.GetProperty("UserCode").GetString());
        Assert.Equal("admin@solution.hn", data.GetProperty("UserName").GetString());
        // Mật khẩu che bằng Default_PasswordMask ("*********") (System.cs:6816)
        Assert.Equal("*********", data.GetProperty("UserPassword").GetString());
        Assert.Equal("0901234567", data.GetProperty("PhoneNo").GetString());
        Assert.Equal("admin@solution.hn", data.GetProperty("EMail").GetString());
        Assert.Equal("DEPT_BGD", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("DEPT_BGD", data.GetProperty("mdept_DepartmentCode").GetString());
        Assert.Equal("Ban Giám Đốc", data.GetProperty("mdept_DepartmentName").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.False(data.GetProperty("IsParent").GetBoolean());

        // Kiểm tra đối tượng Sys_User lồng bên trong
        var nestedUser = data.GetProperty("Sys_User");
        Assert.Equal("U01", nestedUser.GetProperty("UserCode").GetString());

        // Danh sách NNT: isSysadmin = "0" mặc định -> 1 NNT (0100000001)
        var nntList = data.GetProperty("ListMst_NNTCur");
        Assert.Equal(1, nntList.GetArrayLength());
        Assert.Equal("0100000001", nntList[0].GetProperty("MST").GetString());

        // Danh sách phòng ban: 2 phòng ban active được thụt lề cây HLevel
        var deptList = data.GetProperty("ListMst_Department");
        Assert.Equal(2, deptList.GetArrayLength());
        Assert.Equal("DEPT_BGD", deptList[0].GetProperty("DepartmentCode").GetString());
        Assert.Equal(0, deptList[0].GetProperty("HLevel").GetInt32());
        Assert.Equal("Ban Giám Đốc", deptList[0].GetProperty("HlevelTitle").GetString());
        Assert.Equal("DEPT_KT", deptList[1].GetProperty("DepartmentCode").GetString());
        Assert.Equal(1, deptList[1].GetProperty("HLevel").GetInt32());
        Assert.Equal("|--Phòng Kế Toán", deptList[1].GetProperty("HlevelTitle").GetString());

        // Danh sách khách hàng đại lý: CUS001 thuộc OrgID 7200000001, FlagDealer=1, FlagActive=1
        var custList = data.GetProperty("ListMst_Customer");
        Assert.Equal(1, custList.GetArrayLength());
        Assert.Equal("CUS001", custList[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("KH001", custList[0].GetProperty("CustomerCode").GetString());
        Assert.Equal("Công ty Cổ phần Alpha", custList[0].GetProperty("CustomerName").GetString());
        Assert.Equal("1", custList[0].GetProperty("FlagDealer").GetString());
    }

    [Fact]
    public async Task Detail_SysAdmin_FilterComparison_ReturnsDifferentNNTCounts()
    {
        // Bộ lọc 1: Người dùng thông thường (IsSysAdmin = "0") -> 1 NNT (0100000001)
        var reqNormal = new Sys_User_DetailReq
        {
            UserCode = "U01",
            IsSysAdmin = "0"
        };
        var resNormal = await _controller.Detail(reqNormal);
        var okNormal = Assert.IsType<OkObjectResult>(resNormal);
        var docNormal = JsonDocument.Parse(JsonSerializer.Serialize(okNormal.Value));
        Assert.Equal(1, docNormal.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());

        // Bộ lọc 2: SysAdmin (IsSysAdmin = "1") -> 2 NNT (0100000001, 0100000002) - khác số dòng với bộ lọc 1
        var reqAdmin = new Sys_User_DetailReq
        {
            UserCode = "U01",
            IsSysAdmin = "1"
        };
        var resAdmin = await _controller.Detail(reqAdmin);
        var okAdmin = Assert.IsType<OkObjectResult>(resAdmin);
        var docAdmin = JsonDocument.Parse(JsonSerializer.Serialize(okAdmin.Value));
        Assert.Equal(2, docAdmin.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());
    }

    [Fact]
    public async Task Detail_ViaModelJsonString_Success()
    {
        // Hỗ trợ client truyền model qua chuỗi JSON (Gate MVC)
        var modelJson = JsonSerializer.Serialize(new
        {
            UserCode = "u02"
        });

        var req = new Sys_User_DetailReq
        {
            Model = modelJson
        };

        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("U02", data.GetProperty("UserCode").GetString());
        Assert.Equal("tester@solution.hn", data.GetProperty("UserName").GetString());
        Assert.Equal("DEPT_KT", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Phòng Kế Toán", data.GetProperty("mdept_DepartmentName").GetString());
    }

    [Fact]
    public async Task Delete_NullReq_ReturnsSuccessTrueWithMessageEmptyUserCode()
    {
        // Khi req null (body rỗng): trả về Success = true, Messages = ["Mã người dùng trống!"] (gate: Sys_UserController.cs:892, 939-943)
        var result = await _controller.Delete(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã người dùng trống!", root.GetProperty("Title").GetString());
        Assert.Equal("Mã người dùng trống!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Delete_EmptyUserCode_ReturnsSuccessTrueWithMessageEmptyUserCode()
    {
        // Khi UserCode rỗng: trả về Success = true, Messages = ["Mã người dùng trống!"] (gate: Sys_UserController.cs:892, 939-943)
        var req = new Sys_User_DeleteReq { UserCode = "   " };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã người dùng trống!", root.GetProperty("Title").GetString());
        Assert.Equal("Mã người dùng trống!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Delete_NonExistentUserCode_ReturnsSuccessTrueWithMessageNotFound()
    {
        // Khi UserCode không tồn tại trong hệ thống: trả về Success = true, Messages = ["Mã người dùng '<UserCode>' không có trong hệ thống!"] (gate: Sys_UserController.cs:924, 933-936)
        var req = new Sys_User_DeleteReq { UserCode = "NON_EXISTENT" };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã người dùng 'NON_EXISTENT' không có trong hệ thống!", root.GetProperty("Title").GetString());
        Assert.Equal("Mã người dùng 'NON_EXISTENT' không có trong hệ thống!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Delete_BizGuard_DTimeUsedIsNotNull_ReturnsSys_User_Delete_DTimeUsedIsNotNull()
    {
        // Biz guard: Nếu người dùng có DTimeUsed != null ("User đã tham gia nghiệp vụ")
        // ném TError.ErridQContract.Sys_User_Delete_DTimeUsedIsNotNull (System.cs:12301-12314)
        var testUser = new Sys_User
        {
            UserCode = "U_GUARD_DTIME",
            UserName = "User With DTimeUsed",
            MST = "0100000001",
            DTimeUsed = "2026-01-01 10:00:00",
            FlagActive = "1"
        };
        _db.Set<Sys_User>().Add(testUser);
        await _db.SaveChangesAsync();
        _db.Entry(testUser).State = EntityState.Detached;

        var req = new Sys_User_DeleteReq { UserCode = "u_guard_dtime" };
        var result = await _controller.Delete(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_Delete_DTimeUsedIsNotNull", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_BizGuard_NNTNotFound_ReturnsMst_NNT_CheckDB_NNTNotFound()
    {
        // Biz guard: Nếu MST của người dùng không có trong Mst_NNT
        // ném TError.ErridQContract.Mst_NNT_CheckDB_NNTNotFound (System.cs:12321-12328, Master.cs:12034)
        var testUser = new Sys_User
        {
            UserCode = "U_GUARD_NNT_NOT_FOUND",
            UserName = "User Invalid MST",
            MST = "9999999999",
            FlagActive = "1"
        };
        _db.Set<Sys_User>().Add(testUser);
        await _db.SaveChangesAsync();
        _db.Entry(testUser).State = EntityState.Detached;

        var req = new Sys_User_DeleteReq { UserCode = "u_guard_nnt_not_found" };
        var result = await _controller.Delete(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_NNTNotFound", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_BizGuard_FlagActiveNotMatched_ReturnsMst_NNT_CheckDB_FlagActiveNotMatched()
    {
        // Biz guard: Nếu NNT có FlagActive != '1'
        // ném TError.ErridQContract.Mst_NNT_CheckDB_FlagActiveNotMatched (Master.cs:12061)
        var inactiveNNT = new Mst_NNT
        {
            MST = "0100000099",
            NNTFullName = "Công ty Inactive",
            FlagActive = "0"
        };
        _db.Set<Mst_NNT>().Add(inactiveNNT);

        var testUser = new Sys_User
        {
            UserCode = "U_GUARD_NNT_INACTIVE",
            UserName = "User Inactive NNT",
            MST = "0100000099",
            FlagActive = "1"
        };
        _db.Set<Sys_User>().Add(testUser);
        await _db.SaveChangesAsync();
        _db.Entry(inactiveNNT).State = EntityState.Detached;
        _db.Entry(testUser).State = EntityState.Detached;

        var req = new Sys_User_DeleteReq { UserCode = "u_guard_nnt_inactive" };
        var result = await _controller.Delete(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_FlagActiveNotMatched", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_ValidRoundTrip_DeletesUserAndCascadesSuccessfully()
    {
        // Chuẩn bị dữ liệu: người dùng trong Sys_User, nhóm trong Sys_UserInGroup, thông báo trong Mst_ManageNotify và Map_UserInNotifyType
        var testUser = new Sys_User
        {
            UserCode = "U_DELETE_ROUNDTRIP",
            UserName = "User To Be Deleted",
            MST = "0100000001",
            FlagActive = "1"
        };
        var userInGroup = new Sys_UserInGroup
        {
            UserCode = "U_DELETE_ROUNDTRIP",
            GroupCode = "ADMIN_GROUP"
        };
        var notify = new Mst_ManageNotify
        {
            UserCode = "U_DELETE_ROUNDTRIP",
            UserName = "User To Be Deleted"
        };
        var mapNotify = new Map_UserInNotifyType
        {
            UserCode = "U_DELETE_ROUNDTRIP",
            NotifyType = "EMAIL",
            FlagNotify = "1"
        };

        _db.Set<Sys_User>().Add(testUser);
        _db.Set<Sys_UserInGroup>().Add(userInGroup);
        _db.Set<Mst_ManageNotify>().Add(notify);
        _db.Set<Map_UserInNotifyType>().Add(mapNotify);
        await _db.SaveChangesAsync();
        _db.Entry(testUser).State = EntityState.Detached;
        _db.Entry(userInGroup).State = EntityState.Detached;
        _db.Entry(notify).State = EntityState.Detached;
        _db.Entry(mapNotify).State = EntityState.Detached;

        // Thực hiện xoá qua action Delete
        var req = new Sys_User_DeleteReq { UserCode = "u_delete_roundtrip" };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Xóa người dùng thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("Xóa người dùng thành công!", root.GetProperty("Messages")[0].GetString());
        Assert.Equal("/Sys_User", root.GetProperty("RedirectUrl").GetString());

        // Kiểm tra database: tất cả các bảng liên quan đã bị xoá
        var checkUser = await _db.Set<Sys_User>().AsNoTracking().FirstOrDefaultAsync(u => u.UserCode == "U_DELETE_ROUNDTRIP");
        Assert.Null(checkUser);

        var checkGroup = await _db.Set<Sys_UserInGroup>().AsNoTracking().FirstOrDefaultAsync(g => g.UserCode == "U_DELETE_ROUNDTRIP");
        Assert.Null(checkGroup);

        var checkNotify = await _db.Set<Mst_ManageNotify>().AsNoTracking().FirstOrDefaultAsync(m => m.UserCode == "U_DELETE_ROUNDTRIP");
        Assert.Null(checkNotify);

        var checkMapNotify = await _db.Set<Map_UserInNotifyType>().AsNoTracking().FirstOrDefaultAsync(m => m.UserCode == "U_DELETE_ROUNDTRIP");
        Assert.Null(checkMapNotify);
    }

    [Fact]
    public async Task Delete_ViaModelJsonString_Success()
    {
        // Chuẩn bị dữ liệu
        var testUser = new Sys_User
        {
            UserCode = "U_DELETE_JSON",
            UserName = "User Delete Via Json",
            MST = "0100000001",
            FlagActive = "1"
        };
        _db.Set<Sys_User>().Add(testUser);
        await _db.SaveChangesAsync();
        _db.Entry(testUser).State = EntityState.Detached;

        // Xoá qua chuỗi JSON Model
        var modelJson = JsonSerializer.Serialize(new
        {
            UserCode = "u_delete_json"
        });

        var req = new Sys_User_DeleteReq
        {
            Model = modelJson
        };

        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Xóa người dùng thành công!", root.GetProperty("Messages")[0].GetString());

        var checkUser = await _db.Set<Sys_User>().AsNoTracking().FirstOrDefaultAsync(u => u.UserCode == "U_DELETE_JSON");
        Assert.Null(checkUser);
    }

    [Fact]
    public void ShowPopupChangePassword_NullReq_ReturnsSuccessTrueAndNullUserCode()
    {
        // Khi req null (body rỗng): trả về Success = true, UserCode = null (gate: Sys_UserController.cs:956-957)
        var result = _controller.ShowPopupChangePassword(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").GetProperty("UserCode").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("UserCode").ValueKind);
        Assert.Equal("<h2>ShowPopupChangePassword</h2>", root.GetProperty("Html").GetString());
    }

    [Fact]
    public void ShowPopupChangePassword_EmptyReq_ReturnsSuccessTrueAndNullUserCode()
    {
        // Khi req rỗng (không truyền UserCode): trả về Success = true, UserCode = null
        var req = new Sys_User_ShowPopupChangePasswordReq();
        var result = _controller.ShowPopupChangePassword(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").GetProperty("UserCode").ValueKind);
    }

    [Fact]
    public void ShowPopupChangePassword_ValidUserCode_ReturnsSuccessTrueAndUppercaseUserCode()
    {
        // Khi truyền UserCode: chuẩn hoá mã viết hoa và trả về Success = true kèm UserCode
        var req = new Sys_User_ShowPopupChangePasswordReq { UserCode = "u01" };
        var result = _controller.ShowPopupChangePassword(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("U01", root.GetProperty("UserCode").GetString());
        Assert.Equal("U01", root.GetProperty("Data").GetProperty("UserCode").GetString());
        Assert.Equal("<h2>ShowPopupChangePassword</h2>", root.GetProperty("Html").GetString());
    }

    [Fact]
    public void ShowPopupChangePassword_ViaModelJson_Success()
    {
        // Hỗ trợ truyền qua chuỗi JSON Model
        var modelJson = JsonSerializer.Serialize(new { UserCode = "u02" });
        var req = new Sys_User_ShowPopupChangePasswordReq { Model = modelJson };
        var result = _controller.ShowPopupChangePassword(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("U02", root.GetProperty("Data").GetProperty("UserCode").GetString());
    }

    [Fact]
    public void ShowPopupChangePassword_Comparison_ReturnsDifferentUserCodes()
    {
        // So sánh 2 ca đầu vào khác nhau: u01 vs u02 -> kết quả trả về UserCode tương ứng
        var req1 = new Sys_User_ShowPopupChangePasswordReq { UserCode = "u01" };
        var res1 = _controller.ShowPopupChangePassword(req1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(res1).Value));
        Assert.Equal("U01", doc1.RootElement.GetProperty("Data").GetProperty("UserCode").GetString());

        var req2 = new Sys_User_ShowPopupChangePasswordReq { UserCode = "u02" };
        var res2 = _controller.ShowPopupChangePassword(req2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(res2).Value));
        Assert.Equal("U02", doc2.RootElement.GetProperty("Data").GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task RealServer_VerifyAllEndpointsAsync()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_sys_user_{Guid.NewGuid():N}.db");
        var dllPath = typeof(Sys_UserControllerPort).Assembly.Location;
        if (!File.Exists(dllPath))
        {
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
        }
        if (!File.Exists(dllPath))
        {
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "MiniContract.dll"));
        }
        Assert.True(File.Exists(dllPath), $"Cannot find MiniContract.dll at {dllPath}");

        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(dllPath),
        };
        var freePort = FreePort.Get();
        psi.Environment["PORT"] = freePort.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort}"), Timeout = TimeSpan.FromSeconds(5) };
            bool started = false;
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(500);
                try
                {
                    var res = await http.GetAsync("/healthz");
                    if (res.IsSuccessStatusCode)
                    {
                        started = true;
                        break;
                    }
                }
                catch { }
            }
            Assert.True(started, "MiniContract server failed to start within 15 seconds.");

            // 1. Ca body rỗng: POST {} -> 200 OK, ItemCount=3, mật khẩu đã che *********
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/sys_user/index", req1);
            Assert.True(res1.IsSuccessStatusCode);
            var json1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(json1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(3, data1.GetProperty("ItemCount").GetInt32());
            var list1 = data1.GetProperty("DataList");
            Assert.Equal(3, list1.GetArrayLength());
            Assert.Equal("U01", list1[0].GetProperty("UserCode").GetString());
            Assert.Equal("*********", list1[0].GetProperty("UserPassword").GetString());
            Assert.Equal("DEPT_BGD", list1[0].GetProperty("mdept_DepartmentCode").GetString());
            Assert.Equal("Ban Giám Đốc", list1[0].GetProperty("mdept_DepartmentName").GetString());
            Assert.Equal("7200000001", list1[0].GetProperty("OrgID").GetString());

            // 2. Ca guard: WAUserCode không tồn tại -> 400 BadRequest (Sys_User_BizInvalidUserAbility)
            var req2 = new StringContent(JsonSerializer.Serialize(new { WAUserCode = "INVALID_USER" }), Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/sys_user/index", req2);
            Assert.False(res2.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res2.StatusCode);
            var doc2 = JsonDocument.Parse(await res2.Content.ReadAsStringAsync());
            Assert.False(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_User_BizInvalidUserAbility", doc2.RootElement.GetProperty("ErrorCode").GetString());

            // 3. Ca bộ lọc 1: UserName = admin -> 1 dòng
            var req3 = new StringContent(JsonSerializer.Serialize(new { UserName = "admin" }), Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/sys_user/index", req3);
            Assert.True(res3.IsSuccessStatusCode);
            var doc3 = JsonDocument.Parse(await res3.Content.ReadAsStringAsync());
            Assert.Equal(1, doc3.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal("U01", doc3.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("UserCode").GetString());

            // 4. Ca bộ lọc 2: UserName = solution.hn -> 3 dòng (khác số dòng với bộ lọc 1)
            var req4 = new StringContent(JsonSerializer.Serialize(new { UserName = "solution.hn" }), Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/sys_user/index", req4);
            Assert.True(res4.IsSuccessStatusCode);
            var doc4 = JsonDocument.Parse(await res4.Content.ReadAsStringAsync());
            Assert.Equal(3, doc4.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 5. Ca bộ lọc MST: 0100000001 -> 2 dòng (U01, U02)
            var req5 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000001" }), Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/sys_user/index", req5);
            Assert.True(res5.IsSuccessStatusCode);
            var doc5 = JsonDocument.Parse(await res5.Content.ReadAsStringAsync());
            Assert.Equal(2, doc5.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 6. Ca bộ lọc không tồn tại: UserName = nonexistent -> 0 dòng
            var req6 = new StringContent(JsonSerializer.Serialize(new { UserName = "nonexistent" }), Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/sys_user/index", req6);
            Assert.True(res6.IsSuccessStatusCode);
            var doc6 = JsonDocument.Parse(await res6.Content.ReadAsStringAsync());
            Assert.Equal(0, doc6.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, doc6.RootElement.GetProperty("Data").GetProperty("PageCount").GetInt32());
            Assert.Equal(0, doc6.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());

            // 7. Ca action alias search: POST /api/sys_user/search với body rỗng -> ItemCount=3
            var req7 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/sys_user/search", req7);
            Assert.True(res7.IsSuccessStatusCode);
            var doc7 = JsonDocument.Parse(await res7.Content.ReadAsStringAsync());
            Assert.True(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(3, doc7.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 8. Ca khởi tạo Create với body rỗng: POST /api/sys_user/create -> 200 OK
            var req8 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/sys_user/create", req8);
            var content8 = await res8.Content.ReadAsStringAsync();
            Assert.True(res8.IsSuccessStatusCode, $"res8 failed with status {res8.StatusCode}: {content8}");
            var doc8 = JsonDocument.Parse(content8);
            Assert.True(doc8.RootElement.GetProperty("Success").GetBoolean());
            var initData = doc8.RootElement.GetProperty("Data");
            Assert.False(initData.GetProperty("IsParent").GetBoolean());
            Assert.Equal(1, initData.GetProperty("ListMst_NNTCur").GetArrayLength());
            // Kiểm tra cây phòng ban có 2 phần tử FlagActive = '1' và thụt lề HLevel
            var deptArr = initData.GetProperty("ListMst_Department");
            Assert.Equal(2, deptArr.GetArrayLength());
            Assert.Equal("DEPT_BGD", deptArr[0].GetProperty("DepartmentCode").GetString());
            Assert.Equal("Ban Giám Đốc", deptArr[0].GetProperty("HlevelTitle").GetString());
            Assert.Equal("DEPT_KT", deptArr[1].GetProperty("DepartmentCode").GetString());
            Assert.Equal("|--Phòng Kế Toán", deptArr[1].GetProperty("HlevelTitle").GetString());

            // 9. Ca khởi tạo Create với IsSysAdmin = 1 -> lấy 2 NNT active
            var req9 = new StringContent(JsonSerializer.Serialize(new { IsSysAdmin = "1" }), Encoding.UTF8, "application/json");
            var res9 = await http.PostAsync("/api/sys_user/create", req9);
            Assert.True(res9.IsSuccessStatusCode);
            var doc9 = JsonDocument.Parse(await res9.Content.ReadAsStringAsync());
            Assert.Equal(2, doc9.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());

            // 10. Ca guard Create: UserCode rỗng -> 400 BadRequest (Sys_User_Create_InvalidUserCode)
            var req10 = new StringContent(JsonSerializer.Serialize(new { Save = "1", UserCode = "" }), Encoding.UTF8, "application/json");
            var res10 = await http.PostAsync("/api/sys_user/create", req10);
            Assert.False(res10.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res10.StatusCode);
            var doc10 = JsonDocument.Parse(await res10.Content.ReadAsStringAsync());
            Assert.False(doc10.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_User_Create_InvalidUserCode", doc10.RootElement.GetProperty("ErrorCode").GetString());

            // 11. Ca guard Create: UserCode trùng -> 400 BadRequest (Sys_User_CheckDB_UserExist)
            var req11 = new StringContent(JsonSerializer.Serialize(new { Save = "1", UserCode = "U01", MST = "0100000001" }), Encoding.UTF8, "application/json");
            var res11 = await http.PostAsync("/api/sys_user/create", req11);
            Assert.False(res11.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res11.StatusCode);
            var doc11 = JsonDocument.Parse(await res11.Content.ReadAsStringAsync());
            Assert.False(doc11.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_UserExist", doc11.RootElement.GetProperty("ErrorCode").GetString());

            // 12. Ca guard Create: MST không tồn tại -> 400 BadRequest (Mst_NNT_CheckDB_NNTNotFound)
            var req12 = new StringContent(JsonSerializer.Serialize(new { Save = "1", UserCode = "U_FAIL", MST = "9999999999" }), Encoding.UTF8, "application/json");
            var res12 = await http.PostAsync("/api/sys_user/create", req12);
            Assert.False(res12.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res12.StatusCode);
            var doc12 = JsonDocument.Parse(await res12.Content.ReadAsStringAsync());
            Assert.False(doc12.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_NNTNotFound", doc12.RootElement.GetProperty("ErrorCode").GetString());

            // 13. Ca ghi thành công round-trip: POST /api/sys_user/create -> 200 OK
            var req13 = new StringContent(JsonSerializer.Serialize(new
            {
                Save = "1",
                UserCode = "u_server_created",
                UserName = "Server Created User",
                UserPassword = "Password789",
                PhoneNo = "0911223344",
                EMail = "server_user@solution.hn",
                MST = "0100000001",
                DepartmentCode = "DEPT_KT"
            }), Encoding.UTF8, "application/json");
            var res13 = await http.PostAsync("/api/sys_user/create", req13);
            Assert.True(res13.IsSuccessStatusCode);
            var doc13 = JsonDocument.Parse(await res13.Content.ReadAsStringAsync());
            Assert.True(doc13.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("/Sys_User", doc13.RootElement.GetProperty("RedirectUrl").GetString());

            // 14. Kiểm tra người dùng vừa tạo xuất hiện trong danh sách khi tìm kiếm qua Index
            var req14 = new StringContent(JsonSerializer.Serialize(new { UserName = "Server Created User" }), Encoding.UTF8, "application/json");
            var res14 = await http.PostAsync("/api/sys_user/index", req14);
            Assert.True(res14.IsSuccessStatusCode);
            var doc14 = JsonDocument.Parse(await res14.Content.ReadAsStringAsync());
            Assert.Equal(1, doc14.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            var createdItem = doc14.RootElement.GetProperty("Data").GetProperty("DataList")[0];
            Assert.Equal("U_SERVER_CREATED", createdItem.GetProperty("UserCode").GetString());
            Assert.Equal("Server Created User", createdItem.GetProperty("UserName").GetString());
            Assert.Equal("Phòng Kế Toán", createdItem.GetProperty("mdept_DepartmentName").GetString());
            Assert.Equal("*********", createdItem.GetProperty("UserPassword").GetString());

            // 15. Action LoadMstCustomer: POST /api/sys_user/loadmstcustomer với body rỗng -> 200 OK, Data = []
            var req15 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res15 = await http.PostAsync("/api/sys_user/loadmstcustomer", req15);
            Assert.True(res15.IsSuccessStatusCode);
            var doc15 = JsonDocument.Parse(await res15.Content.ReadAsStringAsync());
            Assert.True(doc15.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(0, doc15.RootElement.GetProperty("Data").GetArrayLength());

            // 16. Action LoadMstCustomer: POST với MST không tồn tại -> 200 OK, Data = []
            var req16 = new StringContent(JsonSerializer.Serialize(new { MST = "9999999999" }), Encoding.UTF8, "application/json");
            var res16 = await http.PostAsync("/api/sys_user/loadmstcustomer", req16);
            Assert.True(res16.IsSuccessStatusCode);
            var doc16 = JsonDocument.Parse(await res16.Content.ReadAsStringAsync());
            Assert.True(doc16.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(0, doc16.RootElement.GetProperty("Data").GetArrayLength());

            // 17. Action LoadMstCustomer: POST với MST hợp lệ 0100000001 -> 200 OK, Data có 1 phần tử (CUS001)
            var req17 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000001", OrgID = "7200000001" }), Encoding.UTF8, "application/json");
            var res17 = await http.PostAsync("/api/sys_user/loadmstcustomer", req17);
            Assert.True(res17.IsSuccessStatusCode);
            var doc17 = JsonDocument.Parse(await res17.Content.ReadAsStringAsync());
            Assert.True(doc17.RootElement.GetProperty("Success").GetBoolean());
            var custList17 = doc17.RootElement.GetProperty("Data");
            Assert.Equal(1, custList17.GetArrayLength());
            Assert.Equal("CUS001", custList17[0].GetProperty("CustomerCodeSys").GetString());
            Assert.Equal("Công ty Cổ phần Alpha", custList17[0].GetProperty("CustomerName").GetString());
            Assert.Equal("1", custList17[0].GetProperty("FlagDealer").GetString());

            // 18. Action LoadMstCustomer alias: POST /api/sys_user/loadmst_customer -> 200 OK, Data có 1 phần tử
            var req18 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000001", OrgID = "7200000001" }), Encoding.UTF8, "application/json");
            var res18 = await http.PostAsync("/api/sys_user/loadmst_customer", req18);
            Assert.True(res18.IsSuccessStatusCode);
            var doc18 = JsonDocument.Parse(await res18.Content.ReadAsStringAsync());
            Assert.True(doc18.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(1, doc18.RootElement.GetProperty("Data").GetArrayLength());

            // 19. Action LoadMstCustomer bộ lọc khác biệt: Người dùng thường không khớp OrgID -> 0 dòng
            var req19 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000001", OrgID = "7200000002", IsSysAdmin = "0" }), Encoding.UTF8, "application/json");
            var res19 = await http.PostAsync("/api/sys_user/loadmstcustomer", req19);
            Assert.True(res19.IsSuccessStatusCode);
            var doc19 = JsonDocument.Parse(await res19.Content.ReadAsStringAsync());
            Assert.Equal(0, doc19.RootElement.GetProperty("Data").GetArrayLength());

            // 20. Action LoadMstCustomer bộ lọc khác biệt: SysAdmin khớp mọi NNT -> 1 dòng (khác số dòng)
            var req20 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000001", OrgID = "7200000002", IsSysAdmin = "1" }), Encoding.UTF8, "application/json");
            var res20 = await http.PostAsync("/api/sys_user/loadmstcustomer", req20);
            Assert.True(res20.IsSuccessStatusCode);
            var doc20 = JsonDocument.Parse(await res20.Content.ReadAsStringAsync());
            Assert.Equal(1, doc20.RootElement.GetProperty("Data").GetArrayLength());

            // 21. Action StopService: POST /api/sys_user/stopservice với body rỗng -> 200 OK, Success = true, Data = null
            var req21 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res21 = await http.PostAsync("/api/sys_user/stopservice", req21);
            Assert.True(res21.IsSuccessStatusCode);
            var doc21 = JsonDocument.Parse(await res21.Content.ReadAsStringAsync());
            Assert.True(doc21.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc21.RootElement.GetProperty("Data").ValueKind);

            // 22. Action StopService: GET /api/sys_user/stopservice -> 200 OK, Success = true, Data = null (khớp [HttpGet] nguồn)
            var res22 = await http.GetAsync("/api/sys_user/stopservice");
            Assert.True(res22.IsSuccessStatusCode);
            var doc22 = JsonDocument.Parse(await res22.Content.ReadAsStringAsync());
            Assert.True(doc22.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc22.RootElement.GetProperty("Data").ValueKind);

            // 23. Action Update: POST /api/sys_user/update với body rỗng -> 200 OK, Success = true, Data = null
            var req23 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res23 = await http.PostAsync("/api/sys_user/update", req23);
            Assert.True(res23.IsSuccessStatusCode);
            var doc23 = JsonDocument.Parse(await res23.Content.ReadAsStringAsync());
            Assert.True(doc23.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc23.RootElement.GetProperty("Data").ValueKind);

            // 24. Action Update: POST /api/sys_user/update guard WAUserCode không tồn tại -> 400 BadRequest (Sys_User_BizInvalidUserAbility)
            var req24 = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01", WAUserCode = "INVALID_USER" }), Encoding.UTF8, "application/json");
            var res24 = await http.PostAsync("/api/sys_user/update", req24);
            Assert.False(res24.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res24.StatusCode);
            var doc24 = JsonDocument.Parse(await res24.Content.ReadAsStringAsync());
            Assert.False(doc24.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_User_BizInvalidUserAbility", doc24.RootElement.GetProperty("ErrorCode").GetString());

            // 25. Action Update: POST /api/sys_user/update với UserCode không tồn tại -> 200 OK, Data = null
            var req25 = new StringContent(JsonSerializer.Serialize(new { UserCode = "NONEXISTENT" }), Encoding.UTF8, "application/json");
            var res25 = await http.PostAsync("/api/sys_user/update", req25);
            Assert.True(res25.IsSuccessStatusCode);
            var doc25 = JsonDocument.Parse(await res25.Content.ReadAsStringAsync());
            Assert.True(doc25.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc25.RootElement.GetProperty("Data").ValueKind);

            // 26. Action Update: POST /api/sys_user/update với UserCode hợp lệ U01 -> 200 OK, lấy đầy đủ thông tin và danh sách
            var req26 = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01" }), Encoding.UTF8, "application/json");
            var res26 = await http.PostAsync("/api/sys_user/update", req26);
            Assert.True(res26.IsSuccessStatusCode);
            var doc26 = JsonDocument.Parse(await res26.Content.ReadAsStringAsync());
            Assert.True(doc26.RootElement.GetProperty("Success").GetBoolean());
            var updateData26 = doc26.RootElement.GetProperty("Data");
            Assert.Equal("U01", updateData26.GetProperty("UserCode").GetString());
            Assert.Equal("admin@solution.hn", updateData26.GetProperty("UserName").GetString());
            Assert.Equal("*********", updateData26.GetProperty("UserPassword").GetString());
            Assert.Equal("DEPT_BGD", updateData26.GetProperty("mdept_DepartmentCode").GetString());
            Assert.Equal("Ban Giám Đốc", updateData26.GetProperty("mdept_DepartmentName").GetString());
            Assert.Equal(2, updateData26.GetProperty("ListMst_Department").GetArrayLength());
            Assert.Equal(1, updateData26.GetProperty("ListMst_Customer").GetArrayLength());
            Assert.Equal("CUS001", updateData26.GetProperty("ListMst_Customer")[0].GetProperty("CustomerCodeSys").GetString());

            // 27. Action Update: so sánh bộ lọc SysAdmin -> khác số dòng NNT
            var req27a = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01", IsSysAdmin = "0" }), Encoding.UTF8, "application/json");
            var res27a = await http.PostAsync("/api/sys_user/update", req27a);
            Assert.True(res27a.IsSuccessStatusCode);
            var doc27a = JsonDocument.Parse(await res27a.Content.ReadAsStringAsync());
            Assert.Equal(1, doc27a.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());

            var req27b = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01", IsSysAdmin = "1" }), Encoding.UTF8, "application/json");
            var res27b = await http.PostAsync("/api/sys_user/update", req27b);
            Assert.True(res27b.IsSuccessStatusCode);
            var doc27b = JsonDocument.Parse(await res27b.Content.ReadAsStringAsync());
            Assert.Equal(2, doc27b.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());

            // 28. Action Update: GET /api/sys_user/update?usercode=U01 -> 200 OK (khớp [HttpGet] nguồn)
            var res28 = await http.GetAsync("/api/sys_user/update?usercode=U01");
            Assert.True(res28.IsSuccessStatusCode);
            var doc28 = JsonDocument.Parse(await res28.Content.ReadAsStringAsync());
            Assert.True(doc28.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("U01", doc28.RootElement.GetProperty("Data").GetProperty("UserCode").GetString());

            // 29. Action Edit: POST /api/sys_user/edit với body rỗng -> 200 OK, Messages = ["Mã người dùng trống!"]
            var req29 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res29 = await http.PostAsync("/api/sys_user/edit", req29);
            Assert.True(res29.IsSuccessStatusCode);
            var doc29 = JsonDocument.Parse(await res29.Content.ReadAsStringAsync());
            Assert.True(doc29.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã người dùng trống!", doc29.RootElement.GetProperty("Messages")[0].GetString());

            // 30. Action Edit: POST /api/sys_user/edit với UserCode không tồn tại -> 200 OK, Messages = ["Mã người dùng 'NONEXISTENT' không có trong hệ thống!"]
            var req30 = new StringContent(JsonSerializer.Serialize(new { UserCode = "NONEXISTENT" }), Encoding.UTF8, "application/json");
            var res30 = await http.PostAsync("/api/sys_user/edit", req30);
            Assert.True(res30.IsSuccessStatusCode);
            var doc30 = JsonDocument.Parse(await res30.Content.ReadAsStringAsync());
            Assert.True(doc30.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã người dùng 'NONEXISTENT' không có trong hệ thống!", doc30.RootElement.GetProperty("Messages")[0].GetString());

            // 31. Action Edit: POST /api/sys_user/edit biz guard: UserName rỗng khi cập nhật UserName -> 400 BadRequest (Sys_User_Update_InvalidUserName)
            var req31 = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01", UserName = "", Ft_Cols_Upd = "SYS_USER.USERNAME," }), Encoding.UTF8, "application/json");
            var res31 = await http.PostAsync("/api/sys_user/edit", req31);
            Assert.False(res31.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res31.StatusCode);
            var doc31 = JsonDocument.Parse(await res31.Content.ReadAsStringAsync());
            Assert.False(doc31.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_User_Update_InvalidUserName", doc31.RootElement.GetProperty("ErrorCode").GetString());

            // 32. Action Edit: POST /api/sys_user/edit sửa thành công round-trip -> 200 OK, Messages = ["Sửa người dùng thành công!"]
            var req32 = new StringContent(JsonSerializer.Serialize(new
            {
                UserCode = "u01",
                UserName = "Admin Solution Updated",
                PhoneNo = "0999888777",
                Position = "Giam Doc Dieu Hanh",
                DepartmentCode = "DEPT_BGD"
            }), Encoding.UTF8, "application/json");
            var res32 = await http.PostAsync("/api/sys_user/edit", req32);
            Assert.True(res32.IsSuccessStatusCode);
            var doc32 = JsonDocument.Parse(await res32.Content.ReadAsStringAsync());
            Assert.True(doc32.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Sửa người dùng thành công!", doc32.RootElement.GetProperty("Messages")[0].GetString());
            Assert.Equal("/Sys_User", doc32.RootElement.GetProperty("RedirectUrl").GetString());
            var data32 = doc32.RootElement.GetProperty("Data");
            Assert.Equal("U01", data32.GetProperty("UserCode").GetString());
            Assert.Equal("Admin Solution Updated", data32.GetProperty("UserName").GetString());
            Assert.Equal("0999888777", data32.GetProperty("PhoneNo").GetString());
            Assert.Equal("*********", data32.GetProperty("UserPassword").GetString());

            // 33. Action Edit alias: POST /api/sys_user/save sửa thành công round-trip -> 200 OK
            var req33 = new StringContent(JsonSerializer.Serialize(new
            {
                UserCode = "u02",
                UserName = "Tester Solution Updated Via Save",
                PhoneNo = "0888999000"
            }), Encoding.UTF8, "application/json");
            var res33 = await http.PostAsync("/api/sys_user/save", req33);
            Assert.True(res33.IsSuccessStatusCode);
            var doc33 = JsonDocument.Parse(await res33.Content.ReadAsStringAsync());
            Assert.True(doc33.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Sửa người dùng thành công!", doc33.RootElement.GetProperty("Messages")[0].GetString());

            // 34. Action Edit qua chuỗi JSON Model -> 200 OK
            var modelJson34 = JsonSerializer.Serialize(new
            {
                UserCode = "u03",
                UserName = "Tester 03 Updated Via Model String",
                Position = "Lead QA"
            });
            var req34 = new StringContent(JsonSerializer.Serialize(new { Model = modelJson34 }), Encoding.UTF8, "application/json");
            var res34 = await http.PostAsync("/api/sys_user/edit", req34);
            Assert.True(res34.IsSuccessStatusCode);
            var doc34 = JsonDocument.Parse(await res34.Content.ReadAsStringAsync());
            Assert.True(doc34.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Sửa người dùng thành công!", doc34.RootElement.GetProperty("Messages")[0].GetString());
            Assert.Equal("Tester 03 Updated Via Model String", doc34.RootElement.GetProperty("Data").GetProperty("UserName").GetString());

            // 35. Action Detail: POST /api/sys_user/detail với body rỗng -> 200 OK, Success = true, Data = null
            var req35 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res35 = await http.PostAsync("/api/sys_user/detail", req35);
            Assert.True(res35.IsSuccessStatusCode);
            var doc35 = JsonDocument.Parse(await res35.Content.ReadAsStringAsync());
            Assert.True(doc35.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc35.RootElement.GetProperty("Data").ValueKind);

            // 36. Action Detail: POST /api/sys_user/detail guard WAUserCode không tồn tại -> 400 BadRequest (Sys_User_BizInvalidUserAbility)
            var req36 = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01", WAUserCode = "INVALID_USER" }), Encoding.UTF8, "application/json");
            var res36 = await http.PostAsync("/api/sys_user/detail", req36);
            Assert.False(res36.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res36.StatusCode);
            var doc36 = JsonDocument.Parse(await res36.Content.ReadAsStringAsync());
            Assert.False(doc36.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_User_BizInvalidUserAbility", doc36.RootElement.GetProperty("ErrorCode").GetString());

            // 37. Action Detail: POST /api/sys_user/detail với UserCode không tồn tại -> 200 OK, Data = null
            var req37 = new StringContent(JsonSerializer.Serialize(new { UserCode = "NONEXISTENT" }), Encoding.UTF8, "application/json");
            var res37 = await http.PostAsync("/api/sys_user/detail", req37);
            Assert.True(res37.IsSuccessStatusCode);
            var doc37 = JsonDocument.Parse(await res37.Content.ReadAsStringAsync());
            Assert.True(doc37.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc37.RootElement.GetProperty("Data").ValueKind);

            // 38. Action Detail: POST /api/sys_user/detail với UserCode hợp lệ U01 -> 200 OK, lấy đầy đủ thông tin và danh sách
            var req38 = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01" }), Encoding.UTF8, "application/json");
            var res38 = await http.PostAsync("/api/sys_user/detail", req38);
            Assert.True(res38.IsSuccessStatusCode);
            var doc38 = JsonDocument.Parse(await res38.Content.ReadAsStringAsync());
            Assert.True(doc38.RootElement.GetProperty("Success").GetBoolean());
            var detailData38 = doc38.RootElement.GetProperty("Data");
            Assert.Equal("U01", detailData38.GetProperty("UserCode").GetString());
            Assert.Equal("Admin Solution Updated", detailData38.GetProperty("UserName").GetString());
            Assert.Equal("*********", detailData38.GetProperty("UserPassword").GetString());
            Assert.Equal("DEPT_BGD", detailData38.GetProperty("mdept_DepartmentCode").GetString());
            Assert.Equal("Ban Giám Đốc", detailData38.GetProperty("mdept_DepartmentName").GetString());
            Assert.Equal(2, detailData38.GetProperty("ListMst_Department").GetArrayLength());
            Assert.Equal(1, detailData38.GetProperty("ListMst_Customer").GetArrayLength());
            Assert.Equal("CUS001", detailData38.GetProperty("ListMst_Customer")[0].GetProperty("CustomerCodeSys").GetString());

            // 39. Action Detail: so sánh bộ lọc SysAdmin -> khác số dòng NNT (1 vs 2 NNT)
            var req39a = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01", IsSysAdmin = "0" }), Encoding.UTF8, "application/json");
            var res39a = await http.PostAsync("/api/sys_user/detail", req39a);
            Assert.True(res39a.IsSuccessStatusCode);
            var doc39a = JsonDocument.Parse(await res39a.Content.ReadAsStringAsync());
            Assert.Equal(1, doc39a.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());

            var req39b = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01", IsSysAdmin = "1" }), Encoding.UTF8, "application/json");
            var res39b = await http.PostAsync("/api/sys_user/detail", req39b);
            Assert.True(res39b.IsSuccessStatusCode);
            var doc39b = JsonDocument.Parse(await res39b.Content.ReadAsStringAsync());
            Assert.Equal(2, doc39b.RootElement.GetProperty("Data").GetProperty("ListMst_NNTCur").GetArrayLength());

            // 40. Action Detail: GET /api/sys_user/detail?usercode=U01 -> 200 OK (khớp [HttpGet] nguồn)
            var res40 = await http.GetAsync("/api/sys_user/detail?usercode=U01");
            Assert.True(res40.IsSuccessStatusCode);
            var doc40 = JsonDocument.Parse(await res40.Content.ReadAsStringAsync());
            Assert.True(doc40.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("U01", doc40.RootElement.GetProperty("Data").GetProperty("UserCode").GetString());

            // 41. Action Detail: qua chuỗi JSON Model -> 200 OK
            var modelJson41 = JsonSerializer.Serialize(new
            {
                UserCode = "u02"
            });
            var req41 = new StringContent(JsonSerializer.Serialize(new { Model = modelJson41 }), Encoding.UTF8, "application/json");
            var res41 = await http.PostAsync("/api/sys_user/detail", req41);
            Assert.True(res41.IsSuccessStatusCode);
            var doc41 = JsonDocument.Parse(await res41.Content.ReadAsStringAsync());
            Assert.True(doc41.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("U02", doc41.RootElement.GetProperty("Data").GetProperty("UserCode").GetString());

            // 42. Action Delete: POST /api/sys_user/delete với body rỗng -> 200 OK, Messages = ["Mã người dùng trống!"]
            var req42 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res42 = await http.PostAsync("/api/sys_user/delete", req42);
            Assert.True(res42.IsSuccessStatusCode);
            var doc42 = JsonDocument.Parse(await res42.Content.ReadAsStringAsync());
            Assert.True(doc42.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã người dùng trống!", doc42.RootElement.GetProperty("Messages")[0].GetString());

            // 43. Action Delete: POST /api/sys_user/delete với UserCode không tồn tại -> 200 OK, Messages = ["Mã người dùng 'NON_EXISTENT' không có trong hệ thống!"]
            var req43 = new StringContent(JsonSerializer.Serialize(new { UserCode = "NON_EXISTENT" }), Encoding.UTF8, "application/json");
            var res43 = await http.PostAsync("/api/sys_user/delete", req43);
            Assert.True(res43.IsSuccessStatusCode);
            var doc43 = JsonDocument.Parse(await res43.Content.ReadAsStringAsync());
            Assert.True(doc43.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã người dùng 'NON_EXISTENT' không có trong hệ thống!", doc43.RootElement.GetProperty("Messages")[0].GetString());

            // 44. Action Delete: POST /api/sys_user/delete xoá người dùng U_SERVER_CREATED vừa tạo ở bước 13 -> 200 OK, Messages = ["Xóa người dùng thành công!"]
            var req44 = new StringContent(JsonSerializer.Serialize(new { UserCode = "u_server_created" }), Encoding.UTF8, "application/json");
            var res44 = await http.PostAsync("/api/sys_user/delete", req44);
            Assert.True(res44.IsSuccessStatusCode);
            var doc44 = JsonDocument.Parse(await res44.Content.ReadAsStringAsync());
            Assert.True(doc44.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa người dùng thành công!", doc44.RootElement.GetProperty("Messages")[0].GetString());
            Assert.Equal("/Sys_User", doc44.RootElement.GetProperty("RedirectUrl").GetString());

            // 45. Kiểm tra người dùng U_SERVER_CREATED không còn tồn tại sau khi xoá
            var req45 = new StringContent(JsonSerializer.Serialize(new { UserCode = "U_SERVER_CREATED" }), Encoding.UTF8, "application/json");
            var res45 = await http.PostAsync("/api/sys_user/detail", req45);
            Assert.True(res45.IsSuccessStatusCode);
            var doc45 = JsonDocument.Parse(await res45.Content.ReadAsStringAsync());
            Assert.True(doc45.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc45.RootElement.GetProperty("Data").ValueKind);

            // 46. Action Delete: xoá qua chuỗi JSON Model
            var modelJson46 = JsonSerializer.Serialize(new { UserCode = "NON_EXISTENT_JSON" });
            var req46 = new StringContent(JsonSerializer.Serialize(new { Model = modelJson46 }), Encoding.UTF8, "application/json");
            var res46 = await http.PostAsync("/api/sys_user/delete", req46);
            Assert.True(res46.IsSuccessStatusCode);
            var doc46 = JsonDocument.Parse(await res46.Content.ReadAsStringAsync());
            Assert.True(doc46.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã người dùng 'NON_EXISTENT_JSON' không có trong hệ thống!", doc46.RootElement.GetProperty("Messages")[0].GetString());

            // 47. Action ShowPopupChangePassword: POST /api/sys_user/showpopupchangepassword với body rỗng -> 200 OK, Success = true, UserCode = null
            var req47 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res47 = await http.PostAsync("/api/sys_user/showpopupchangepassword", req47);
            Assert.True(res47.IsSuccessStatusCode);
            var doc47 = JsonDocument.Parse(await res47.Content.ReadAsStringAsync());
            Assert.True(doc47.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc47.RootElement.GetProperty("Data").GetProperty("UserCode").ValueKind);
            Assert.Equal("<h2>ShowPopupChangePassword</h2>", doc47.RootElement.GetProperty("Html").GetString());

            // 48. Action ShowPopupChangePassword: POST /api/sys_user/showpopupchangepassword với UserCode = u01 -> 200 OK, UserCode = U01
            var req48 = new StringContent(JsonSerializer.Serialize(new { UserCode = "u01" }), Encoding.UTF8, "application/json");
            var res48 = await http.PostAsync("/api/sys_user/showpopupchangepassword", req48);
            Assert.True(res48.IsSuccessStatusCode);
            var doc48 = JsonDocument.Parse(await res48.Content.ReadAsStringAsync());
            Assert.True(doc48.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("U01", doc48.RootElement.GetProperty("UserCode").GetString());
            Assert.Equal("U01", doc48.RootElement.GetProperty("Data").GetProperty("UserCode").GetString());

            // 49. Action ShowPopupChangePassword: so sánh ca UserCode = u02 -> kết quả khác biệt U02
            var req49 = new StringContent(JsonSerializer.Serialize(new { UserCode = "u02" }), Encoding.UTF8, "application/json");
            var res49 = await http.PostAsync("/api/sys_user/showpopupchangepassword", req49);
            Assert.True(res49.IsSuccessStatusCode);
            var doc49 = JsonDocument.Parse(await res49.Content.ReadAsStringAsync());
            Assert.True(doc49.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("U02", doc49.RootElement.GetProperty("Data").GetProperty("UserCode").GetString());

            // 50. Action ShowPopupChangePassword: GET /api/sys_user/showpopupchangepassword?usercode=u03 -> 200 OK, UserCode = U03
            var res50 = await http.GetAsync("/api/sys_user/showpopupchangepassword?usercode=u03");
            Assert.True(res50.IsSuccessStatusCode);
            var doc50 = JsonDocument.Parse(await res50.Content.ReadAsStringAsync());
            Assert.True(doc50.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("U03", doc50.RootElement.GetProperty("Data").GetProperty("UserCode").GetString());

            // 51. Action ShowPopupChangePassword: POST với Model JSON string -> 200 OK, UserCode = U01
            var modelJson51 = JsonSerializer.Serialize(new { UserCode = "u01" });
            var req51 = new StringContent(JsonSerializer.Serialize(new { Model = modelJson51 }), Encoding.UTF8, "application/json");
            var res51 = await http.PostAsync("/api/sys_user/showpopupchangepassword", req51);
            Assert.True(res51.IsSuccessStatusCode);
            var doc51 = JsonDocument.Parse(await res51.Content.ReadAsStringAsync());
            Assert.True(doc51.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("U01", doc51.RootElement.GetProperty("Data").GetProperty("UserCode").GetString());
        }
        finally
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(true);
                    proc.WaitForExit(3000);
                }
            }
            catch { }

            try
            {
                if (File.Exists(tempDb))
                {
                    File.Delete(tempDb);
                }
            }
            catch { }
        }
    }
}
