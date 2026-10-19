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
using Xunit.Abstractions;

namespace MiniContract.Tests;

public class Contract_ContractControllerPortTests : IDisposable
{
    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Contract_ContractControllerPort _controller;

    public Contract_ContractControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        new Mst_ContractTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractPartySeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractTBodySeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractElementSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Contract_ContractControllerPort(_db);
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
    public async Task Index_EmptyBody_NoApiCall_ReturnsEmptyPageAndDefaults()
    {
        // Gate :662-669 — init rỗng: KHÔNG gọi API, pageInfo rỗng, init = "search", flag = "1"
        var root = Root(await _controller.Index(null));
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        var pageInfo = data.GetProperty("PageInfo");
        Assert.Equal(0, pageInfo.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, pageInfo.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, pageInfo.GetProperty("DataList").GetArrayLength());
        Assert.Equal("search", data.GetProperty("Init").GetString());
        Assert.Equal("1", data.GetProperty("FlagCheckContractNeedToSign").GetString());
        Assert.Equal($"{DateTime.Now.Year}-01-01", data.GetProperty("CreateDTimeUTCFrom").GetString());
        Assert.Equal($"{DateTime.Now.Year}-01-01", data.GetProperty("ContractDateUTCFrom").GetString());

        // Dropdown loại hợp đồng: FlagActive = '1' -> 4 dòng seed (HD_KT, HD_LD, HD_DV, HD_VAY)
        Assert.Equal(4, data.GetProperty("ListMst_ContractType").GetArrayLength());
    }

