using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;
using Xunit.Abstractions;

namespace MiniContract.Tests;

public class Contract_ContractSaveApprovedSignAndSendMailControllerPortTests : IDisposable
{
    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Contract_ContractSaveApprovedSignAndSendMailControllerPort _controller;

    public Contract_ContractSaveApprovedSignAndSendMailControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractPartySeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractTBodySeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_Contract_HistActionSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Contract_ContractSaveApprovedSignAndSendMailControllerPort(_db);
    }

    public void Dispose() { _db.Dispose(); _conn.Dispose(); }

    [Fact]
    public async Task BodyRong_TraLoiContractCodeNull()
    {
        var r = await _controller.SaveApprovedSignAndSendMail(null);
        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(r);
    }

    [Fact]
    public async Task ContractFileNameNull_TraLoiInvalidContractFileName()
    {
        var req = new Contract_Contract_SaveApprovedSignAndSendMailReq
        {
            ContractCode = "HD0001",
            ContractNo = "HD0001/2026",
            ContractFileName = null,
            PartyList = new List<Contract_Contract_SaveApprovedSignAndSendMail_PartyItem>
            {
                new() { PartyCode = "P1", MST = "0100109106", CustomerCodeSys = "CUST001", CustomerEmail = "abc@x.vn" }
            }
        };
        var r = await _controller.SaveApprovedSignAndSendMail(req);
        var bad = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(r);
        var json = JsonSerializer.Serialize(bad.Value);
        Assert.Contains("Contract_Contract_Approved_InvalidContractFileName", json);
    }

    [Fact]
    public async Task HappyPath_HD0001_SaveApproveSendMail()
    {
        var req = new Contract_Contract_SaveApprovedSignAndSendMailReq
        {
            OrgID = "7200000001",
            WAUserCode = "U01",
            ContractCode = "HD0001",
            ContractNo = "HD0001/2026",
            ContractName = "HĐ test",
            ContractFileName = "HD0001_signed.pdf",
            SignedFileContent = "SIGNED_CONTENT",
            ContractBody = "Body nội dung",
            PartyList = new List<Contract_Contract_SaveApprovedSignAndSendMail_PartyItem>
            {
                new() { PartyCode = "P1", MST = "0100109106", CustomerCodeSys = "CUST001",
                        CustomerEmail = "a@x.vn;b@x.vn" }
            }
        };
        var r = await _controller.SaveApprovedSignAndSendMail(req);
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(r);

        var c = await _db.Set<Contract_Contract>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0001");
        Assert.Equal("HD0001_signed.pdf", c.ContractFilePath);
        Assert.Equal("CONFIRMED", c.ContractStatus);

        var hist = await _db.Set<Contract_Contract_HistAction>().AsNoTracking()
            .Where(x => x.ContractCode == "HD0001" && x.FunctionActionType == "APPROVED").ToListAsync();
        Assert.NotEmpty(hist);

        var p = await _db.Set<Contract_ContractParty>().AsNoTracking()
            .FirstAsync(x => x.ContractCode == "HD0001" && x.MST == "0100109106");
        Assert.Equal("a@x.vn;b@x.vn", p.EmailSend);
        Assert.NotNull(p.SendEmailDTimeUTC);
    }
}
