using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

// Test cho hạng mục #274 Sys_GroupController/SaveObjectInGroup
// Nguồn: Sys_GroupController.cs:731-826 -> SysAccessController.cs:94-165 -> System.cs:1251-1337 / 1008-1249
public class Sys_Group_SaveObjectInGroupPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Sys_Group_SaveObjectInGroupPort _controller;

    public Sys_Group_SaveObjectInGroupPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        new Sys_GroupSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Sys_ObjectSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Sys_AccessSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        var env = new SaveObjectInGroupTestEnv();
        _controller = new Sys_Group_SaveObjectInGroupPort(_db, null, env);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    private async Task<List<Sys_Access>> GetAccessAsync(string groupCode)
    {
        return await _db.Set<Sys_Access>().AsNoTracking()
            .Where(x => x.GroupCode == groupCode)
            .OrderBy(x => x.ObjectCode)
            .ToListAsync();
    }

    [Fact]
    public async Task SaveObjectInGroup_NullBody_ReturnsTitleNoMenuButton()
    {
        var result = await _controller.SaveObjectInGroup(null);
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Chưa chọn Menu - Button gán vào nhóm người dùng!",
            doc.RootElement.GetProperty("Title").GetString());
    }

    [Fact]
    public async Task SaveObjectInGroup_UnknownGroup_ReturnsGroupCodeNotFound()
    {
        var req = new Sys_Group_SaveObjectInGroupReq
        {
            Sys_Group = new Sys_Group { GroupCode = "GRP_NOT_EXIST" },
            Lst_Sys_Access = new List<Sys_Access>
            {
                new Sys_Access { ObjectCode = "MNU_CONTRACT", so_ObjectType = "MENU" }
            }
        };

        var result = await _controller.SaveObjectInGroup(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeNotFound",
            doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task SaveObjectInGroup_UnknownObject_ReturnsObjectCodeNotFound()
    {
        var req = new Sys_Group_SaveObjectInGroupReq
        {
            Sys_Group = new Sys_Group { GroupCode = "GRP_STAFF" },
            Lst_Sys_Access = new List<Sys_Access>
            {
                new Sys_Access { ObjectCode = "MNU_NOT_EXIST", so_ObjectType = "MENU" }
            }
        };

        var result = await _controller.SaveObjectInGroup(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.Equal("Erridn.Skycic.QContract.Sys_Object_CheckDB_ObjectCodeNotFound",
            doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task SaveObjectInGroup_InactiveObject_ReturnsFlagActiveNotMatched()
    {
        var req = new Sys_Group_SaveObjectInGroupReq
        {
            Sys_Group = new Sys_Group { GroupCode = "GRP_STAFF" },
            Lst_Sys_Access = new List<Sys_Access>
            {
                new Sys_Access { ObjectCode = "MNU_ARCHIVED", so_ObjectType = "BUTTON" }
            }
        };

        var result = await _controller.SaveObjectInGroup(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.Equal("Erridn.Skycic.QContract.Sys_Object_CheckDB_FlagActiveNotMatched",
            doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task SaveObjectInGroup_Button_GrantSuccess_RoundTrip()
    {
        var req = new Sys_Group_SaveObjectInGroupReq
        {
            Sys_Group = new Sys_Group { GroupCode = "GRP_STAFF" },
            Lst_Sys_Access = new List<Sys_Access>
            {
                new Sys_Access { ObjectCode = "BTN_SAVE", so_ObjectType = "BUTTON" }
            }
        };

        var result = await _controller.SaveObjectInGroup(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Gán module vào nhóm người dùng thành công!",
            doc.RootElement.GetProperty("Title").GetString());

        var rows = await GetAccessAsync("GRP_STAFF");
        Assert.Single(rows);
        Assert.Equal("BTN_SAVE", rows[0].ObjectCode);
    }

    [Fact]
    public async Task SaveObjectInGroup_Menu_NoSetting_InsertsMenuRow()
    {
        var req = new Sys_Group_SaveObjectInGroupReq
        {
            Sys_Group = new Sys_Group { GroupCode = "GRP_MANAGER" },
            Lst_Sys_Access = new List<Sys_Access>
            {
                new Sys_Access { ObjectCode = "MNU_CONTRACT", so_ObjectType = "MENU" }
            }
        };

        var result = await _controller.SaveObjectInGroup(req);
        Assert.IsType<OkObjectResult>(result);

        var rows = await GetAccessAsync("GRP_MANAGER");
        Assert.Single(rows);
        Assert.Equal("MNU_CONTRACT", rows[0].ObjectCode);
    }

    [Fact]
    public async Task SaveObjectInGroup_EmptyList_DeletesAllGroupAccess()
    {
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        _db.Set<Sys_Access>().AddRange(
            new Sys_Access { GroupCode = "GRP_MANAGER", ObjectCode = "MNU_CONTRACT", LogLUDTimeUTC = now, LogLUBy = "test" },
            new Sys_Access { GroupCode = "GRP_MANAGER", ObjectCode = "MNU_CUSTOMER", LogLUDTimeUTC = now, LogLUBy = "test" });
        await _db.SaveChangesAsync();

        var before = await GetAccessAsync("GRP_MANAGER");
        Assert.Equal(2, before.Count);

        var req = new Sys_Group_SaveObjectInGroupReq
        {
            Sys_Group = new Sys_Group { GroupCode = "GRP_MANAGER" },
            Lst_Sys_Access = new List<Sys_Access>()
        };

        var result = await _controller.SaveObjectInGroup(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.Equal("Xóa module khỏi nhóm thành công!",
            doc.RootElement.GetProperty("Title").GetString());

        var after = await GetAccessAsync("GRP_MANAGER");
        Assert.Empty(after);
    }

    [Fact]
    public async Task SaveObjectInGroup_HttpLive_GrantAndGuard()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_saveobj_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
        if (!File.Exists(dllPath))
        {
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "MiniContract.dll"));
        }
        Assert.True(File.Exists(dllPath), $"Cannot find MiniContract.dll at {dllPath}");

        var port = FreePort.Get();
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(dllPath),
        };
        psi.Environment["PORT"] = port.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(5) };
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
            Assert.True(started, $"MiniContract server failed to start on port {port}.");

            var badBody = "{\"Sys_Group\":{\"GroupCode\":\"GRP_NOT_EXIST\"},\"Lst_Sys_Access\":[{\"ObjectCode\":\"BTN_SAVE\",\"so_ObjectType\":\"BUTTON\"}]}";
            var badRes = await http.PostAsync("/api/sys_group/saveobjectingroup",
                new StringContent(badBody, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, badRes.StatusCode);
            var badDoc = JsonDocument.Parse(await badRes.Content.ReadAsStringAsync());
            Assert.Equal("Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeNotFound",
                badDoc.RootElement.GetProperty("ErrorCode").GetString());

            var okBody = "{\"Sys_Group\":{\"GroupCode\":\"GRP_STAFF\"},\"Lst_Sys_Access\":[{\"ObjectCode\":\"BTN_SAVE\",\"so_ObjectType\":\"BUTTON\"}]}";
            var okRes = await http.PostAsync("/api/sys_group/saveobjectingroup",
                new StringContent(okBody, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.OK, okRes.StatusCode);
            var okDoc = JsonDocument.Parse(await okRes.Content.ReadAsStringAsync());
            Assert.True(okDoc.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Gán module vào nhóm người dùng thành công!",
                okDoc.RootElement.GetProperty("Title").GetString());
        }
        finally
        {
            try { if (!proc.HasExited) proc.Kill(true); } catch { }
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }
}

public class SaveObjectInGroupTestEnv : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "minicontract_saveobj_test");
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "MiniContract.Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "minicontract_saveobj_test");
    public string EnvironmentName { get; set; } = "Test";
}
