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
