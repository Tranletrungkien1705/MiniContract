using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models;
using MiniContract.Services;
using Xunit;

namespace MiniContract.Tests;

/// <summary>Test HĐĐT: vòng đời Draft→Sent→PartiallySigned→Completed, ký CKS/OTP, guard gửi/ký/hủy.</summary>
public class ContractServiceTests
{
    private static (AppDbContext db, IContractService svc, OtpService otp, SqliteConnection conn) NewSvc()
    {
        var conn = new SqliteConnection("DataSource=:memory:"); conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        var otp = new OtpService();
        return (db, new ContractService(db, new SignatureService(), otp), otp, conn);
    }

    private static async Task<int> NewContract(IContractService svc, int nParties = 2)
    {
        var parties = Enumerable.Range(1, nParties).Select(i => new ContractParty { Name = $"Bên {i}", Role = (PartyRole)(i - 1) }).ToList();
        return await svc.CreateAsync(new Contract { Title = "HĐ test", Value = 100_000_000 }, parties);
    }

    [Fact]
    public async Task Create_StartsDraft_WithCode()
    {
        var (db, svc, _, conn) = NewSvc(); using (conn)
        {
            var id = await NewContract(svc);
            var c = await svc.GetAsync(id);
            Assert.Equal(ContractStatus.Draft, c!.Status);
            Assert.StartsWith("HD", c.Code);
            Assert.Equal(2, c.Parties.Count);
        }
    }

    [Fact]
    public async Task Send_FromDraft_SetsSent()
    {
        var (db, svc, _, conn) = NewSvc(); using (conn)
        {
            var id = await NewContract(svc);
            await svc.SendAsync(id);
            Assert.Equal(ContractStatus.Sent, (await svc.GetAsync(id))!.Status);
        }
    }

    [Fact]
    public async Task Send_Twice_Throws()
    {
        var (db, svc, _, conn) = NewSvc(); using (conn)
        {
            var id = await NewContract(svc);
            await svc.SendAsync(id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SendAsync(id));
        }
    }

    [Fact]
    public async Task SignCks_BeforeSend_Fails()
    {
        var (db, svc, _, conn) = NewSvc(); using (conn)
        {
            var id = await NewContract(svc);
            var c = await svc.GetAsync(id);
            var (ok, _) = await svc.SignCksAsync(id, c!.Parties[0].Id);
            Assert.False(ok);
        }
    }

    [Fact]
    public async Task SignCks_PartialThenComplete()
    {
        var (db, svc, _, conn) = NewSvc(); using (conn)
        {
            var id = await NewContract(svc);
            await svc.SendAsync(id);
            var c = await svc.GetAsync(id);
            var (ok1, _) = await svc.SignCksAsync(id, c!.Parties[0].Id);
            Assert.True(ok1);
            Assert.Equal(ContractStatus.PartiallySigned, (await svc.GetAsync(id))!.Status);
            await svc.SignCksAsync(id, c.Parties[1].Id);
            var done = await svc.GetAsync(id);
            Assert.Equal(ContractStatus.Completed, done!.Status);
            Assert.NotNull(done.CompletedAt);
        }
    }

    [Fact]
    public async Task SignCks_Twice_SameParty_Fails()
    {
        var (db, svc, _, conn) = NewSvc(); using (conn)
        {
            var id = await NewContract(svc);
            await svc.SendAsync(id);
            var c = await svc.GetAsync(id);
            await svc.SignCksAsync(id, c!.Parties[0].Id);
            var (ok, _) = await svc.SignCksAsync(id, c.Parties[0].Id);
            Assert.False(ok);
        }
    }

    [Fact]
    public async Task SignOtp_CorrectCode_Works_WrongFails()
    {
        var (db, svc, otp, conn) = NewSvc(); using (conn)
        {
            var id = await NewContract(svc, 1);
            await svc.SendAsync(id);
            var c = await svc.GetAsync(id);
            var pid = c!.Parties[0].Id;
            var (bad, _) = await svc.SignOtpAsync(id, pid, "000000");
            Assert.False(bad);
            var code = svc.OtpGenerate(pid);
            var (ok, _) = await svc.SignOtpAsync(id, pid, code);
            Assert.True(ok);
            Assert.Equal(ContractStatus.Completed, (await svc.GetAsync(id))!.Status);  // 1 bên → hoàn tất
        }
    }

    [Fact]
    public async Task Cancel_SetsCancelled_ButNotWhenCompleted()
    {
        var (db, svc, _, conn) = NewSvc(); using (conn)
        {
            var id = await NewContract(svc, 1);
            await svc.CancelAsync(id);
            Assert.Equal(ContractStatus.Cancelled, (await svc.GetAsync(id))!.Status);

            var id2 = await NewContract(svc, 1);
            await svc.SendAsync(id2);
            var c2 = await svc.GetAsync(id2);
            await svc.SignCksAsync(id2, c2!.Parties[0].Id);   // → Completed
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CancelAsync(id2));
        }
    }

    [Fact]
    public async Task Dashboard_CountsCompletedValue()
    {
        var (db, svc, _, conn) = NewSvc(); using (conn)
        {
            var id = await NewContract(svc, 1);
            await svc.SendAsync(id);
            var c = await svc.GetAsync(id);
            await svc.SignCksAsync(id, c!.Parties[0].Id);
            var d = await svc.DashboardAsync();
            Assert.Equal(1, d.Completed);
            Assert.Equal(100_000_000, d.TotalValue);
        }
    }
}
