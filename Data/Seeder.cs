using Microsoft.EntityFrameworkCore;
using MiniContract.Models;

namespace MiniContract.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await MigratePostgresAsync(db);
        await MigrateSqliteAsync(db);

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
        }
    }

    // ── Danh sách cột THÊM khi port 1:1 (dùng chung cho cả 2 nhánh ALTER) ───────────────
    // Tên cột và ý nghĩa xem chú thích trong Models/Entities.cs (mỗi cột đều dẫn file:dòng nguồn).

    /// <summary>Cột thêm vào bảng Contracts — port từ bảng nguồn Contract_Contract.</summary>
    private static readonly (string Col, string Pg, string Sqlite)[] ContractCols =
    [
        ("ContractNo", "text", "TEXT"), ("ContractRefNo", "text", "TEXT"),
        ("TContractCode", "text", "TEXT"), ("TContracName", "text", "TEXT"),
        ("ContractDate", "timestamp", "TEXT"), ("EffDateStart", "timestamp", "TEXT"), ("EffDateEnd", "timestamp", "TEXT"),
        ("CurrencyCode", "text", "TEXT"), ("CurrencyName", "text", "TEXT"),
        ("CurrencyRate", "numeric(18,6) NOT NULL DEFAULT 1", "TEXT NOT NULL DEFAULT '1'"),
        ("ValExchange", "numeric(18,2) NOT NULL DEFAULT 0", "TEXT NOT NULL DEFAULT '0'"),
        ("EffStatus", "integer NOT NULL DEFAULT 0", "INTEGER NOT NULL DEFAULT 0"),
        ("FlagContractAnnex", "boolean NOT NULL DEFAULT false", "INTEGER NOT NULL DEFAULT 0"),
        ("FlagCheckOrderSign", "boolean NOT NULL DEFAULT false", "INTEGER NOT NULL DEFAULT 0"),
        ("FlagSysGenAccessKey", "boolean NOT NULL DEFAULT false", "INTEGER NOT NULL DEFAULT 0"),
        ("FlagCheckOTP", "boolean NOT NULL DEFAULT false", "INTEGER NOT NULL DEFAULT 0"),
        ("ChannelTypeContract", "text", "TEXT"), ("ChannelTypeOTP", "text", "TEXT"),
        ("ContractFileName", "text", "TEXT"), ("ContractFilePath", "text", "TEXT"), ("ContractFileVersion", "text", "TEXT"),
        ("NNTFullName", "text", "TEXT"), ("ContactName", "text", "TEXT"), ("UserNameCreate", "text", "TEXT"),
        ("PositionCreate", "text", "TEXT"), ("PhoneNoCreate", "text", "TEXT"), ("EMailCreate", "text", "TEXT"),
        ("ContractFinishReasonCode", "text", "TEXT"), ("ContractFinishReasonName", "text", "TEXT"),
        ("FinishDescription", "text", "TEXT"), ("FinishedAt", "timestamp", "TEXT"),
        ("ContractFinishReasonFilePath", "text", "TEXT"), ("ContractFinishReasonFilePathVersion", "text", "TEXT"),
        ("FlagActive", "boolean NOT NULL DEFAULT true", "INTEGER NOT NULL DEFAULT 1"),
        ("FlagElement", "boolean NOT NULL DEFAULT false", "INTEGER NOT NULL DEFAULT 0"),
    ];

    /// <summary>Cột thêm vào bảng Parties — port từ bảng nguồn Contract_ContractParty.</summary>
    private static readonly (string Col, string Pg, string Sqlite)[] PartyCols =
    [
        ("PartyCode", "text", "TEXT"), ("CustomerCodeSys", "text", "TEXT"), ("CustomerCode", "text", "TEXT"),
        ("CustomerName", "text", "TEXT"), ("CustomerAddress", "text", "TEXT"), ("CustomerWebsite", "text", "TEXT"),
        ("CustomerMobilePhone", "text", "TEXT"), ("Facebook", "text", "TEXT"), ("ZaloUserID", "text", "TEXT"),
        ("BankCode", "text", "TEXT"), ("BankName", "text", "TEXT"), ("BankAccountNo", "text", "TEXT"),
        ("RepresentName", "text", "TEXT"), ("RepresentPosition", "text", "TEXT"),
        ("ValContract", "numeric(18,2) NOT NULL DEFAULT 0", "TEXT NOT NULL DEFAULT '0'"),
        ("ValPaymented", "numeric(18,2) NOT NULL DEFAULT 0", "TEXT NOT NULL DEFAULT '0'"),
        ("ValRemain", "numeric(18,2) NOT NULL DEFAULT 0", "TEXT NOT NULL DEFAULT '0'"),
        ("ValExchange", "numeric(18,2) NOT NULL DEFAULT 0", "TEXT NOT NULL DEFAULT '0'"),
        ("ContractPartyStatus", "text", "TEXT"),
        ("FlagOrgCreate", "boolean NOT NULL DEFAULT false", "INTEGER NOT NULL DEFAULT 0"),
        ("UserToken", "text", "TEXT"), ("Description", "text", "TEXT"), ("Remark", "text", "TEXT"),
    ];

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "ContractTypes", "Contracts", "Parties", "Signatures", "SendHists" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS minicontract.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON minicontract.\"Orgs\" (\"ApiKey\")",
            // Bảng lịch sử gửi (port 1:1 Contract_SendHist) — DB cũ chưa có nên EnsureCreated không dựng
            """
            CREATE TABLE IF NOT EXISTS minicontract."SendHists" (
                "Id" serial PRIMARY KEY, "OrgId" uuid NOT NULL, "ContractId" integer NOT NULL,
                "PartyCode" text, "UserToken" text, "UserName" text NOT NULL DEFAULT '', "PartyName" text,
                "DTimeSend" timestamp, "ChannelType" integer NOT NULL DEFAULT 0, "BulletinType" integer NOT NULL DEFAULT 0,
                "InfoReceive" text, "FlagActive" text, "Remark" text,
                "LogLUDTime" timestamp NOT NULL DEFAULT now(), "LogLUBy" text)
            """,
        };
        foreach (var t in tables)
            sql.Add($"ALTER TABLE minicontract.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        foreach (var (col, pg, _) in ContractCols)
            sql.Add($"ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"{col}\" {pg}");
        foreach (var (col, pg, _) in PartyCols)
            sql.Add($"ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"{col}\" {pg}");
        foreach (var s in sql)
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }

    /// <summary>
    /// ALTER cho SQLite. <c>EnsureCreated</c> KHÔNG nâng cấp file .db đã tồn tại, nên các cột port 1:1
    /// phải thêm bằng tay (SQLite không có <c>IF NOT EXISTS</c> cho cột ⇒ bọc try/catch, lỗi trùng cột bỏ qua).
    /// </summary>
    private static async Task MigrateSqliteAsync(AppDbContext db)
    {
        if (!db.Database.IsSqlite()) return;
        var sql = new List<string>
        {
            """
            CREATE TABLE IF NOT EXISTS "SendHists" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SendHists" PRIMARY KEY AUTOINCREMENT,
                "OrgId" TEXT NOT NULL, "ContractId" INTEGER NOT NULL,
                "PartyCode" TEXT, "UserToken" TEXT, "UserName" TEXT NOT NULL DEFAULT '', "PartyName" TEXT,
                "DTimeSend" TEXT, "ChannelType" INTEGER NOT NULL DEFAULT 0, "BulletinType" INTEGER NOT NULL DEFAULT 0,
                "InfoReceive" TEXT, "FlagActive" TEXT, "Remark" TEXT,
                "LogLUDTime" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00', "LogLUBy" TEXT)
            """,
        };
        foreach (var s in sql)
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }

        // Chỉ ALTER cột CHƯA có: tránh mỗi lần khởi động lại nổ một loạt log ERR "duplicate column name".
        var coContracts = await CotDangCoAsync(db, "SELECT name AS \"Value\" FROM pragma_table_info('Contracts')");
        var coParties = await CotDangCoAsync(db, "SELECT name AS \"Value\" FROM pragma_table_info('Parties')");
        var alter = new List<string>();
        foreach (var (col, _, lite) in ContractCols)
            if (!coContracts.Contains(col)) alter.Add($"ALTER TABLE \"Contracts\" ADD COLUMN \"{col}\" {lite}");
        foreach (var (col, _, lite) in PartyCols)
            if (!coParties.Contains(col)) alter.Add($"ALTER TABLE \"Parties\" ADD COLUMN \"{col}\" {lite}");
        foreach (var s in alter)
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }

    /// <summary>
    /// Đọc tên cột hiện có của 1 bảng SQLite qua pragma_table_info (so khớp KHÔNG phân biệt hoa/thường).
    /// Chỉ nhận câu SQL hằng do chính file này viết — không ghép chuỗi từ dữ liệu ngoài.
    /// </summary>
    private static async Task<HashSet<string>> CotDangCoAsync(AppDbContext db, string sqlHang)
    {
        try
        {
            var ten = await db.Database.SqlQueryRaw<string>(sqlHang).ToListAsync();
            return new HashSet<string>(ten, StringComparer.OrdinalIgnoreCase);
        }
        catch { return []; }   // không đọc được ⇒ để nhánh ALTER try/catch tự lo
    }
}
