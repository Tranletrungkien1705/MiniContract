using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using Xunit;

namespace MiniContract.Tests;

public class ZaloControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly ZaloControllerPort _controller;

    public ZaloControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        _controller = new ZaloControllerPort(_db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    private static JsonElement ExtractJson(IActionResult result)
    {
        if (result is OkObjectResult ok)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(ok.Value)).RootElement;
        }
        if (result is BadRequestObjectResult bad)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(bad.Value)).RootElement;
        }
        throw new InvalidOperationException($"Unexpected result type: {result?.GetType().Name}");
    }

    [Fact]
    public void Index_NullReq_ReturnsSuccessWithNullCode()
    {
        // Ca 1: Body rỗng (null) -> trả về Success = true, Code = null (ZaloController.cs:12-16)
        var result = _controller.Index(null);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Code").ValueKind);
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Null, data.GetProperty("Code").ValueKind);
        Assert.Equal(JsonValueKind.Null, data.GetProperty("oa_id").ValueKind);
    }

    [Fact]
    public void Index_EmptyReq_ReturnsSuccessWithNullCode()
    {
        // Ca 2: Body rỗng ({}) -> trả về Success = true, Code = null
        var req = new Zalo_IndexReq();
        var result = _controller.Index(req);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Code").ValueKind);
    }

    [Fact]
    public void Index_WithOaIdAndCode_ReturnsSuccessWithCode()
    {
        // Ca 3: Truyền đầy đủ oa_id và code -> trả về đúng Code đã truyền (ViewBag.Code = code)
        var req = new Zalo_IndexReq
        {
            oa_id = "1358767413636684272",
            code = "ZALO_AUTH_CODE_01"
        };
        var result = _controller.Index(req);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("ZALO_AUTH_CODE_01", root.GetProperty("Code").GetString());
        var data = root.GetProperty("Data");
        Assert.Equal("1358767413636684272", data.GetProperty("oa_id").GetString());
        Assert.Equal("ZALO_AUTH_CODE_01", data.GetProperty("Code").GetString());
    }

    [Fact]
    public void Index_WithDifferentCode_ReturnsSuccess()
    {
        // Ca 4: Bộ tham số thứ hai với giá trị khác để so sánh đối chiếu (ZALO_AUTH_CODE_02 != ZALO_AUTH_CODE_01)
        var req = new Zalo_IndexReq
        {
            oa_id = "246810121416",
            code = "ZALO_AUTH_CODE_02"
        };
        var result = _controller.Index(req);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("ZALO_AUTH_CODE_02", root.GetProperty("Code").GetString());
        var data = root.GetProperty("Data");
        Assert.Equal("246810121416", data.GetProperty("oa_id").GetString());
        Assert.Equal("ZALO_AUTH_CODE_02", data.GetProperty("Code").GetString());
    }

    [Fact]
    public void Index_CodeOnly_ReturnsSuccessWithCode()
    {
        // Ca 5: Chỉ truyền code, không truyền oa_id
        var req = new Zalo_IndexReq
        {
            code = "ONLY_CODE_VAL"
        };
        var result = _controller.Index(req);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("ONLY_CODE_VAL", root.GetProperty("Code").GetString());
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Null, data.GetProperty("oa_id").ValueKind);
        Assert.Equal("ONLY_CODE_VAL", data.GetProperty("Code").GetString());
    }

    [Fact]
    public void Index_QueryStringFallback_ReturnsSuccess()
    {
        // Ca 6: Nhận tham số qua Query string fallback (OAuth redirect GET callback)
        var queryDict = new Dictionary<string, StringValues>
        {
            { "oa_id", "987654321" },
            { "code", "QUERY_AUTH_CODE" }
        };
        _controller.ControllerContext.HttpContext.Request.Query = new QueryCollection(queryDict);

        var result = _controller.Index(null);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("QUERY_AUTH_CODE", root.GetProperty("Code").GetString());
        var data = root.GetProperty("Data");
        Assert.Equal("987654321", data.GetProperty("oa_id").GetString());
        Assert.Equal("QUERY_AUTH_CODE", data.GetProperty("Code").GetString());
    }

    [Fact]
    public async Task RealServer_VerifyAllEndpointsAsync()
    {
        // Chạy kiểm thử trên server thật với FreePort.Get() và SQLite tạm (PORT_RULES §2.6)
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_zalo_{Guid.NewGuid():N}.db");
        var dllPath = typeof(ZaloControllerPort).Assembly.Location;
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
                try
                {
                    var res = await http.GetAsync("/healthz");
                    if (res.IsSuccessStatusCode)
                    {
                        started = true;
                        break;
                    }
                }
                catch
                {
                    await Task.Delay(500);
                }
            }
            Assert.True(started, "Real server did not start within 15 seconds.");

            // 1. POST /api/zalo/index với body rỗng {} -> HTTP 200, Success = true, Code = null
            var emptyContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var resEmpty = await http.PostAsync("/api/zalo/index", emptyContent);
            Assert.True(resEmpty.IsSuccessStatusCode);
            var jsonEmpty = await resEmpty.Content.ReadAsStringAsync();
            var docEmpty = JsonDocument.Parse(jsonEmpty);
            Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docEmpty.RootElement.GetProperty("Code").ValueKind);

            // 2. POST /api/zalo/index với oa_id và code
            var payload1 = JsonSerializer.Serialize(new
            {
                oa_id = "1358767413636684272",
                code = "SRV_CODE_101"
            });
            var content1 = new StringContent(payload1, Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/zalo/index", content1);
            Assert.True(res1.IsSuccessStatusCode);
            var json1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(json1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("SRV_CODE_101", doc1.RootElement.GetProperty("Code").GetString());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal("1358767413636684272", data1.GetProperty("oa_id").GetString());
            Assert.Equal("SRV_CODE_101", data1.GetProperty("Code").GetString());

            // 3. POST /api/zalo/index với code khác (SRV_CODE_202) -> khác biệt rõ ràng
            var payload2 = JsonSerializer.Serialize(new
            {
                oa_id = "9999999999999999999",
                code = "SRV_CODE_202"
            });
            var content2 = new StringContent(payload2, Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/zalo/index", content2);
            Assert.True(res2.IsSuccessStatusCode);
            var json2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(json2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("SRV_CODE_202", doc2.RootElement.GetProperty("Code").GetString());

            // 4. GET /api/zalo/index với Query parameters
            var resGet = await http.GetAsync("/api/zalo/index?oa_id=1358767413636684272&code=SRV_GET_303");
            Assert.True(resGet.IsSuccessStatusCode);
            var jsonGet = await resGet.Content.ReadAsStringAsync();
            var docGet = JsonDocument.Parse(jsonGet);
            Assert.True(docGet.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("SRV_GET_303", docGet.RootElement.GetProperty("Code").GetString());
        }
        finally
        {
            if (!proc.HasExited)
            {
                proc.Kill();
                proc.WaitForExit();
            }

            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
