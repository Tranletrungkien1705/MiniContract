using System.Diagnostics;
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

public class Mst_Sys_ConfigControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_Sys_ConfigControllerPort _controller;

    public Mst_Sys_ConfigControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_Sys_Config
        var sysConfigSeeder = new Mst_Sys_ConfigSeeder();
        sysConfigSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Chạy seeder Mst_Attribute_Contract
        var attrSeeder = new Mst_Attribute_ContractSeeder();
        attrSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_Sys_ConfigControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_NullReq_SuccessWithAllRecords()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(3, listConfig.GetArrayLength());

        var listAttr = data.GetProperty("ListMst_Attribute_Contract");
        Assert.Equal(3, listAttr.GetArrayLength());
    }

    [Fact]
    public async Task Index_EmptyReq_SuccessWithAllRecords()
    {
        var req = new Mst_Sys_Config_IndexReq();
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(3, listConfig.GetArrayLength());

        var firstConfig = data.GetProperty("Mst_Sys_Config");
        Assert.Equal(JsonValueKind.Object, firstConfig.ValueKind);
        Assert.False(string.IsNullOrEmpty(firstConfig.GetProperty("OrgID").GetString()));
    }

    [Fact]
    public async Task Index_FilterByOrgID_ReturnsSingleMatchedConfig()
    {
        var req = new Mst_Sys_Config_IndexReq { OrgID = "7200000001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(1, listConfig.GetArrayLength());
        Assert.Equal("7200000001", listConfig[0].GetProperty("OrgID").GetString());
        Assert.Equal("1001", listConfig[0].GetProperty("NetworkID").GetString());
        Assert.Equal("1", listConfig[0].GetProperty("FlagOTP").GetString());

        var configObj = data.GetProperty("Mst_Sys_Config");
        Assert.Equal("7200000001", configObj.GetProperty("OrgID").GetString());

        var listAttr = data.GetProperty("ListMst_Attribute_Contract");
        Assert.Equal(3, listAttr.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByNonExistentOrgID_ReturnsZeroConfigs()
    {
        var req = new Mst_Sys_Config_IndexReq { OrgID = "9999999999" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(0, listConfig.GetArrayLength());

        var configObj = data.GetProperty("Mst_Sys_Config");
        Assert.Equal(JsonValueKind.Null, configObj.ValueKind);

        // Thuộc tính hợp đồng vẫn được trả về đầy đủ
        var listAttr = data.GetProperty("ListMst_Attribute_Contract");
        Assert.Equal(3, listAttr.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByNetworkID_ReturnsFilteredConfigsAndAttributes()
    {
        var req = new Mst_Sys_Config_IndexReq { NetworkID = "1001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(2, listConfig.GetArrayLength());
        foreach (var c in listConfig.EnumerateArray())
        {
            Assert.Equal("1001", c.GetProperty("NetworkID").GetString());
        }

        var listAttr = data.GetProperty("ListMst_Attribute_Contract");
        Assert.Equal(3, listAttr.GetArrayLength());
        foreach (var a in listAttr.EnumerateArray())
        {
            Assert.Equal("1001", a.GetProperty("NetworkID").GetString());
        }
    }

    [Fact]
    public async Task Index_Attributes_OrderedByCodeAscending()
    {
        var req = new Mst_Sys_Config_IndexReq();
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");
        var listAttr = data.GetProperty("ListMst_Attribute_Contract");

        Assert.Equal(3, listAttr.GetArrayLength());
        Assert.Equal("ATTR01", listAttr[0].GetProperty("AttributeContractCode").GetString());
        Assert.Equal("ATTR02", listAttr[1].GetProperty("AttributeContractCode").GetString());
        Assert.Equal("ATTR03", listAttr[2].GetProperty("AttributeContractCode").GetString());
    }

    [Fact]
    public async Task Update_NullReq_ReturnsEmptyConfigListMessage()
    {
        var result = await _controller.Update(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var messages = root.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Danh sách cấu hình trống!", messages[0].GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_EmptyReq_ReturnsEmptyConfigListMessage()
    {
        var req = new Mst_Sys_Config_UpdateReq();
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var messages = root.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Danh sách cấu hình trống!", messages[0].GetString());
    }

    [Fact]
    public async Task Update_NonExistentOrgID_ReturnsOrganNotFoundFail()
    {
        var req = new Mst_Sys_Config_UpdateReq { OrgID = "9999999999", FlagCurrency = "1" };
        var result = await _controller.Update(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Sys_Config_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal("9999999999", p.GetProperty("Check.OrgID").GetString());
    }

    [Fact]
    public async Task Update_ValidReq_RoundTrip_Success()
    {
        // Ban đầu trên OrgID 7200000001: FlagCurrency="0", FlagColmnConfig="0", FlagOTP="1"
        var req = new Mst_Sys_Config_UpdateReq
        {
            OrgID = "7200000001",
            FlagCurrency = "1",
            FlagColmnConfig = "1",
            FlagOTP = "0"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var messages = root.GetProperty("Messages");
        Assert.Equal("Sửa cấu hình thành công!", messages[0].GetString());

        // Kiểm tra round-trip đọc lại từ DB
        var updated = await _db.Set<Mst_Sys_Config>().FirstOrDefaultAsync(x => x.OrgID == "7200000001");
        Assert.NotNull(updated);
        Assert.Equal("1", updated.FlagCurrency);
        Assert.Equal("1", updated.FlagColmnConfig);
        Assert.Equal("0", updated.FlagOTP);
        Assert.False(string.IsNullOrEmpty(updated.LogLUDTimeUTC));
        Assert.Equal("port", updated.LogLUBy);
    }

    [Fact]
    public async Task Update_JsonModelString_Success()
    {
        var req = new Mst_Sys_Config_UpdateReq
        {
            Model = "{\"Mst_Sys_Config\":{\"OrgID\":\"7200000001\",\"FlagCurrency\":\"0\",\"FlagColmnConfig\":\"0\",\"FlagOTP\":\"1\"}}"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        var updated = await _db.Set<Mst_Sys_Config>().FirstOrDefaultAsync(x => x.OrgID == "7200000001");
        Assert.NotNull(updated);
        Assert.Equal("0", updated.FlagCurrency);
        Assert.Equal("0", updated.FlagColmnConfig);
        Assert.Equal("1", updated.FlagOTP);
    }

    [Fact]
    public async Task Update_WithFtColsUpd_OnlyUpdatesSpecifiedColumns()
    {
        // OrgID 7200000002 ban đầu có: FlagCurrency="1", FlagColmnConfig="1", FlagOTP="0"
        var req = new Mst_Sys_Config_UpdateReq
        {
            OrgID = "7200000002",
            FlagOTP = "1",
            FlagCurrency = "0", // Không cập nhật vì Ft_Cols_Upd chỉ định FlagOTP
            Ft_Cols_Upd = "Mst_Sys_Config.FlagOTP"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var updated = await _db.Set<Mst_Sys_Config>().FirstOrDefaultAsync(x => x.OrgID == "7200000002");
        Assert.NotNull(updated);
        Assert.Equal("1", updated.FlagOTP); // Cột được chỉ định đã đổi từ 0 thành 1
        Assert.Equal("1", updated.FlagCurrency); // Cột không chỉ định giữ nguyên giá trị ban đầu là 1
        Assert.Equal("1", updated.FlagColmnConfig); // Giữ nguyên
    }

    [Fact]
    public async Task DeleteAttribute_NullReq_ReturnsOrganNotFoundFail()
    {
        var result = await _controller.DeleteAttribute(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal(JsonValueKind.Null, p.GetProperty("Check.AttributeContractCode").ValueKind);
    }

    [Fact]
    public async Task DeleteAttribute_EmptyReq_ReturnsOrganNotFoundFail()
    {
        var req = new Mst_Sys_Config_DeleteAttributeReq();
        var result = await _controller.DeleteAttribute(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal(JsonValueKind.Null, p.GetProperty("Check.AttributeContractCode").ValueKind);
    }

    [Fact]
    public async Task DeleteAttribute_NonExistentCode_ReturnsOrganNotFoundFail()
    {
        var req = new Mst_Sys_Config_DeleteAttributeReq { AttributeContractCode = "ATTR_NON_EXISTENT" };
        var result = await _controller.DeleteAttribute(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal("ATTR_NON_EXISTENT", p.GetProperty("Check.AttributeContractCode").GetString());
    }

    [Fact]
    public async Task DeleteAttribute_ValidCode_RoundTrip_Success()
    {
        // Kiểm tra trước khi xoá: ATTR03 tồn tại trong DB
        var before = await _db.Set<Mst_Attribute_Contract>().FirstOrDefaultAsync(x => x.AttributeContractCode == "ATTR03");
        Assert.NotNull(before);

        var req = new Mst_Sys_Config_DeleteAttributeReq { AttributeContractCode = "ATTR03" };
        var result = await _controller.DeleteAttribute(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var messages = root.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Xoá thông tin thành công!", messages[0].GetString());

        // Kiểm tra sau khi xoá: ATTR03 không còn trong DB (round-trip)
        var after = await _db.Set<Mst_Attribute_Contract>().FirstOrDefaultAsync(x => x.AttributeContractCode == "ATTR03");
        Assert.Null(after);
    }

    [Fact]
    public async Task DeleteAttribute_JsonModelString_Success()
    {
        // Kiểm tra trước khi xoá: ATTR02 tồn tại trong DB
        var before = await _db.Set<Mst_Attribute_Contract>().FirstOrDefaultAsync(x => x.AttributeContractCode == "ATTR02");
        Assert.NotNull(before);

        var req = new Mst_Sys_Config_DeleteAttributeReq
        {
            Model = "{\"AttributeContractCode\":\"ATTR02\"}"
        };
        var result = await _controller.DeleteAttribute(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        // Kiểm tra sau khi xoá: ATTR02 không còn trong DB
        var after = await _db.Set<Mst_Attribute_Contract>().FirstOrDefaultAsync(x => x.AttributeContractCode == "ATTR02");
        Assert.Null(after);
    }

    [Fact]
    public async Task UpdateAttribute_NullReq_ReturnsOrganNotFoundFail()
    {
        var result = await _controller.UpdateAttribute(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal(JsonValueKind.Null, p.GetProperty("Check.AttributeContractCode").ValueKind);
    }

    [Fact]
    public async Task UpdateAttribute_EmptyReq_ReturnsOrganNotFoundFail()
    {
        var req = new Mst_Sys_Config_UpdateAttributeReq();
        var result = await _controller.UpdateAttribute(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal(JsonValueKind.Null, p.GetProperty("Check.AttributeContractCode").ValueKind);
    }

    [Fact]
    public async Task UpdateAttribute_NonExistentCode_ReturnsOrganNotFoundFail()
    {
        var req = new Mst_Sys_Config_UpdateAttributeReq { AttributeContractCode = "ATTR_NON_EXISTENT" };
        var result = await _controller.UpdateAttribute(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal("ATTR_NON_EXISTENT", p.GetProperty("Check.AttributeContractCode").GetString());
    }

    [Fact]
    public async Task UpdateAttribute_ValidReq_RoundTrip_Success()
    {
        // ATTR01 ban đầu: AttributeName="Số hợp đồng gốc", DefaultValues="", FlagActive="1"
        var req = new Mst_Sys_Config_UpdateAttributeReq
        {
            AttributeContractCode = "ATTR01",
            AttributeName = "Số hợp đồng gốc đã sửa",
            DefaultValues = "HD-01,HD-02",
            FlagActive = "0"
        };
        var result = await _controller.UpdateAttribute(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var messages = root.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Cập nhật thông tin hợp đồng thành công!", messages[0].GetString());
        Assert.Equal("/Mst_Sys_Config", root.GetProperty("RedirectUrl").GetString());

        // Kiểm tra round-trip đọc lại từ DB
        var updated = await _db.Set<Mst_Attribute_Contract>().FirstOrDefaultAsync(x => x.AttributeContractCode == "ATTR01");
        Assert.NotNull(updated);
        Assert.Equal("Số hợp đồng gốc đã sửa", updated.AttributeName);
        Assert.Equal("HD-01,HD-02", updated.DefaultValues);
        Assert.Equal("0", updated.FlagActive);
        Assert.False(string.IsNullOrEmpty(updated.LogLUDTimeUTC));
        Assert.Equal("port", updated.LogLUBy);
    }

    [Fact]
    public async Task UpdateAttribute_JsonModelString_Success()
    {
        var req = new Mst_Sys_Config_UpdateAttributeReq
        {
            Model = "{\"AttributeContractCode\":\"ATTR01\",\"AttributeName\":\"Số hợp đồng gốc khôi phục\",\"DefaultValues\":\"\",\"FlagActive\":\"1\"}"
        };
        var result = await _controller.UpdateAttribute(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        var updated = await _db.Set<Mst_Attribute_Contract>().FirstOrDefaultAsync(x => x.AttributeContractCode == "ATTR01");
        Assert.NotNull(updated);
        Assert.Equal("Số hợp đồng gốc khôi phục", updated.AttributeName);
        Assert.Equal("", updated.DefaultValues);
        Assert.Equal("1", updated.FlagActive);
    }

    [Fact]
    public async Task UpdateAttribute_WithFtColsUpd_OnlyUpdatesSpecifiedColumns()
    {
        // ATTR01: AttributeName="Số hợp đồng gốc", DefaultValues="", FlagActive="1"
        var req = new Mst_Sys_Config_UpdateAttributeReq
        {
            AttributeContractCode = "ATTR01",
            AttributeName = "Tên cập nhật qua FtCols",
            DefaultValues = "GIÁ TRỊ KHÔNG ĐƯỢC CẬP NHẬT",
            Ft_Cols_Upd = "Mst_Attribute_Contract.AttributeName"
        };
        var result = await _controller.UpdateAttribute(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var updated = await _db.Set<Mst_Attribute_Contract>().FirstOrDefaultAsync(x => x.AttributeContractCode == "ATTR01");
        Assert.NotNull(updated);
        Assert.Equal("Tên cập nhật qua FtCols", updated.AttributeName);
        Assert.Equal("", updated.DefaultValues); // Giữ nguyên không bị cập nhật
    }

    [Fact]
    public async Task CreateAttribute_NullReq_ReturnsInvalidAttributeContractCodeFail()
    {
        var result = await _controller.CreateAttribute(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_Create_InvalidAttributeContractCode", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal(JsonValueKind.Null, p.GetProperty("Check.strAttributeContractCode").ValueKind);
    }

    [Fact]
    public async Task CreateAttribute_EmptyReq_ReturnsInvalidAttributeContractCodeFail()
    {
        var req = new Mst_Sys_Config_CreateAttributeReq();
        var result = await _controller.CreateAttribute(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_Create_InvalidAttributeContractCode", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal(JsonValueKind.Null, p.GetProperty("Check.strAttributeContractCode").ValueKind);
    }

    [Fact]
    public async Task CreateAttribute_ExistingCode_ReturnsOrganExistFail()
    {
        var req = new Mst_Sys_Config_CreateAttributeReq
        {
            AttributeContractCode = "ATTR01",
            AttributeName = "Thuoc tinh trung"
        };
        var result = await _controller.CreateAttribute(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganExist", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal("ATTR01", p.GetProperty("Check.AttributeContractCode").GetString());
    }

    [Fact]
    public async Task CreateAttribute_ValidReq_RoundTrip_Success()
    {
        var req = new Mst_Sys_Config_CreateAttributeReq
        {
            AttributeContractCode = "ATTR04",
            AttributeName = "Thời hạn hợp đồng",
            DefaultValues = "12 tháng"
        };
        var result = await _controller.CreateAttribute(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var messages = root.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Lưu thông tin hợp đồng thành công!", messages[0].GetString());
        Assert.Equal("/Mst_Sys_Config", root.GetProperty("RedirectUrl").GetString());

        // Kiểm tra round-trip đọc lại từ DB
        var created = await _db.Set<Mst_Attribute_Contract>().FirstOrDefaultAsync(x => x.AttributeContractCode == "ATTR04");
        Assert.NotNull(created);
        Assert.Equal("Thời hạn hợp đồng", created.AttributeName);
        Assert.Equal("12 tháng", created.DefaultValues);
        Assert.Equal("1", created.FlagActive);
        Assert.Equal("1001", created.NetworkID);
        Assert.Equal("port", created.LogLUBy);
        Assert.False(string.IsNullOrEmpty(created.LogLUDTimeUTC));
    }

    [Fact]
    public async Task CreateAttribute_JsonModelString_Success()
    {
        var req = new Mst_Sys_Config_CreateAttributeReq
        {
            Model = "{\"AttributeContractCode\":\"ATTR05\",\"AttributeName\":\"Mã dự án\",\"DefaultValues\":\"PRJ-01\"}"
        };
        var result = await _controller.CreateAttribute(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        var created = await _db.Set<Mst_Attribute_Contract>().FirstOrDefaultAsync(x => x.AttributeContractCode == "ATTR05");
        Assert.NotNull(created);
        Assert.Equal("Mã dự án", created.AttributeName);
        Assert.Equal("PRJ-01", created.DefaultValues);
        Assert.Equal("1", created.FlagActive);
    }

    [Fact]
    public async Task CreateAttribute_DuplicateCodeAfterCreate_ReturnsOrganExistFail()
    {
        var req1 = new Mst_Sys_Config_CreateAttributeReq
        {
            AttributeContractCode = "ATTR06",
            AttributeName = "Lần đầu tạo"
        };
        var result1 = await _controller.CreateAttribute(req1);
        Assert.IsType<OkObjectResult>(result1);

        var req2 = new Mst_Sys_Config_CreateAttributeReq
        {
            AttributeContractCode = "ATTR06",
            AttributeName = "Tạo lại cùng mã"
        };
        var result2 = await _controller.CreateAttribute(req2);
        var badResult = Assert.IsType<BadRequestObjectResult>(result2);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganExist", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task RealServer_Verification_Port8544()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_sys_config_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
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

            // 1. Ca body rỗng: POST {} -> 200 OK, ItemCount=3, Lst_Mst_Sys_Config=3, ListMst_Attribute_Contract=3
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_sys_config/index", req1);
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(3, data1.GetProperty("ItemCount").GetInt32());
            Assert.Equal(3, data1.GetProperty("Lst_Mst_Sys_Config").GetArrayLength());
            Assert.Equal(3, data1.GetProperty("ListMst_Attribute_Contract").GetArrayLength());

            // 2. Ca lọc OrgID="7200000001" -> 200 OK, ItemCount=1, Lst_Mst_Sys_Config=1, OrgID khớp
            var req2 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_sys_config/index", req2);
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            var data2 = doc2.RootElement.GetProperty("Data");
            Assert.Equal(1, data2.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data2.GetProperty("Lst_Mst_Sys_Config").GetArrayLength());
            Assert.Equal("7200000001", data2.GetProperty("Lst_Mst_Sys_Config")[0].GetProperty("OrgID").GetString());

            // 3. Ca lọc OrgID="9999999999" (không tồn tại) -> 200 OK, ItemCount=0, Lst_Mst_Sys_Config=0 (khác số dòng với ca 2)
            var req3 = new StringContent("{\"OrgID\":\"9999999999\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_sys_config/index", req3);
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(0, data3.GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, data3.GetProperty("Lst_Mst_Sys_Config").GetArrayLength());
            Assert.Equal(3, data3.GetProperty("ListMst_Attribute_Contract").GetArrayLength());

            // 4. Ca lọc NetworkID="1001" -> 200 OK, ItemCount=2, Lst_Mst_Sys_Config=2
            var req4 = new StringContent("{\"NetworkID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_sys_config/index", req4);
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(2, data4.GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, data4.GetProperty("Lst_Mst_Sys_Config").GetArrayLength());

            // 5. Ca Update body rỗng: POST {} -> 200 OK, Danh sách cấu hình trống! (Gate guard)
            var req5 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_sys_config/update", req5);
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var body5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Danh sách cấu hình trống!", doc5.RootElement.GetProperty("Messages")[0].GetString());

            // 6. Ca Update guard OrgID không tồn tại -> 400 Bad Request, mã lỗi Mst_Sys_Config_CheckDB_OrganNotFound (Biz guard)
            var req6 = new StringContent("{\"OrgID\":\"9999999999\",\"FlagCurrency\":\"1\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_sys_config/update", req6);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res6.StatusCode);
            var body6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(body6);
            Assert.False(doc6.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Sys_Config_CheckDB_OrganNotFound", doc6.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal("9999999999", doc6.RootElement.GetProperty("Params").GetProperty("Check.OrgID").GetString());

            // 7. Ca Update round-trip thành công trên OrgID="7200000001"
            var req7 = new StringContent("{\"OrgID\":\"7200000001\",\"FlagCurrency\":\"1\",\"FlagColmnConfig\":\"1\",\"FlagOTP\":\"0\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_sys_config/update", req7);
            Assert.Equal(System.Net.HttpStatusCode.OK, res7.StatusCode);
            var body7 = await res7.Content.ReadAsStringAsync();
            var doc7 = JsonDocument.Parse(body7);
            Assert.True(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Sửa cấu hình thành công!", doc7.RootElement.GetProperty("Messages")[0].GetString());

            // 8. Đọc lại qua Index để xác nhận giá trị round-trip thật trong DB
            var req8 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/mst_sys_config/index", req8);
            Assert.Equal(System.Net.HttpStatusCode.OK, res8.StatusCode);
            var body8 = await res8.Content.ReadAsStringAsync();
            var doc8 = JsonDocument.Parse(body8);
            var updatedConfig = doc8.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Sys_Config")[0];
            Assert.Equal("1", updatedConfig.GetProperty("FlagCurrency").GetString());
            Assert.Equal("1", updatedConfig.GetProperty("FlagColmnConfig").GetString());
            Assert.Equal("0", updatedConfig.GetProperty("FlagOTP").GetString());

            // 9. Ca DeleteAttribute body rỗng: POST {} -> 400 Bad Request, mã lỗi Mst_Attribute_Contract_CheckDB_OrganNotFound
            var req9 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res9 = await http.PostAsync("/api/mst_sys_config/deleteattribute", req9);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res9.StatusCode);
            var body9 = await res9.Content.ReadAsStringAsync();
            var doc9 = JsonDocument.Parse(body9);
            Assert.False(doc9.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", doc9.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal(JsonValueKind.Null, doc9.RootElement.GetProperty("Params").GetProperty("Check.AttributeContractCode").ValueKind);

            // 10. Ca DeleteAttribute mã không tồn tại -> 400 Bad Request, mã lỗi Mst_Attribute_Contract_CheckDB_OrganNotFound
            var req10 = new StringContent("{\"AttributeContractCode\":\"ATTR_NON_EXISTENT\"}", Encoding.UTF8, "application/json");
            var res10 = await http.PostAsync("/api/mst_sys_config/deleteattribute", req10);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res10.StatusCode);
            var body10 = await res10.Content.ReadAsStringAsync();
            var doc10 = JsonDocument.Parse(body10);
            Assert.False(doc10.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", doc10.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal("ATTR_NON_EXISTENT", doc10.RootElement.GetProperty("Params").GetProperty("Check.AttributeContractCode").GetString());

            // 11. Ca DeleteAttribute round-trip thành công xoá ATTR03 -> 200 OK
            var req11 = new StringContent("{\"AttributeContractCode\":\"ATTR03\"}", Encoding.UTF8, "application/json");
            var res11 = await http.PostAsync("/api/mst_sys_config/deleteattribute", req11);
            Assert.Equal(System.Net.HttpStatusCode.OK, res11.StatusCode);
            var body11 = await res11.Content.ReadAsStringAsync();
            var doc11 = JsonDocument.Parse(body11);
            Assert.True(doc11.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xoá thông tin thành công!", doc11.RootElement.GetProperty("Messages")[0].GetString());

            // 12. Đọc lại qua Index để xác nhận ATTR03 đã bị xoá thật trong DB (chỉ còn 2 thuộc tính)
            var req12 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res12 = await http.PostAsync("/api/mst_sys_config/index", req12);
            Assert.Equal(System.Net.HttpStatusCode.OK, res12.StatusCode);
            var body12 = await res12.Content.ReadAsStringAsync();
            var doc12 = JsonDocument.Parse(body12);
            var listAttrAfter = doc12.RootElement.GetProperty("Data").GetProperty("ListMst_Attribute_Contract");
            Assert.Equal(2, listAttrAfter.GetArrayLength());
            Assert.Equal("ATTR01", listAttrAfter[0].GetProperty("AttributeContractCode").GetString());
            Assert.Equal("ATTR02", listAttrAfter[1].GetProperty("AttributeContractCode").GetString());

            // 13. Ca UpdateAttribute body rỗng: POST {} -> 400 Bad Request, mã lỗi Mst_Attribute_Contract_CheckDB_OrganNotFound
            var req13 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res13 = await http.PostAsync("/api/mst_sys_config/updateattribute", req13);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res13.StatusCode);
            var body13 = await res13.Content.ReadAsStringAsync();
            var doc13 = JsonDocument.Parse(body13);
            Assert.False(doc13.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", doc13.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal(JsonValueKind.Null, doc13.RootElement.GetProperty("Params").GetProperty("Check.AttributeContractCode").ValueKind);

            // 14. Ca UpdateAttribute mã không tồn tại -> 400 Bad Request, mã lỗi Mst_Attribute_Contract_CheckDB_OrganNotFound
            var req14 = new StringContent("{\"AttributeContractCode\":\"ATTR_NON_EXISTENT\"}", Encoding.UTF8, "application/json");
            var res14 = await http.PostAsync("/api/mst_sys_config/updateattribute", req14);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res14.StatusCode);
            var body14 = await res14.Content.ReadAsStringAsync();
            var doc14 = JsonDocument.Parse(body14);
            Assert.False(doc14.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganNotFound", doc14.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal("ATTR_NON_EXISTENT", doc14.RootElement.GetProperty("Params").GetProperty("Check.AttributeContractCode").GetString());

            // 15. Ca UpdateAttribute round-trip thành công cập nhật ATTR01 -> 200 OK
            var req15 = new StringContent("{\"AttributeContractCode\":\"ATTR01\",\"AttributeName\":\"Số hợp đồng gốc sửa thật\",\"DefaultValues\":\"VAL_TEST\",\"FlagActive\":\"0\"}", Encoding.UTF8, "application/json");
            var res15 = await http.PostAsync("/api/mst_sys_config/updateattribute", req15);
            Assert.Equal(System.Net.HttpStatusCode.OK, res15.StatusCode);
            var body15 = await res15.Content.ReadAsStringAsync();
            var doc15 = JsonDocument.Parse(body15);
            Assert.True(doc15.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Cập nhật thông tin hợp đồng thành công!", doc15.RootElement.GetProperty("Messages")[0].GetString());
            Assert.Equal("/Mst_Sys_Config", doc15.RootElement.GetProperty("RedirectUrl").GetString());

            // 16. Đọc lại qua Index để xác nhận ATTR01 đã được cập nhật thật trong DB (round-trip)
            var req16 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res16 = await http.PostAsync("/api/mst_sys_config/index", req16);
            Assert.Equal(System.Net.HttpStatusCode.OK, res16.StatusCode);
            var body16 = await res16.Content.ReadAsStringAsync();
            var doc16 = JsonDocument.Parse(body16);
            var listAttrFinal = doc16.RootElement.GetProperty("Data").GetProperty("ListMst_Attribute_Contract");
            Assert.Equal("ATTR01", listAttrFinal[0].GetProperty("AttributeContractCode").GetString());
            Assert.Equal("Số hợp đồng gốc sửa thật", listAttrFinal[0].GetProperty("AttributeName").GetString());
            Assert.Equal("VAL_TEST", listAttrFinal[0].GetProperty("DefaultValues").GetString());
            Assert.Equal("0", listAttrFinal[0].GetProperty("FlagActive").GetString());

            // 17. Ca CreateAttribute body rỗng: POST {} -> 400 Bad Request, mã lỗi Mst_Attribute_Contract_Create_InvalidAttributeContractCode
            var req17 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res17 = await http.PostAsync("/api/mst_sys_config/createattribute", req17);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res17.StatusCode);
            var body17 = await res17.Content.ReadAsStringAsync();
            var doc17 = JsonDocument.Parse(body17);
            Assert.False(doc17.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_Create_InvalidAttributeContractCode", doc17.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal(JsonValueKind.Null, doc17.RootElement.GetProperty("Params").GetProperty("Check.strAttributeContractCode").ValueKind);

            // 18. Ca CreateAttribute mã đã tồn tại (ATTR01) -> 400 Bad Request, mã lỗi Mst_Attribute_Contract_CheckDB_OrganExist
            var req18 = new StringContent("{\"AttributeContractCode\":\"ATTR01\",\"AttributeName\":\"Trùng mã\"}", Encoding.UTF8, "application/json");
            var res18 = await http.PostAsync("/api/mst_sys_config/createattribute", req18);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res18.StatusCode);
            var body18 = await res18.Content.ReadAsStringAsync();
            var doc18 = JsonDocument.Parse(body18);
            Assert.False(doc18.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganExist", doc18.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal("ATTR01", doc18.RootElement.GetProperty("Params").GetProperty("Check.AttributeContractCode").GetString());

            // 19. Ca CreateAttribute round-trip thành công tạo mới ATTR04 -> 200 OK
            var req19 = new StringContent("{\"AttributeContractCode\":\"ATTR04\",\"AttributeName\":\"Thời hạn hiệu lực\",\"DefaultValues\":\"12 tháng\"}", Encoding.UTF8, "application/json");
            var res19 = await http.PostAsync("/api/mst_sys_config/createattribute", req19);
            Assert.Equal(System.Net.HttpStatusCode.OK, res19.StatusCode);
            var body19 = await res19.Content.ReadAsStringAsync();
            var doc19 = JsonDocument.Parse(body19);
            Assert.True(doc19.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Lưu thông tin hợp đồng thành công!", doc19.RootElement.GetProperty("Messages")[0].GetString());
            Assert.Equal("/Mst_Sys_Config", doc19.RootElement.GetProperty("RedirectUrl").GetString());

            // 20. Đọc lại qua Index để xác nhận ATTR04 đã tồn tại thật trong DB
            var req20 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res20 = await http.PostAsync("/api/mst_sys_config/index", req20);
            Assert.Equal(System.Net.HttpStatusCode.OK, res20.StatusCode);
            var body20 = await res20.Content.ReadAsStringAsync();
            var doc20 = JsonDocument.Parse(body20);
            var listAttrWithNew = doc20.RootElement.GetProperty("Data").GetProperty("ListMst_Attribute_Contract");
            Assert.Equal(3, listAttrWithNew.GetArrayLength()); // Trước đó 2 (do xoá ATTR03), giờ thêm ATTR04 thành 3
            var attr04 = listAttrWithNew[2];
            Assert.Equal("ATTR04", attr04.GetProperty("AttributeContractCode").GetString());
            Assert.Equal("Thời hạn hiệu lực", attr04.GetProperty("AttributeName").GetString());
            Assert.Equal("12 tháng", attr04.GetProperty("DefaultValues").GetString());
            Assert.Equal("1", attr04.GetProperty("FlagActive").GetString());

            // 21. Tạo lại ATTR04 vừa tạo -> 400 Bad Request, mã lỗi Mst_Attribute_Contract_CheckDB_OrganExist
            var req21 = new StringContent("{\"AttributeContractCode\":\"ATTR04\",\"AttributeName\":\"Thời hạn hiệu lực 2\"}", Encoding.UTF8, "application/json");
            var res21 = await http.PostAsync("/api/mst_sys_config/createattribute", req21);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res21.StatusCode);
            var body21 = await res21.Content.ReadAsStringAsync();
            var doc21 = JsonDocument.Parse(body21);
            Assert.False(doc21.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Attribute_Contract_CheckDB_OrganExist", doc21.RootElement.GetProperty("ErrorCode").GetString());
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
                if (File.Exists(tempDb)) File.Delete(tempDb);
            }
            catch { }
        }
    }
}