    [Fact]
    public async Task Index_InitSearch_ReturnsAllContracts_OrderedByDateDesc()
    {
        var root = Root(await _controller.Index(new Contract_Contract_IndexReq { Init = "search" }));
        var pageInfo = root.GetProperty("Data").GetProperty("PageInfo");

        // #11: seeder thêm HD0004 -> 4 dòng
        Assert.Equal(4, pageInfo.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, pageInfo.GetProperty("PageCount").GetInt32());
        var list = pageInfo.GetProperty("DataList");
        Assert.Equal(4, list.GetArrayLength());
        // QContract.cs:9850-9852 — ContractDateUTC desc
        Assert.Equal("HD0004", list[0].GetProperty("ContractCode").GetString());
        Assert.Equal("HD0003", list[1].GetProperty("ContractCode").GetString());
        Assert.Equal("HD0002", list[2].GetProperty("ContractCode").GetString());
        Assert.Equal("HD0001", list[3].GetProperty("ContractCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByContractType_Returns2Rows()
    {
        var root = Root(await _controller.Index(new Contract_Contract_IndexReq { Init = "search", ContractType = "HD_KT" }));
        var pageInfo = root.GetProperty("Data").GetProperty("PageInfo");

        Assert.Equal(2, pageInfo.GetProperty("ItemCount").GetInt32());
        var list = pageInfo.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("HD0003", list[0].GetProperty("ContractCode").GetString());
        Assert.Equal("HD0001", list[1].GetProperty("ContractCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByCustomerCode_Returns1Row()
    {
        var root = Root(await _controller.Index(new Contract_Contract_IndexReq { Init = "search", CustomerCode = "KH002" }));
        var pageInfo = root.GetProperty("Data").GetProperty("PageInfo");

        Assert.Equal(1, pageInfo.GetProperty("ItemCount").GetInt32());
        var list = pageInfo.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("HD0002", list[0].GetProperty("ContractCode").GetString());
    }

    [Fact]
    public async Task Index_Pagination_Page1_Size2()
    {
        var root = Root(await _controller.Index(new Contract_Contract_IndexReq { Init = "search", Page = 1, RecordCount = 2 }));
        var pageInfo = root.GetProperty("Data").GetProperty("PageInfo");

        // #11: 4 dòng, trang 1 cỡ 2 -> 2 trang, còn 2 dòng (HD0002, HD0001)
        Assert.Equal(4, pageInfo.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, pageInfo.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, pageInfo.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, pageInfo.GetProperty("PageSize").GetInt32());
        var list = pageInfo.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("HD0002", list[0].GetProperty("ContractCode").GetString());
        Assert.Equal("HD0001", list[1].GetProperty("ContractCode").GetString());
    }

    [Fact]
    public async Task Index_CurrentUserSigningInfo_FromOrgID()
    {
        var root = Root(await _controller.Index(new Contract_Contract_IndexReq { Init = "search", OrgID = "7200000001" }));
        var info = root.GetProperty("Data").GetProperty("CurrentUserSigningInfo");
        Assert.Equal("Công ty TNHH Giải Pháp Số Hà Nội", info.GetProperty("name").GetString());
        Assert.Equal("contact@solution.hn", info.GetProperty("email").GetString());
    }

    // ================= #11 DoSearch =================

    [Fact]
    public async Task DoSearch_EmptyBody_ReturnsEmptyPageWithTotalCount()
    {
        // Gate :709-715, :791-792 — Page=0, RecordCount=0 -> Ft_RecordCount=0 -> DataList rỗng, PageCount=0,
        // ItemCount vẫn là MyCount (QContract.cs:9856) = 4 dòng seed
        var root = Root(await _controller.DoSearch(null));
        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(0, data.GetProperty("PageSize").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_FilterByContractNo_Returns1Row()
    {
        // Gate :7985-7988 — Contract_Contract.ContractNo = ContractNo (toán tử "=")
        var root = Root(await _controller.DoSearch(new Contract_Contract_DoSearchReq { ContractNo = "HD0003/2026", RecordCount = 10 }));
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("HD0003", list[0].GetProperty("ContractCode").GetString());
    }

    [Fact]
    public async Task DoSearch_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: ContractNo = HD0003/2026 -> 1 dòng
        var res1 = Root(await _controller.DoSearch(new Contract_Contract_DoSearchReq { ContractNo = "HD0003/2026", RecordCount = 10 }));
        Assert.Equal(1, res1.GetProperty("Data").GetProperty("ItemCount").GetInt32());

        // Bộ lọc 2: ContractNo = HD0004/2026 -> 1 dòng KHÁC (HD0004)
        var res2 = Root(await _controller.DoSearch(new Contract_Contract_DoSearchReq { ContractNo = "HD0004/2026", RecordCount = 10 }));
        Assert.Equal(1, res2.GetProperty("Data").GetProperty("ItemCount").GetInt32());
        Assert.Equal("HD0004", res2.GetProperty("Data").GetProperty("DataList")[0].GetProperty("ContractCode").GetString());

        // Bộ lọc 3: không lọc -> 4 dòng
        var res3 = Root(await _controller.DoSearch(new Contract_Contract_DoSearchReq { RecordCount = 10 }));
        Assert.Equal(4, res3.GetProperty("Data").GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task DoSearch_Pagination_Page1_Size2()
    {
        // Gate :791-792 + QContract.cs:9784-9785, 9864-9866 — start = 1*2 = 2, end = 3
        var root = Root(await _controller.DoSearch(new Contract_Contract_DoSearchReq { Page = 1, RecordCount = 2 }));
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("HD0002", list[0].GetProperty("ContractCode").GetString());
        Assert.Equal("HD0001", list[1].GetProperty("ContractCode").GetString());
    }

    [Fact]
    public async Task RealServer_Verification_FreePort()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_cc_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin/Release/net8.0/MiniContract.dll"));
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
                    if (res.IsSuccessStatusCode) { started = true; break; }
                }
                catch { }
            }
            Assert.True(started, "MiniContract server failed to start within 15 seconds.");

            // Ca 1: body rỗng -> không gọi API, pageInfo rỗng
            var resEmpty = await http.PostAsync("/api/contract_contract/index", new StringContent("{}", Encoding.UTF8, "application/json"));
            var bodyEmpty = await resEmpty.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resEmpty.StatusCode);
            var docEmpty = JsonDocument.Parse(bodyEmpty);
            Assert.Equal(0, docEmpty.RootElement.GetProperty("Data").GetProperty("PageInfo").GetProperty("ItemCount").GetInt32());
            _output?.WriteLine($"[Index EmptyBody] StatusCode={(int)resEmpty.StatusCode} Body={bodyEmpty}");

            // Ca 2: init=search -> 4 dòng seed (#11 thêm HD0004)
            var resAll = await http.PostAsync("/api/contract_contract/index", new StringContent("{\"Init\":\"search\"}", Encoding.UTF8, "application/json"));
            var bodyAll = await resAll.Content.ReadAsStringAsync();
            var docAll = JsonDocument.Parse(bodyAll);
            Assert.Equal(4, docAll.RootElement.GetProperty("Data").GetProperty("PageInfo").GetProperty("ItemCount").GetInt32());
            Assert.Equal(4, docAll.RootElement.GetProperty("Data").GetProperty("PageInfo").GetProperty("DataList").GetArrayLength());
            _output?.WriteLine($"[Index Init=search] StatusCode={(int)resAll.StatusCode} Body={bodyAll}");

            // Ca 3: bộ lọc ContractType=HD_KT -> 2 dòng (khác ca 2)
            var resType = await http.PostAsync("/api/contract_contract/index", new StringContent("{\"Init\":\"search\",\"ContractType\":\"HD_KT\"}", Encoding.UTF8, "application/json"));
            var bodyType = await resType.Content.ReadAsStringAsync();
            var docType = JsonDocument.Parse(bodyType);
            Assert.Equal(2, docType.RootElement.GetProperty("Data").GetProperty("PageInfo").GetProperty("ItemCount").GetInt32());
            _output?.WriteLine($"[Index ContractType=HD_KT] StatusCode={(int)resType.StatusCode} Body={bodyType}");

            // Ca 4: bộ lọc CustomerCode=KH002 -> 1 dòng
            var resCus = await http.PostAsync("/api/contract_contract/index", new StringContent("{\"Init\":\"search\",\"CustomerCode\":\"KH002\"}", Encoding.UTF8, "application/json"));
            var bodyCus = await resCus.Content.ReadAsStringAsync();
            var docCus = JsonDocument.Parse(bodyCus);
            Assert.Equal(1, docCus.RootElement.GetProperty("Data").GetProperty("PageInfo").GetProperty("ItemCount").GetInt32());
            _output?.WriteLine($"[Index CustomerCode=KH002] StatusCode={(int)resCus.StatusCode} Body={bodyCus}");

            // ===== #11 DoSearch =====
            // Ca 5: body rỗng -> ItemCount = 4 (MyCount), DataList rỗng (RecordCount = 0)
            var resDs0 = await http.PostAsync("/api/contract_contract/dosearch", new StringContent("{}", Encoding.UTF8, "application/json"));
            var bodyDs0 = await resDs0.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resDs0.StatusCode);
            var docDs0 = JsonDocument.Parse(bodyDs0);
            Assert.Equal(4, docDs0.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, docDs0.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());
            _output?.WriteLine($"[DoSearch EmptyBody] StatusCode={(int)resDs0.StatusCode} Body={bodyDs0}");

            // Ca 6: ContractNo = HD0003/2026 -> 1 dòng
            var resDs1 = await http.PostAsync("/api/contract_contract/dosearch", new StringContent("{\"ContractNo\":\"HD0003/2026\",\"RecordCount\":10}", Encoding.UTF8, "application/json"));
            var bodyDs1 = await resDs1.Content.ReadAsStringAsync();
            var docDs1 = JsonDocument.Parse(bodyDs1);
            Assert.Equal(1, docDs1.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal("HD0003", docDs1.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("ContractCode").GetString());
            _output?.WriteLine($"[DoSearch ContractNo=HD0003/2026] StatusCode={(int)resDs1.StatusCode} Body={bodyDs1}");

            // Ca 7: ContractNo = HD0004/2026 -> 1 dòng KHÁC (bộ lọc thứ 2 ra số dòng khác ca 6)
            var resDs2 = await http.PostAsync("/api/contract_contract/dosearch", new StringContent("{\"ContractNo\":\"HD0004/2026\",\"RecordCount\":10}", Encoding.UTF8, "application/json"));
            var bodyDs2 = await resDs2.Content.ReadAsStringAsync();
            var docDs2 = JsonDocument.Parse(bodyDs2);
            Assert.Equal(1, docDs2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal("HD0004", docDs2.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("ContractCode").GetString());
            _output?.WriteLine($"[DoSearch ContractNo=HD0004/2026] StatusCode={(int)resDs2.StatusCode} Body={bodyDs2}");

            // Ca 8: không lọc, RecordCount = 10 -> 4 dòng, sắp xếp ContractDateUTC desc
            var resDs3 = await http.PostAsync("/api/contract_contract/dosearch", new StringContent("{\"RecordCount\":10}", Encoding.UTF8, "application/json"));
            var bodyDs3 = await resDs3.Content.ReadAsStringAsync();
            var docDs3 = JsonDocument.Parse(bodyDs3);
            Assert.Equal(4, docDs3.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(4, docDs3.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());
            Assert.Equal("HD0004", docDs3.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("ContractCode").GetString());
            _output?.WriteLine($"[DoSearch NoFilter] StatusCode={(int)resDs3.StatusCode} Body={bodyDs3}");
        }
        finally
        {
            try { if (!proc.HasExited) proc.Kill(true); } catch { }
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }
}
