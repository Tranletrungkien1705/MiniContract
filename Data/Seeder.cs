using Microsoft.EntityFrameworkCore;
using MiniContract.Models;
using MiniContract.Services;

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

        // Hợp đồng mẫu (Contract_TempContract) — minh họa soạn nhanh hợp đồng từ mẫu.
        if (!await db.Templates.AnyAsync())
        {
            var types = await db.ContractTypes.OrderBy(t => t.Id).ToListAsync();
            if (types.Count >= 2)
            {
                db.Templates.AddRange(
                    new ContractTemplate { Code = "M-MUABAN", Name = "Mẫu hợp đồng mua bán hàng hóa", TypeId = types[0].Id, Body = "Bên A bán và Bên B mua hàng hóa theo danh mục đính kèm. Giá trị, thời hạn giao nhận và phương thức thanh toán do hai bên thỏa thuận.", Remark = "Mẫu chuẩn cho giao dịch mua bán", CreatedBy = "seed" },
                    new ContractTemplate { Code = "M-DAILY", Name = "Mẫu hợp đồng đại lý phân phối", TypeId = types[1].Id, Body = "Bên A chỉ định Bên B làm đại lý phân phối sản phẩm trong khu vực thỏa thuận. Bên B hưởng hoa hồng theo doanh số.", Remark = "Mẫu cho mạng lưới đại lý", CreatedBy = "seed" });
                await db.SaveChangesAsync();
            }
        }

        // Nhóm hợp đồng mẫu (Contract_TempGroup) — minh họa nhóm + thuộc tính dùng chung.
        if (!await db.TemplateGroups.AnyAsync())
        {
            var g1 = new ContractTemplateGroup
            {
                Code = "G-THUONGMAI", Name = "Nhóm hợp đồng thương mại",
                ContractName = "Hợp đồng mua bán", Body = "Nhóm hợp đồng phục vụ giao dịch thương mại mua bán hàng hóa.",
                Remark = "Nhóm chuẩn cho giao dịch thương mại", CreatedBy = "seed"
            };
            g1.Attributes.Add(new ContractAttributeGroup { AttributeCode = "LOAI_HD", AttributeValue = "Mua bán", CreatedBy = "seed" });
            g1.Attributes.Add(new ContractAttributeGroup { AttributeCode = "THOI_HAN", AttributeValue = "12 tháng", CreatedBy = "seed" });
            var g2 = new ContractTemplateGroup
            {
                Code = "G-DAILY", Name = "Nhóm hợp đồng đại lý",
                ContractName = "Hợp đồng đại lý", Body = "Nhóm hợp đồng cho mạng lưới đại lý phân phối.",
                Remark = "Nhóm cho mạng lưới đại lý", CreatedBy = "seed"
            };
            g2.Attributes.Add(new ContractAttributeGroup { AttributeCode = "KHU_VUC", AttributeValue = "Toàn quốc", CreatedBy = "seed" });
            db.TemplateGroups.AddRange(g1, g2);
            await db.SaveChangesAsync();
        }

        // Quy tắc đánh số hợp đồng theo loại (Mst_ContractTypeContractNo) — minh họa tiền tố/hậu tố.
        if (!await db.NumberRules.AnyAsync())
        {
            var types = await db.ContractTypes.OrderBy(t => t.Id).ToListAsync();
            if (types.Count >= 2)
            {
                db.NumberRules.AddRange(
                    new ContractNumberRule { TypeId = types[0].Id, TypefixCode = Typefix.Prefix, TypefixInput = "MB-", SeqNumberLength = 4, NumberStart = 1 },
                    new ContractNumberRule { TypeId = types[1].Id, TypefixCode = Typefix.Postfix, TypefixInput = "/DL", SeqNumberLength = 3, NumberStart = 1 });
                await db.SaveChangesAsync();
            }
        }
        // Cấu hình loại hợp đồng (Mst_ContractTypeDtl) — minh họa kênh gửi HĐ/OTP + tự sinh số.
        if (!await db.TypeConfigs.AnyAsync())
        {
            var types = await db.ContractTypes.OrderBy(t => t.Id).ToListAsync();
            if (types.Count >= 2)
            {
                db.TypeConfigs.AddRange(
                    new ContractTypeConfig { TypeId = types[0].Id, GenContractNo = true, EmailContract = true, SmsContract = true, ZaloContract = false, EmailOtp = true, SmsOtp = true, ZaloOtp = false, Remark = "Loại thương mại — gửi HĐ qua Email/SMS, OTP qua Email/SMS", CreatedBy = "seed" },
                    new ContractTypeConfig { TypeId = types[1].Id, GenContractNo = true, EmailContract = true, SmsContract = false, ZaloContract = true, EmailOtp = true, SmsOtp = false, ZaloOtp = true, Remark = "Loại đại lý — gửi HĐ qua Email/Zalo, OTP qua Email/Zalo", CreatedBy = "seed" });
                await db.SaveChangesAsync();
            }
        }
        if (!await db.FinishReasons.AnyAsync())
        {
            db.FinishReasons.AddRange(
                new FinishedContractReason { Code = "LR-HOANTHANH", Name = "Hoàn thành nghĩa vụ hợp đồng", Type = FinishType.Finished, Description = "Các bên đã thực hiện đầy đủ nghĩa vụ theo hợp đồng." },
                new FinishedContractReason { Code = "LR-HETHAN", Name = "Hết thời hạn hiệu lực", Type = FinishType.Finished, Description = "Hợp đồng hết thời hạn hiệu lực theo thỏa thuận." },
                new FinishedContractReason { Code = "LR-CHAMDUT", Name = "Chấm dứt trước hạn theo thỏa thuận", Type = FinishType.Stopped, Description = "Hai bên thống nhất chấm dứt hợp đồng trước thời hạn." },
                new FinishedContractReason { Code = "LR-VIPHAM", Name = "Chấm dứt do vi phạm", Type = FinishType.Stopped, Description = "Một bên vi phạm nghĩa vụ, bên còn lại chấm dứt hợp đồng." });
            await db.SaveChangesAsync();
        }

        // Chữ ký số của tổ chức (Mst_OrgCKS) — minh họa chứng thư còn hiệu lực + đã hết hạn.
        if (!await db.OrgCertificates.AnyAsync())
        {
            db.OrgCertificates.AddRange(
                new OrgCertificate { CANumber = "0101234567-001", CAOrg = "VNPT-CA", EffectiveFrom = DateTime.Now.AddMonths(-6), EffectiveTo = DateTime.Now.AddMonths(6), CtsPath = "/certs/htc-vnpt.pfx", Active = true, CreatedBy = "seed" },
                new OrgCertificate { CANumber = "0101234567-002", CAOrg = "Viettel-CA", EffectiveFrom = DateTime.Now.AddYears(-2), EffectiveTo = DateTime.Now.AddMonths(-1), CtsPath = "/certs/htc-viettel.pfx", Active = true, CreatedBy = "seed" });
            await db.SaveChangesAsync();
        }

        // Cấu hình ký của tổ chức (Mst_OrgSignConfig) — minh họa ký từ xa + ký server.
        if (!await db.SignConfigs.AnyAsync())
        {
            db.SignConfigs.AddRange(
                new OrgSignConfig
                {
                    SignType = SignType.Remote, NetworkID = "NET-HTC", OrgCode = "HTC",
                    CANumber = "0101234567-001", CAOrg = "VNPT-CA",
                    EffectiveFrom = DateTime.Now.AddMonths(-6), EffectiveTo = DateTime.Now.AddMonths(6),
                    SupplierCode = "EFY", RemoteSignAgreementUUID = "8f3c1a2b-remote", RemoteSignPassCode = "rs-pass",
                    AuthenCode = "OTP", Active = true, CreatedBy = "seed"
                },
                new OrgSignConfig
                {
                    SignType = SignType.Server, NetworkID = "NET-HTC", OrgCode = "HTC",
                    CANumber = "0101234567-001", CAOrg = "VNPT-CA",
                    EffectiveFrom = DateTime.Now.AddMonths(-6), EffectiveTo = DateTime.Now.AddMonths(6),
                    ServerSignFilePath = "/sign/htc-server.pfx", ServerSignPassword = "srv-pass",
                    AuthenCode = "CKS", Active = true, CreatedBy = "seed"
                });
            await db.SaveChangesAsync();
        }

        // Cấu hình kênh gửi của tổ chức (Mst_Channel) — minh họa kênh gửi HĐ/OTP + thông số Email/SMS/Zalo.
        if (!await db.ChannelConfigs.AnyAsync())
        {
            var ch = new ChannelConfig
            {
                NetworkID = "NET-HTC", ContractChannel = ChannelType.Email, OtpChannel = ChannelType.Sms,
                AccessKeyChannel = ChannelType.Email, Active = true, CreatedBy = "seed",
                Email = new ChannelEmailConfig
                {
                    MailFrom = "no-reply@corp.vn", DisplayNameMailFrom = "Hợp đồng điện tử HTC",
                    APIsSendMail = "https://mail-gw.corp.vn/api/send", ApiKeySendMail = "mail-key-demo",
                    SolutionCodeSendMail = "QCONTRACT", SubFormCodeEmailContract = "MAIL-HD",
                    SubFormCodeEmailOtp = "MAIL-OTP", Active = true, CreatedBy = "seed"
                },
                Sms = new ChannelSmsConfig
                {
                    SmsBrandName = "HTC", SubFormCodeContractSms = "SMS-HD",
                    SubFormCodeSmsOtp = "SMS-OTP", Active = true, CreatedBy = "seed"
                },
                Zalo = new ChannelZaloConfig
                {
                    ZaloOaId = "oa-htc-001", AppId = "app-htc", AccessToken = "zalo-access-demo",
                    RefreshToken = "zalo-refresh-demo", AppSecret = "zalo-secret-demo",
                    SubFormCodeContractZaloUserId = "ZALO-HD", SubFormCodeOtp = "ZALO-OTP",
                    Active = true, CreatedBy = "seed"
                }
            };
            db.ChannelConfigs.Add(ch);
            await db.SaveChangesAsync();
        }

        // Mẫu nội dung gửi (Mst_SubmissionForm) — minh họa mẫu gửi HĐ/OTP qua Email + Zalo.
        if (!await db.SubmissionForms.AnyAsync())
        {
            var mailHd = new SubmissionForm
            {
                SubFormCode = "MAIL-HD", SubFormName = "Mẫu gửi hợp đồng qua Email",
                ChannelType = ChannelType.Email, BulletinType = BulletinType.Contract, Active = true, CreatedBy = "seed"
            };
            mailHd.Messages.Add(new SubmissionFormMessage { SubFormCode = "MAIL-HD", SubTitle = "Thông báo ký hợp đồng", Message = "Kính gửi {TenBen}, vui lòng ký hợp đồng {SoHopDong} tại liên kết đính kèm.", CreatedBy = "seed" });

            var mailOtp = new SubmissionForm
            {
                SubFormCode = "MAIL-OTP", SubFormName = "Mẫu gửi OTP qua Email",
                ChannelType = ChannelType.Email, BulletinType = BulletinType.Otp, Active = true, CreatedBy = "seed"
            };
            mailOtp.Messages.Add(new SubmissionFormMessage { SubFormCode = "MAIL-OTP", SubTitle = "Mã xác thực ký hợp đồng", Message = "Mã OTP của bạn là {OTP}. Mã có hiệu lực trong 2 phút.", CreatedBy = "seed" });

            var zaloHd = new SubmissionForm
            {
                SubFormCode = "ZALO-HD", SubFormName = "Mẫu gửi hợp đồng qua Zalo ZNS",
                ChannelType = ChannelType.Zalo, BulletinType = BulletinType.Contract, IdZns = "123456", Active = true, CreatedBy = "seed"
            };
            zaloHd.Messages.Add(new SubmissionFormMessage { SubFormCode = "ZALO-HD", SubTitle = "Hợp đồng cần ký", Message = "{TenBen} ơi, hợp đồng {SoHopDong} đang chờ bạn ký.", CreatedBy = "seed" });
            zaloHd.ZnsParams.Add(new SubmissionFormZns { SubFormCode = "ZALO-HD", ParamContractCodeZns = "customer_name", SourceDataType = "Contract", ParamContractCode = "TenBen", ParamValue = "{TenBen}", CreatedBy = "seed" });
            zaloHd.ZnsParams.Add(new SubmissionFormZns { SubFormCode = "ZALO-HD", ParamContractCodeZns = "contract_no", SourceDataType = "Contract", ParamContractCode = "SoHopDong", ParamValue = "{SoHopDong}", CreatedBy = "seed" });

            db.SubmissionForms.AddRange(mailHd, mailOtp, zaloHd);
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
            var parent = await db.Contracts.OrderBy(c => c.Id).FirstAsync();            var annex = new Contract
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

            // File hợp đồng mẫu (Contract_Contract_UpdateFilePath) — minh họa file bản thể hiện đã ký.
            var completedForFile = await db.Contracts.FirstOrDefaultAsync(c => c.Status == ContractStatus.Completed);
            if (completedForFile != null)
            {
                completedForFile.FileName = $"HopDong-{completedForFile.Code}.pdf";
                completedForFile.FilePath = $"/files/contracts/{completedForFile.Code}.pdf";
                completedForFile.FileVersion = "1";
                completedForFile.FileUpdatedAt = DateTime.Now.AddDays(-1);
                completedForFile.FileUpdatedBy = "seed";
                await db.SaveChangesAsync();
            }

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

            // Ô ký mẫu (Contract_ContractElement) cho hợp đồng đã gửi ký — minh họa các loại ô ký.
            var sent = all.FirstOrDefault(c => c.Status == ContractStatus.Sent);
            if (sent != null)
            {
                var pa = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyA);
                var pb = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyB);
                db.Elements.AddRange(
                    new ContractElement { ContractId = sent.Id, PartyId = pa?.Id, ElementCode = "EL-A-SIGN", ElementName = "Chữ ký Bên A", Type = ElementType.Digital, PageIdx = 1, ElementX = 60, ElementY = 700, ElementWidth = 150, ElementHeight = 50 },
                    new ContractElement { ContractId = sent.Id, PartyId = pb?.Id, ElementCode = "EL-B-SIGN", ElementName = "Chữ ký Bên B", Type = ElementType.Electronic, PageIdx = 1, ElementX = 320, ElementY = 700, ElementWidth = 150, ElementHeight = 50 },
                    new ContractElement { ContractId = sent.Id, PartyId = pb?.Id, ElementCode = "EL-B-INIT", ElementName = "Ký tắt Bên B", Type = ElementType.Short, PageIdx = 1, ElementX = 320, ElementY = 60, ElementWidth = 80, ElementHeight = 30 });
                await db.SaveChangesAsync();
            }

            // Người kiểm tra mẫu (Contract_Checker) cho hợp đồng đã gửi ký — minh họa luồng kiểm tra tuần tự.
            if (sent != null)
            {
                db.Checkers.AddRange(
                    new ContractChecker { ContractId = sent.Id, UserCode = "kt.le", UserName = "Lê Thị Kiểm", Position = "Chuyên viên pháp chế", Idx = 1, IsChecker = true, Sequential = true, HasChecked = true, CheckedAt = DateTime.Now.AddDays(-1), Remark = "Đã đối chiếu điều khoản" },
                    new ContractChecker { ContractId = sent.Id, UserCode = "kt.tran", UserName = "Trần Văn Duyệt", Position = "Trưởng phòng", Idx = 2, IsChecker = true, Sequential = true, HasChecked = false });
                sent.CheckerStatus = CheckerStatus.Pending;
                await db.SaveChangesAsync();

                // Phê duyệt hợp đồng mẫu (Contract_Contract_Approved) — minh họa hợp đồng đã được người kiểm tra duyệt.
                var completed = all.FirstOrDefault(c => c.Status == ContractStatus.Completed);
                if (completed != null)
                {
                    db.Checkers.Add(new ContractChecker
                    {
                        ContractId = completed.Id, UserCode = "kt.le", UserName = "Lê Thị Kiểm", Position = "Chuyên viên pháp chế",
                        Idx = 1, IsChecker = true, Sequential = true, HasChecked = true,
                        CheckedAt = DateTime.Now.AddDays(-2), ApprovedAt = DateTime.Now.AddDays(-2), Remark = "Đồng ý phê duyệt"
                    });
                    completed.ApprovedAt = DateTime.Now.AddDays(-2);
                    completed.ApprovedBy = "kt.le";
                    completed.CheckerStatus = CheckerStatus.OnProcess;
                    await db.SaveChangesAsync();
                }
            }

            // Phân quyền hợp đồng mẫu (Contract_UserInContract) — minh họa danh sách người dùng được phép xử lý.
            if (sent != null)
            {
                db.UserAssignments.AddRange(
                    new ContractUserInContract { ContractId = sent.Id, UserCode = "kt.le", UserName = "Lê Thị Kiểm", Email = "kt.le@corp.vn", AssignedBy = "seed" },
                    new ContractUserInContract { ContractId = sent.Id, UserCode = "kt.tran", UserName = "Trần Văn Duyệt", Email = "kt.tran@corp.vn", AssignedBy = "seed" });
                await db.SaveChangesAsync();
            }

            // Người ký của hợp đồng mẫu (Contract_ContractUser) — minh họa người ký theo bên + trạng thái ký/gửi.
            if (sent != null)
            {
                var pa = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyA);
                var pb = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyB);
                db.Signers.AddRange(
                    new ContractSigner
                    {
                        ContractId = sent.Id, PartyId = pa?.Id, PartyCode = pa != null ? $"P{pa.Id}" : "",
                        UserCodeSysSign = "giam.doc.htc", UserCodeSign = "giam.doc.htc", UserNameSign = "Giám đốc HTC",
                        UserEmail = "giamdoc@corp.vn", Idx = 1, SignStatus = UserSignStatus.Confirmed,
                        ConfirmDTimeUTC = DateTime.Now.AddDays(-1), ConfirmBy = "seed",
                        FlagSendUser = true, SendDateUTC = DateTime.Now.AddDays(-1), SendBy = "seed",
                        UserToken = "ut_seed_a", CreatedBy = "seed"
                    },
                    new ContractSigner
                    {
                        ContractId = sent.Id, PartyId = pb?.Id, PartyCode = pb != null ? $"P{pb.Id}" : "",
                        UserCodeSysSign = "phuong.nam", UserCodeSign = "phuong.nam", UserNameSign = "Nguyễn Phương Nam",
                        UserEmail = "pn@dl.vn", Idx = 2, SignStatus = UserSignStatus.Pending,
                        FlagSendUser = true, SendDateUTC = DateTime.Now.AddHours(-6), SendBy = "seed",
                        UserToken = "ut_seed_b", CreatedBy = "seed"
                    });
                await db.SaveChangesAsync();
            }

            // Ghi chú của một bên mẫu (Contract_Contract_Party_UpdateRemark) — minh họa cập nhật Remark cho bên.
            if (sent != null)
            {
                var pb = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyB);
                if (pb != null)
                {
                    pb.Remark = "Đã xác nhận thông tin pháp lý, chờ ký.";
                    await db.SaveChangesAsync();
                    db.Histories.Add(new ContractHistory
                    {
                        ContractId = sent.Id, Action = HistoryAction.UpdateRemark, Actor = "seed",
                        Description = $"Cập nhật ghi chú cho {pb.Name} ({Ui.Role(pb.Role)}) — {pb.Remark}",
                        At = DateTime.Now.AddHours(-3)
                    });
                    await db.SaveChangesAsync();
                }
            }

            // Ký hợp đồng bởi một bên mẫu (Contract_Contract_PartySign) — minh họa bên A đã ký, bên B chờ ký.
            if (sent != null)
            {
                var pa = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyA);
                var pb = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyB);
                if (pa != null)
                {
                    pa.Status = PartyStatus.Confirmed;
                    pa.HasSigned = true;
                    pa.SignedAt = DateTime.Now.AddDays(-1);
                    pa.UserCodeSign = "giamdoc@corp.vn";
                    pa.UserNameSign = "Giám đốc HTC";
                    pa.UserToken = "ut_party_a";
                    pa.SignDateUTC = DateTime.Now.AddDays(-1);
                    pa.SignBy = "seed";
                    pa.ContractFileVersion = sent.FileVersion;
                }
                if (pb != null)
                {
                    pb.UserToken = "ut_party_b";
                }
                await db.SaveChangesAsync();
                if (pa != null)
                {
                    db.Histories.Add(new ContractHistory
                    {
                        ContractId = sent.Id, Action = HistoryAction.PartySigned, Actor = pa.UserCodeSign ?? "seed",
                        Description = $"{pa.UserNameSign} ({Ui.Role(pa.Role)}) ký hợp đồng {sent.Code}",
                        At = DateTime.Now.AddDays(-1)
                    });
                    await db.SaveChangesAsync();
                }
            }

            // Cập nhật giá trị hợp đồng sau phê duyệt mẫu (Contract_ContractParty_UpdAfterApproved) —
            // minh họa bên đã có giá trị hợp đồng/đã thanh toán/còn lại.
            if (sent != null)
            {
                var pa = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyA);
                if (pa != null)
                {
                    pa.ValContract = sent.Value;
                    pa.ValPaymented = sent.Value * 0.4m;
                    pa.ValRemain = pa.ValContract - pa.ValPaymented;
                    pa.ValExchange = pa.ValContract * sent.CurrencyRate;
                    pa.ContractType = sent.Type?.Code;
                    pa.ContractTypeName = sent.Type?.Name;
                    pa.ValueUpdatedAt = DateTime.Now.AddHours(-2);
                    pa.ValueUpdatedBy = "seed";
                    await db.SaveChangesAsync();
                    db.Histories.Add(new ContractHistory
                    {
                        ContractId = sent.Id, Action = HistoryAction.PartyUpdAfterApproved, Actor = "seed",
                        Description = $"Cập nhật giá trị hợp đồng cho {pa.Name} ({Ui.Role(pa.Role)}): giá trị {pa.ValContract:N0} đ, đã thanh toán {pa.ValPaymented:N0} đ, còn lại {pa.ValRemain:N0} đ",
                        At = DateTime.Now.AddHours(-2)
                    });
                    await db.SaveChangesAsync();
                }
            }

            // Lịch sử gửi hợp đồng mẫu (Contract_SendHist) — minh họa gửi qua nhiều kênh.
            if (sent != null)
            {
                var pa = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyA);
                var pb = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyB);
                db.SendHistory.AddRange(
                    new ContractSendHist { ContractId = sent.Id, PartyId = pa?.Id, PartyName = pa?.Name ?? "", UserName = pa?.Name ?? "", UserToken = pa != null ? $"ut_{pa.Id}" : null, Channel = ChannelType.Email, Bulletin = BulletinType.Contract, InfoReceive = pa?.Email, SentAt = DateTime.Now.AddDays(-1), SentBy = "seed" },
                    new ContractSendHist { ContractId = sent.Id, PartyId = pb?.Id, PartyName = pb?.Name ?? "", UserName = pb?.Name ?? "", UserToken = pb != null ? $"ut_{pb.Id}" : null, Channel = ChannelType.Email, Bulletin = BulletinType.Contract, InfoReceive = pb?.Email, SentAt = DateTime.Now.AddDays(-1), SentBy = "seed" },
                    new ContractSendHist { ContractId = sent.Id, PartyId = pb?.Id, PartyName = pb?.Name ?? "", UserName = pb?.Name ?? "", UserToken = pb != null ? $"ut_{pb.Id}" : null, Channel = ChannelType.Sms, Bulletin = BulletinType.Otp, InfoReceive = pb?.Phone ?? "0900000000", SentAt = DateTime.Now.AddHours(-6), Remark = "Gửi OTP xác thực", SentBy = "seed" });
                await db.SaveChangesAsync();
            }

            // Mã OTP xác thực ký mẫu (Contract_ContractVerifyOtp) — minh họa mã còn hiệu lực + mã đã dùng.
            if (sent != null)
            {
                var pb = sent.Parties.FirstOrDefault(p => p.Role == PartyRole.PartyB);
                var pbCode = pb != null ? (string.IsNullOrWhiteSpace(pb.Email) ? pb.Name : pb.Email) : "";
                db.VerifyOtps.AddRange(
                    new ContractVerifyOtp { ContractId = sent.Id, ContractCode = sent.Code, OtpCode = "a1b2c3", UserCodeSign = pbCode, CreateDate = DateTime.Now.AddMinutes(-1), EndDate = DateTime.Now.AddMinutes(1), Active = true, CreatedBy = "seed" },
                    new ContractVerifyOtp { ContractId = sent.Id, ContractCode = sent.Code, OtpCode = "d4e5f6", UserCodeSign = pbCode, CreateDate = DateTime.Now.AddHours(-1), EndDate = DateTime.Now.AddMinutes(-58), Active = true, UsedAt = DateTime.Now.AddMinutes(-59), CreatedBy = "seed" });
                await db.SaveChangesAsync();
            }

            // Chi tiết hợp đồng mẫu (Contract_ContractDtl) — minh họa các dòng hàng hóa/dịch vụ + tính tiền.
            // Công thức (SignMulti.cshtml): Thành tiền = SL × Đơn giá; Chiết khấu = Thành tiền × CK%;
            // Thuế = (Thành tiền − Chiết khấu) × VAT%.
            if (sent != null)
            {
                ContractDetail D(int idx, string code, string name, string unit, decimal price, decimal qty, decimal vat, decimal disc, string? remark = null)
                {
                    var valContract = qty * price;
                    var valDiscount = valContract * disc / 100m;
                    var valTax = (valContract - valDiscount) * vat / 100m;
                    return new ContractDetail
                    {
                        ContractId = sent.Id, Idx = idx, SpecCode = code, SpecName = name, UnitName = unit,
                        UnitPrice = price, Qty = qty, VATRate = vat, DiscountRate = disc,
                        ValContract = valContract, ValDiscount = valDiscount, ValTax = valTax,
                        Remark = remark, CreatedBy = "seed"
                    };
                }
                db.Details.AddRange(
                    D(1, "SP-BT", "Bản thể hiện hợp đồng điện tử", "Bộ", 5_000_000, 10, 10, 0, "Gói triển khai"),
                    D(2, "SP-KY", "Dịch vụ ký số (CKS) theo hợp đồng", "HĐ", 2_000_000, 20, 8, 5, "Chiết khấu đại lý"),
                    D(3, "SP-OTP", "Gói OTP xác thực ký", "Gói", 500_000, 50, 10, 0));
                await db.SaveChangesAsync();
            }

            // Hủy hợp đồng bởi một bên mẫu (Contract_ContractParty_Cancel) — minh họa bên B hủy hợp đồng.
            var cancelled = new Contract
            {
                Code = $"HD{DateTime.Now:yyMM}-{all.Count + 1:D4}", Title = "Hợp đồng cung cấp thiết bị (đã bị Bên B hủy)",
                TypeId = types[0].Id, Body = "Bên B từ chối thực hiện hợp đồng do không đủ điều kiện.",
                Value = 80_000_000, Status = ContractStatus.Cancelled, CreatedBy = "seed",
                CreatedAt = DateTime.Now.AddDays(-5), SentAt = DateTime.Now.AddDays(-4)
            };
            cancelled.Parties.Add(new ContractParty { Name = "Công ty HTC", Role = PartyRole.PartyA, Email = "htc@corp.vn", SignOrder = 1 });
            cancelled.Parties.Add(new ContractParty
            {
                Name = "Công ty TNHH Bình Minh", Role = PartyRole.PartyB, Email = "bm@corp.vn", SignOrder = 2,
                Status = PartyStatus.Cancelled, CancelledAt = DateTime.Now.AddDays(-3), CancelledBy = "seed",
                Remark = "Không đủ điều kiện thực hiện"
            });
            db.Contracts.Add(cancelled);
            await db.SaveChangesAsync();
            db.Histories.Add(new ContractHistory
            {
                ContractId = cancelled.Id, Action = HistoryAction.PartyCancelled, Actor = "seed",
                Description = $"Công ty TNHH Bình Minh (Bên B) hủy hợp đồng {cancelled.Code} — Không đủ điều kiện thực hiện",
                At = DateTime.Now.AddDays(-3)
            });
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
        // Ô ký trên hợp đồng (Contract_ContractElement).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"Elements\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ContractId\" integer NOT NULL, \"PartyId\" integer NULL, \"ElementCode\" text NOT NULL DEFAULT '', \"ElementName\" text NOT NULL DEFAULT '', \"Type\" integer NOT NULL DEFAULT 0, \"PageIdx\" integer NOT NULL DEFAULT 1, \"ElementX\" double precision NOT NULL DEFAULT 0, \"ElementY\" double precision NOT NULL DEFAULT 0, \"ElementWidth\" double precision NOT NULL DEFAULT 0, \"ElementHeight\" double precision NOT NULL DEFAULT 0, \"IsSigned\" boolean NOT NULL DEFAULT false, \"SignerName\" text NULL, \"SignedAt\" timestamp NULL, \"SignFrom\" text NULL, \"ElementIP\" text NULL)");
        sql.Add("ALTER TABLE minicontract.\"Elements\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Người kiểm tra hợp đồng (Contract_Checker).
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"CheckerStatus\" integer NOT NULL DEFAULT 0");
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"Checkers\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ContractId\" integer NOT NULL, \"UserCode\" text NOT NULL DEFAULT '', \"UserName\" text NOT NULL DEFAULT '', \"Position\" text NULL, \"Idx\" integer NOT NULL DEFAULT 1, \"IsChecker\" boolean NOT NULL DEFAULT true, \"HasChecked\" boolean NOT NULL DEFAULT false, \"Sequential\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CheckedAt\" timestamp NULL)");
        sql.Add("ALTER TABLE minicontract.\"Checkers\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Lý do kết thúc hợp đồng (Mst_FinishedContractReason) + trường kết thúc trên hợp đồng.
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"FinishReasons\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL DEFAULT '', \"Name\" text NOT NULL DEFAULT '', \"Type\" integer NOT NULL DEFAULT 0, \"Description\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())");
        sql.Add("ALTER TABLE minicontract.\"FinishReasons\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"FinishReasonCode\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"FinishReasonName\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"FinishDescription\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"FinishedAt\" timestamp NULL");
        // Phê duyệt hợp đồng (Contract_Contract_Approved).
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"ApprovedAt\" timestamp NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"ApprovedBy\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Checkers\" ADD COLUMN IF NOT EXISTS \"ApprovedAt\" timestamp NULL");
        // Phân quyền hợp đồng (Contract_UserInContract).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"UserAssignments\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ContractId\" integer NOT NULL, \"UserCode\" text NOT NULL DEFAULT '', \"UserName\" text NOT NULL DEFAULT '', \"Email\" text NULL, \"AssignedAt\" timestamp NOT NULL DEFAULT now(), \"AssignedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"UserAssignments\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Lịch sử gửi hợp đồng (Contract_SendHist).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"SendHistory\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ContractId\" integer NOT NULL, \"PartyId\" integer NULL, \"PartyName\" text NOT NULL DEFAULT '', \"UserName\" text NOT NULL DEFAULT '', \"UserToken\" text NULL, \"Channel\" integer NOT NULL DEFAULT 0, \"Bulletin\" integer NOT NULL DEFAULT 0, \"InfoReceive\" text NULL, \"SentAt\" timestamp NOT NULL DEFAULT now(), \"Remark\" text NULL, \"SentBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"SendHistory\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Quy tắc đánh số hợp đồng theo loại (Mst_ContractTypeContractNo).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"NumberRules\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"TypeId\" integer NOT NULL, \"TypefixCode\" integer NOT NULL DEFAULT 0, \"TypefixInput\" text NOT NULL DEFAULT '', \"SeqNumberLength\" integer NOT NULL DEFAULT 4, \"NumberStart\" integer NOT NULL DEFAULT 1, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())");
        sql.Add("ALTER TABLE minicontract.\"NumberRules\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Hủy hợp đồng bởi một bên (Contract_ContractParty_Cancel).
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"Status\" integer NOT NULL DEFAULT 0");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"CancelledAt\" timestamp NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"CancelledBy\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"Remark\" text NULL");
        // Ký hợp đồng bởi một bên (Contract_Contract_PartySign).
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"UserCodeSign\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"UserNameSign\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"InfoToken\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"UserToken\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"SignDateUTC\" timestamp NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"SignBy\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"ContractFileVersion\" text NULL");
        // Mã OTP xác thực ký hợp đồng (Contract_ContractVerifyOtp).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"VerifyOtps\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ContractId\" integer NOT NULL, \"ContractCode\" text NOT NULL DEFAULT '', \"OtpCode\" text NOT NULL DEFAULT '', \"UserCodeSign\" text NOT NULL DEFAULT '', \"CreateDate\" timestamp NOT NULL DEFAULT now(), \"EndDate\" timestamp NOT NULL DEFAULT now(), \"Active\" boolean NOT NULL DEFAULT true, \"UsedAt\" timestamp NULL, \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"VerifyOtps\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Hợp đồng mẫu (Contract_TempContract).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"Templates\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL DEFAULT '', \"Name\" text NOT NULL DEFAULT '', \"TypeId\" integer NULL, \"Body\" text NOT NULL DEFAULT '', \"Remark\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"Templates\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"TemplateId\" integer NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"TemplateCode\" text NULL");
        // Nhóm hợp đồng mẫu (Contract_TempGroup) + thuộc tính (Contract_Attribute_Group).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"TemplateGroups\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL DEFAULT '', \"Name\" text NOT NULL DEFAULT '', \"Body\" text NULL, \"ContractName\" text NULL, \"Remark\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"TemplateGroups\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"AttributeGroups\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"GroupId\" integer NOT NULL, \"AttributeCode\" text NOT NULL DEFAULT '', \"AttributeValue\" text NOT NULL DEFAULT '', \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"AttributeGroups\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Chữ ký số của tổ chức (Mst_OrgCKS).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"OrgCertificates\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"CANumber\" text NOT NULL DEFAULT '', \"CAOrg\" text NULL, \"EffectiveFrom\" timestamp NULL, \"EffectiveTo\" timestamp NULL, \"CtsPath\" text NULL, \"CtsPwd\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '', \"UpdatedAt\" timestamp NULL, \"UpdatedBy\" text NULL)");
        sql.Add("ALTER TABLE minicontract.\"OrgCertificates\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_OrgCertificates_OrgId_CANumber\" ON minicontract.\"OrgCertificates\" (\"OrgId\", \"CANumber\")");
        // File hợp đồng (Contract_Contract_UpdateFilePath).
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"FileName\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"FilePath\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"FileVersion\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"FileUpdatedAt\" timestamp NULL");
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"FileUpdatedBy\" text NULL");
        // Cấu hình loại hợp đồng (Mst_ContractTypeDtl).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"TypeConfigs\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"TypeId\" integer NOT NULL, \"GenContractNo\" boolean NOT NULL DEFAULT true, \"EmailContract\" boolean NOT NULL DEFAULT true, \"SmsContract\" boolean NOT NULL DEFAULT false, \"ZaloContract\" boolean NOT NULL DEFAULT false, \"EmailOtp\" boolean NOT NULL DEFAULT true, \"SmsOtp\" boolean NOT NULL DEFAULT false, \"ZaloOtp\" boolean NOT NULL DEFAULT false, \"Active\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"TypeConfigs\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_TypeConfigs_OrgId_TypeId\" ON minicontract.\"TypeConfigs\" (\"OrgId\", \"TypeId\")");
        // Cấu hình ký của tổ chức (Mst_OrgSignConfig).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"SignConfigs\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"SignType\" integer NOT NULL DEFAULT 0, \"NetworkID\" text NULL, \"OrgCode\" text NULL, \"CANumber\" text NULL, \"CAOrg\" text NULL, \"EffectiveFrom\" timestamp NULL, \"EffectiveTo\" timestamp NULL, \"ServerSignFilePath\" text NULL, \"ServerSignPassword\" text NULL, \"SupplierCode\" text NULL, \"RemoteSignAgreementUUID\" text NULL, \"RemoteSignPassCode\" text NULL, \"AuthenCode\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '', \"UpdatedAt\" timestamp NULL, \"UpdatedBy\" text NULL)");
        sql.Add("ALTER TABLE minicontract.\"SignConfigs\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Cập nhật hợp đồng sau phê duyệt (Contract_ContractParty_UpdAfterApproved).
        sql.Add("ALTER TABLE minicontract.\"Contracts\" ADD COLUMN IF NOT EXISTS \"CurrencyRate\" numeric(18,4) NOT NULL DEFAULT 1");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"ValContract\" numeric(18,2) NOT NULL DEFAULT 0");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"ValPaymented\" numeric(18,2) NOT NULL DEFAULT 0");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"ValRemain\" numeric(18,2) NOT NULL DEFAULT 0");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"ValExchange\" numeric(18,2) NOT NULL DEFAULT 0");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"ContractType\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"ContractTypeName\" text NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"ValueUpdatedAt\" timestamp NULL");
        sql.Add("ALTER TABLE minicontract.\"Parties\" ADD COLUMN IF NOT EXISTS \"ValueUpdatedBy\" text NULL");
        // Người ký của hợp đồng (Contract_ContractUser).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"Signers\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ContractId\" integer NOT NULL, \"PartyId\" integer NULL, \"PartyCode\" text NOT NULL DEFAULT '', \"UserCodeSysSign\" text NOT NULL DEFAULT '', \"Idx\" integer NOT NULL DEFAULT 1, \"UserCodeSign\" text NOT NULL DEFAULT '', \"UserNameSign\" text NOT NULL DEFAULT '', \"UserEmail\" text NULL, \"UserPhone\" text NULL, \"UserZalo\" text NULL, \"UserToken\" text NULL, \"SignStatus\" integer NOT NULL DEFAULT 1, \"ConfirmDTimeUTC\" timestamp NULL, \"ConfirmBy\" text NULL, \"FlagSendUser\" boolean NOT NULL DEFAULT false, \"SendDateUTC\" timestamp NULL, \"SendBy\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"Signers\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Cấu hình kênh gửi của tổ chức (Mst_Channel + Mst_ChannelEmail/SMS/Zalo).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"ChannelConfigs\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"NetworkID\" text NULL, \"ContractChannel\" integer NOT NULL DEFAULT 0, \"OtpChannel\" integer NOT NULL DEFAULT 0, \"AccessKeyChannel\" integer NOT NULL DEFAULT 0, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"ChannelConfigs\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ChannelConfigs_OrgId\" ON minicontract.\"ChannelConfigs\" (\"OrgId\")");
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"ChannelEmails\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ChannelConfigId\" integer NOT NULL, \"SubFormCodeEmailContract\" text NULL, \"SubFormCodeEmailOtp\" text NULL, \"SubFormCodeEmailAccessKey\" text NULL, \"MailFrom\" text NULL, \"APIsSendMail\" text NULL, \"ApiKeySendMail\" text NULL, \"SolutionCodeSendMail\" text NULL, \"DisplayNameMailFrom\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"ChannelEmails\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"ChannelSms\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ChannelConfigId\" integer NOT NULL, \"SubFormCodeContractSms\" text NULL, \"SubFormCodeSmsOtp\" text NULL, \"SubFormCodeSmsAccessKey\" text NULL, \"SmsBrandName\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"ChannelSms\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"ChannelZalo\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ChannelConfigId\" integer NOT NULL, \"SubFormCodeContractZaloUserId\" text NULL, \"SubFormCodeContractPhone\" text NULL, \"SubFormCodeOtp\" text NULL, \"SubFormCodeAccessKeyZaloUserId\" text NULL, \"SubFormCodeAccessKeyPhone\" text NULL, \"AppId\" text NULL, \"ZaloOaId\" text NULL, \"RefreshToken\" text NULL, \"AccessToken\" text NULL, \"AppSecret\" text NULL, \"AccessCode\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"ChannelZalo\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Mẫu nội dung gửi (Mst_SubmissionForm + Mst_SubmissionFormMessage + Mst_SubmissionFormZNS).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"SubmissionForms\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"SubFormCode\" text NOT NULL DEFAULT '', \"SubFormName\" text NOT NULL DEFAULT '', \"ChannelType\" integer NOT NULL DEFAULT 0, \"BulletinType\" integer NOT NULL DEFAULT 0, \"IdZns\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"SubmissionForms\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_SubmissionForms_OrgId_SubFormCode\" ON minicontract.\"SubmissionForms\" (\"OrgId\", \"SubFormCode\")");
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"SubmissionFormMessages\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"SubmissionFormId\" integer NOT NULL, \"SubFormCode\" text NOT NULL DEFAULT '', \"SubTitle\" text NULL, \"Message\" text NOT NULL DEFAULT '', \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"SubmissionFormMessages\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"SubmissionFormZns\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"SubmissionFormId\" integer NOT NULL, \"SubFormCode\" text NOT NULL DEFAULT '', \"ParamContractCodeZns\" text NOT NULL DEFAULT '', \"SourceDataType\" text NULL, \"ParamContractCode\" text NOT NULL DEFAULT '', \"ParamValue\" text NULL, \"Active\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"SubmissionFormZns\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        // Chi tiết hợp đồng (Contract_ContractDtl).
        sql.Add("CREATE TABLE IF NOT EXISTS minicontract.\"Details\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ContractId\" integer NOT NULL, \"Idx\" integer NOT NULL DEFAULT 1, \"SpecCode\" text NOT NULL DEFAULT '', \"SpecName\" text NOT NULL DEFAULT '', \"VATRateCode\" text NULL, \"VATRate\" numeric(18,2) NOT NULL DEFAULT 0, \"UnitCode\" text NULL, \"UnitName\" text NULL, \"UnitPrice\" numeric(18,2) NOT NULL DEFAULT 0, \"Qty\" numeric(18,2) NOT NULL DEFAULT 0, \"ValContract\" numeric(18,2) NOT NULL DEFAULT 0, \"ValTax\" numeric(18,2) NOT NULL DEFAULT 0, \"DiscountRate\" numeric(18,2) NOT NULL DEFAULT 0, \"ValDiscount\" numeric(18,2) NOT NULL DEFAULT 0, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '')");
        sql.Add("ALTER TABLE minicontract.\"Details\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '" + def + "'");
        foreach (var s in sql)
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
