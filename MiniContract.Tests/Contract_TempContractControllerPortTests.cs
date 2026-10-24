using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

// Test hạng mục #78 — Contract_TempContractController/DoSearch
public class Contract_TempContractControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Contract_TempContractControllerPort _controller;

    public Contract_TempContractControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        new Mst_ContractTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_OrgSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_TempContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_TempContractDoSearchSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Contract_TempContractControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    private static JsonElement Root(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return JsonDocument.Parse(JsonSerializer.Serialize(ok.Value)).RootElement;
    }

    [Fact]
    public async Task DoSearch_EmptyBody_NoFilter_Returns3Contracts_PagedByCodeDesc()
    {
        // Gate :194-195 — page=0, recordcount=10; không lọc gì
        // Draft QContract.cs:2229-2252: TCONT003, TCONT002, TCONT001 (desc)
        // Final QContract.cs:2316-2324: inner join Mst_Org (TCONT001 có OrgID "ORG01" -> rớt),
        //   inner join Mst_NNT (Org 7200000001 có 2 NNT -> TCONT002 nhân 2 dòng như nguồn)
        var root = Root(await _controller.DoSearch(null));
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data.GetProperty("PageSize").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());
        Assert.Equal("TCONT003", list[0].GetProperty("TContractCode").GetString());
        Assert.Equal(0, list[0].GetProperty("MyIdxSeq").GetInt64());
        Assert.Equal("TCONT002", list[1].GetProperty("TContractCode").GetString());
        Assert.Equal("TCONT002", list[2].GetProperty("TContractCode").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterStatus0_Returns1Row()
    {
        // Gate :610-613 — FlagActive = '0' ; chỉ TCONT003 có FlagActive '0'
        var root = Root(await _controller.DoSearch(new Contract_TempContract_DoSearchReq { Status = "0" }));
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("TCONT003", list[0].GetProperty("TContractCode").GetString());
        Assert.Equal("0", list[0].GetProperty("FlagActive").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterContractType_JoinsContractTypeName()
    {
        // Gate :606-609 — ContractType = 'HD_KT' ; TCONT002 (TCONT003 là HD_LD)
        var root = Root(await _controller.DoSearch(new Contract_TempContract_DoSearchReq { ContractType = "HD_KT" }));
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("Hợp đồng kinh tế", list[0].GetProperty("mct_ContractTypeName").GetString());
        Assert.Equal("0100000001", list[0].GetProperty("mnnt_MST").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterTContracName_Contains_MatchesVietnamese()
    {
        // Gate :602-605 — TContracName like '%lao động%'
        var root = Root(await _controller.DoSearch(new Contract_TempContract_DoSearchReq { TContracName = "lao động" }));
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal("TCONT003", data.GetProperty("DataList")[0].GetProperty("TContractCode").GetString());
    }

    [Fact]
    public async Task DoSearch_PageSize1_Page1_ReturnsOneCode()
    {
        // QContract.cs:2264-2265 — MyIdxSeq >= start và <= end
        var root = Root(await _controller.DoSearch(new Contract_TempContract_DoSearchReq { Page = 1, RecordCount = 1 }));
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal(1, list[0].GetProperty("MyIdxSeq").GetInt64());
        Assert.Equal("TCONT002", list[0].GetProperty("TContractCode").GetString());
    }

    [Fact]
    public async Task DoSearch_PageSize0_EmptyDataList_ItemCountKept()
    {
        // A.7 — Ft_PageSize = 0 => DataList rỗng, PageCount = 0, ItemCount vẫn là tổng
        var root = Root(await _controller.DoSearch(new Contract_TempContract_DoSearchReq { RecordCount = 0 }));
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_InvalidUtcOffset_ReturnsFail()
    {
        // Ca lỗi: ParseUtcOffset ném FormatException -> PortResult.Fail với mã mặc định của biz
        var res = await _controller.DoSearch(new Contract_TempContract_DoSearchReq
        {
            CreateDTimeUTCFrom = "2026-10-01",
            UtcOffset = "khong-phai-offset"
        });
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var root = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value)).RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Contract_TempContract_Get", root.GetProperty("ErrorCode").GetString());
    }
}
