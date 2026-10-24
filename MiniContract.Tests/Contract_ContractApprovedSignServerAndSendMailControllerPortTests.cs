using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

public class Contract_ContractApprovedSignServerAndSendMailControllerPortTests : IDisposable
{
    private const string ErrPrefix = "Erridn.Skycic.QContract.";

    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Contract_ContractApprovedSignServerAndSendMailControllerPort _controller;

    public Contract_ContractApprovedSignServerAndSendMailControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        new Mst_ContractTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractPartySeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_Contract_HistActionSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Contract_ContractApprovedSignServerAndSendMailControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    private static JsonElement Root(IActionResult result)
    {
        object? val = result switch
        {
            OkObjectResult ok => ok.Value,
            BadRequestObjectResult bad => bad.Value,
            _ => throw new Xunit.Sdk.XunitException($"Unexpected result type {result.GetType().Name}")
        };
        return JsonDocument.Parse(JsonSerializer.Serialize(val)).RootElement;
    }

    // Gate :4351 — fileName luôn lấy từ FileList[0].FileName
    private static Contract_Contract_ApprovedSignServerAndSendMailReq BuildBody(
        string contractCode, string fileName, string flagOrgCreate = "1", string? contractFileNameFlat = null)
        => new()
        {
            ContractCode = contractCode,
            FileList = new()
            {
                new Contract_Contract_ApprovedSignServerAndSendMailFile
                {
                    FileName = fileName,
                    FilePath = "tmp/" + fileName
                }
            },
            FlagOrgCreate = flagOrgCreate,
            FlagElement = "0",
            FlagCheckOTP = "0",
            ContractFileName = contractFileNameFlat
        };

    [Fact]
    public async Task EmptyBody_ReturnsCheckStrJson()
    {
        var res = await _controller.ApprovedSignServerAndSendMail(null);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task MissingContractCode_ReturnsCheckStrJson()
    {
        var req = new Contract_Contract_ApprovedSignServerAndSendMailReq { ContractCode = null };
        var res = await _controller.ApprovedSignServerAndSendMail(req);
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractNotFound_ReturnsInvalid_UserApproved()
    {
        var res = await _controller.ApprovedSignServerAndSendMail(BuildBody("HD9999", "hd9999.pdf"));
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractStatusNotInList_ReturnsInvalid_UserApproved()
    {
        // HD0002 có ContractStatus = SIGNED -> ngoài danh sách {PENDING, ONPROCESS}
        var res = await _controller.ApprovedSignServerAndSendMail(BuildBody("HD0002", "hd0002.pdf"));
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task FileListEmpty_ApprovedPath_ReturnsInvalidContractFileName()
    {
        // Đường Approved (flagOrgCreate="1") -> ContractFileName = FileList[0].FileName = rỗng
        var res = await _controller.ApprovedSignServerAndSendMail(BuildBody("HD0003", ""));
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_InvalidContractFileName", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task PartySignPath_FlatContractFileNameEmpty_ReturnsInvalidContractFileName()
    {
        // Đường PartySign (flagOrgCreate="0"): nguồn dùng ContractFileName phẳng (gate:4578) — rỗng
        // FileList[0].FileName có nhưng không được dùng cho guard ở nhánh này
        var res = await _controller.ApprovedSignServerAndSendMail(BuildBody("HD0003", "hd.pdf", flagOrgCreate: "0", contractFileNameFlat: null));
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_InvalidContractFileName", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ApprovedPath_Success_RoundTrip()
    {
        // HD0003: PENDING/ONPROCESS + ContractNo="HD0003/2026" + 1 party (ONPROCESS)
        var res = await _controller.ApprovedSignServerAndSendMail(BuildBody("HD0003", "signed.pdf", flagOrgCreate: "1"));
        Assert.IsType<OkObjectResult>(res);
        var root = Root(res);
        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("HD0003", data.GetProperty("ContractCode").GetString());
        // Mọi party CONFIRMED -> ContractStatus CONFIRMED
        Assert.Equal("CONFIRMED", data.GetProperty("ContractStatus").GetString());
        // Nhánh Approved: ContractFilePath = FileList[0].FileName
        Assert.Equal("signed.pdf", data.GetProperty("ContractFilePath").GetString());
        Assert.Equal("APPROVED", data.GetProperty("HistActionType").GetString());
        Assert.Equal("Contract_Contract_ApprovedX", (await _db.Set<Contract_Contract_HistAction>()
            .AsNoTracking().SingleAsync(x => x.ContractCode == "HD0003" && x.Version == data.GetProperty("HistActionVersion").GetInt32())).FunctionName);
        // Round-trip: đọc lại thấy ContractStatus đã đổi
        var dbRow = await _db.Set<Contract_Contract>().AsNoTracking().SingleAsync(x => x.ContractCode == "HD0003");
        Assert.Equal("CONFIRMED", dbRow.ContractStatus);
        Assert.Equal("signed.pdf", dbRow.ContractFilePath);
    }

    [Fact]
    public async Task PartySignPath_Success_RoundTrip_WritesFlatContractFileName()
    {
        // HD0004: PENDING + ContractNo="HD0004/2026", KHÔNG có party
        // KHÁC biệt so với _Old: nhánh PartySign dùng ContractFileName phẳng cho write + guard
        var res = await _controller.ApprovedSignServerAndSendMail(
            BuildBody("HD0004", "attached.pdf", flagOrgCreate: "0", contractFileNameFlat: "party.pdf"));
        Assert.IsType<OkObjectResult>(res);
        var root = Root(res);
        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("HD0004", data.GetProperty("ContractCode").GetString());
        // Nhánh PartySign: ContractFilePath = ContractFileName phẳng (không phải FileList[0].FileName)
        Assert.Equal("party.pdf", data.GetProperty("ContractFilePath").GetString());
        Assert.Equal("PartySign", data.GetProperty("HistActionType").GetString());

        var dbRow = await _db.Set<Contract_Contract>().AsNoTracking().SingleAsync(x => x.ContractCode == "HD0004");
        Assert.Equal("party.pdf", dbRow.ContractFilePath);

        var hist = await _db.Set<Contract_Contract_HistAction>()
            .AsNoTracking()
            .Where(x => x.ContractCode == "HD0004")
            .OrderByDescending(x => x.Version)
            .FirstAsync();
        Assert.Equal("Contract_Contract_PartySignX", hist.FunctionName);
        Assert.Equal("PartySign", hist.FunctionActionType);
    }
}
