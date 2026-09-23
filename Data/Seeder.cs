using Microsoft.EntityFrameworkCore;
using MiniContract.Models;

namespace MiniContract.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await MigratePostgresAsync(db);

        if (!await db.Orgs.AnyAsync(o => o.Id == TenantContext.DefaultOrgId))
        {
            db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo Contract", ApiKey = TenantContext.DefaultApiKey });
            await db.SaveChangesAsync();
        }

        if (!await db.ContractTypes.AnyAsync())
        {
            db.ContractTypes.AddRange(
                new ContractType { Name = "Hợp đồng mua bán", Code = "MB", BodyTemplate = "Bên A đồng ý bán, Bên B đồng ý mua hàng hóa/dịch vụ theo các điều khoản sau…" },
                new ContractType { Name = "Hợp đồng đại lý", Code = "DL", BodyTemplate = "Bên A ủy quyền cho Bên B làm đại lý phân phối sản phẩm trong phạm vi…" },
                new ContractType { Name = "Hợp đồng dịch vụ", Code = "DV", BodyTemplate = "Bên A cung cấp dịch vụ cho Bên B với phạm vi công việc…" },
                new ContractType { Name = "Hợp đồng lao động", Code = "LD", BodyTemplate = "Bên A (người sử dụng lao động) và Bên B (người lao động) thỏa thuận…" });
            await db.SaveChangesAsync();
        }

        if (!await db.Contracts.AnyAsync())
        {
            var types = await db.ContractTypes.OrderBy(t => t.Id).ToListAsync();
            int n = 0;
            Contract C(string title, int typeIdx, decimal value, ContractStatus status,
                (string name, PartyRole role, string email, bool signed)[] parties)
            {
                n++;
                var c = new Contract
                {
                    Code = $"HD{DateTime.Now:yyMM}-{n:D4}", Title = title, TypeId = types[typeIdx].Id,
                    Body = types[typeIdx].BodyTemplate ?? "", Value = value, Status = status,
                    CreatedBy = "seed", CreatedAt = DateTime.Now.AddDays(-n * 2),
                    SentAt = status >= ContractStatus.Sent ? DateTime.Now.AddDays(-n) : null,
                    CompletedAt = status == ContractStatus.Completed ? DateTime.Now.AddDays(-n + 1) : null
                };
                int order = 1;
                foreach (var p in parties)
                    c.Parties.Add(new ContractParty
                    {
                        Name = p.name, Role = p.role, Email = p.email, SignOrder = order++,
                        HasSigned = p.signed, SignedAt = p.signed ? DateTime.Now.AddDays(-n + 1) : null
                    });
                return c;
            }

            db.Contracts.AddRange(
                C("Mua bán 100 xe máy Honda", 0, 250_000_000, ContractStatus.Completed,
                    [("Công ty HTC", PartyRole.PartyA, "htc@corp.vn", true), ("Đại lý Minh Anh", PartyRole.PartyB, "minhanh@dl.vn", true)]),
                C("Đại lý phân phối khu vực Miền Nam", 1, 0, ContractStatus.PartiallySigned,
                    [("Công ty HTC", PartyRole.PartyA, "htc@corp.vn", true), ("Đại lý Phương Nam", PartyRole.PartyB, "pn@dl.vn", false)]),
                C("Dịch vụ bảo trì hệ thống 2026", 2, 120_000_000, ContractStatus.Sent,
                    [("Công ty HTC", PartyRole.PartyA, "htc@corp.vn", false), ("TNHH Giải pháp ABC", PartyRole.PartyB, "abc@sol.vn", false)]),
                C("Hợp đồng lao động - NV Kinh doanh", 3, 15_000_000, ContractStatus.Draft,
                    [("Công ty HTC", PartyRole.PartyA, "hr@corp.vn", false), ("Nguyễn Văn A", PartyRole.PartyB, "vana@gmail.com", false)])
            );
            await db.SaveChangesAsync();

            // Phụ lục mẫu: gắn vào hợp đồng gốc đã hoàn tất (HD...-0001).
            var parent = await db.Contracts.OrderBy(c => c.Id).FirstAsync();
            var annex = new Contract
            {
                Code = $"PL{DateTime.Now:yyMM}-0001", Title = "Phụ lục 01 — điều chỉnh giá trị hợp đồng",
                TypeId = parent.TypeId, Body = "Hai bên thống nhất điều chỉnh giá trị hợp đồng gốc tăng thêm 20.000.000 đ.",
                Value = 20_000_000, Status = ContractStatus.Draft, CreatedBy = "seed",
                IsAnnex = true, ParentContractId = parent.Id, ParentContractCode = parent.Code,
                CreatedAt = DateTime.Now.AddDays(-1)
            };
            annex.Parties.Add(new ContractParty { Name = "Công ty HTC", Role = PartyRole.PartyA, Email = "htc@corp.vn", SignOrder = 1 });
            annex.Parties.Add(new ContractParty { Name = "Đại lý Minh Anh", Role = PartyRole.PartyB, Email = "minhanh@dl.vn", SignOrder = 2 });
            db.Contracts.Add(annex);
            await db.SaveChangesAsync();

            // Nhật ký thao tác mẫu (audit trail) cho vài hợp đồng — dựng "vòng đời" có thể kiểm toán.
            var all = await db.Contracts.OrderBy(c => c.Id).ToListAsync();
            var hist = new List<ContractHistory>();
            foreach (var c in all)
            {
                hist.Add(new ContractHistory { ContractId = c.Id, Action = HistoryAction.Created, Actor = c.CreatedBy, Description = $"Tạo {c.Kind.ToLower()} {c.Code}", At = c.CreatedAt });
                if (c.SentAt != null)
                    hist.Add(new ContractHistory { ContractId = c.Id, Action = HistoryAction.Sent, Actor = c.CreatedBy, Description = $"Gửi {c.Kind.ToLower()} {c.Code} cho {c.Parties.Count} bên ký", At = c.SentAt.Value });
                foreach (var p in c.Parties.Where(x => x.HasSigned))
                    hist.Add(new ContractHistory { ContractId = c.Id, Action = HistoryAction.Signed, Actor = p.Name, Description = $"{p.Name} ký — {Ui.Role(p.Role)}", At = p.SignedAt ?? c.CreatedAt });
                if (c.CompletedAt != null)
                    hist.Add(new ContractHistory { ContractId = c.Id, Action = HistoryAction.Completed, Actor = "system", Description = $"Đủ chữ ký các bên — {c.Code} hoàn tất", At = c.CompletedAt.Value });
            }
            db.Histories.AddRange(hist);
            await db.SaveChangesAsync();
        }
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "ContractTypes", "Contracts", "Parties", "Signatures", "Histories" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS minicontract.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON minicontract.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in tables)
            sql.Add($"ALTER TABLE minicontract.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"IsAnnex\" boolean NOT NULL DEFAULT false");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"ParentContractId\" integer NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"ParentContractCode\" text NULL");
        foreach (var s in sql)
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
