using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models;

namespace MiniContract.Services;

public record ContractDash(int Total, int Draft, int AwaitingSign, int Completed, decimal TotalValue,
    List<(string Status, int Count)> ByStatus);

/// <summary>Thống kê ô ký theo loại — port từ Contract_ContractElementSum (QContract).</summary>
public record ElementStats(int Total, int Signed, int Electronic, int Short, int Digital);

/// <summary>Thống kê người kiểm tra — port từ Contract_Checker (QContract).</summary>
public record CheckerStats(int Total, int Checked, int Pending, CheckerStatus Status);

/// <summary>Thống kê phê duyệt hợp đồng — port từ Contract_Contract_Approved (QContract).</summary>
public record ApproveStats(int Checkers, int Approved, bool IsApproved, string? ApprovedBy, DateTime? ApprovedAt);

/// <summary>Thống kê người ký của hợp đồng — port từ Contract_ContractUser (QContract).</summary>
public record SignerStats(int Total, int Confirmed, int Pending, int Sent);

/// <summary>Thống kê ký hợp đồng theo bên — port từ Contract_Contract_PartySign (QContract).</summary>
public record PartySignStats(int Total, int Confirmed, int Pending, bool AllConfirmed);

/// <summary>Thống kê chi tiết hợp đồng — port từ Contract_ContractDtl (QContract).</summary>
public record DetailStats(int Lines, decimal Total, decimal Tax, decimal Discount, decimal GrandTotal);

/// <summary>Thống kê thông tin bên tham gia — port từ Contract_ContractParty (QContract).</summary>
public record PartyInfoStats(int Total, int WithInfo, int EmailSent);

/// <summary>Thống kê file đính kèm hợp đồng — port từ Contract_ContractFiles (QContract).</summary>
public record AttachmentStats(int Total, int Public, int Internal, int WithFile);

/// <summary>
/// Một dòng báo cáo tổng hợp hợp đồng theo kỳ — port từ Rpt_ContractForDashboard (QContract).
/// ReportType: ContractCreateDate (tạo trong ngày), ContractPending (chờ ký),
/// ContractDateInDay/Week/Month/Year (HĐ theo ngày/tuần/tháng/năm).
/// </summary>
public record DashboardReportRow(string ReportType, string Label, int Qty, decimal TotalValExchange);

/// <summary>
/// Báo cáo tổng hợp hợp đồng cho dashboard — port từ Rpt_ContractForDashboard (QContract).
/// Gồm số HĐ tạo trong ngày, số HĐ chờ ký, và số HĐ + tổng giá trị quy đổi theo ngày/tuần/tháng/năm.
/// </summary>
public record DashboardReport(List<DashboardReportRow> Rows, DateTime From, DateTime To)
{
    public DashboardReportRow? Row(string reportType) => Rows.FirstOrDefault(r => r.ReportType == reportType);
    public int Qty(string reportType) => Row(reportType)?.Qty ?? 0;
    public decimal Value(string reportType) => Row(reportType)?.TotalValExchange ?? 0;
}

public interface IContractService
{
    Task<List<Contract>> ListAsync(ContractStatus? status, string? q);
    Task<Contract?> GetAsync(int id);
    Task<List<ContractType>> TypesAsync();

    // ── Danh mục loại hợp đồng (Mst_ContractType) ────────────────────
    Task<List<ContractType>> ContractTypesAsync(bool activeOnly = false);
    Task<ContractType> SaveContractTypeAsync(ContractType type, string actor);
    Task<(bool ok, string msg)> DeleteContractTypeAsync(int id, string actor);
    Task<int> CreateAsync(Contract c, List<ContractParty> parties);
    Task<int> CreateAnnexAsync(int parentId, Contract annex, List<ContractParty> parties);
    Task<List<Contract>> AnnexesAsync(int parentId);
    Task SendAsync(int id);
    Task CancelAsync(int id);
    Task<(bool ok, string msg)> SignCksAsync(int contractId, int partyId);
    string OtpGenerate(int partyId);
    Task<(bool ok, string msg)> SignOtpAsync(int contractId, int partyId, string code);
    Task<List<ContractHistory>> HistoryAsync(int contractId);
    Task AddRemarkAsync(int contractId, string actor, string remark);
    Task<ContractDash> DashboardAsync();

    // ── Báo cáo tổng hợp hợp đồng (Rpt_ContractForDashboard) ─────────
    Task<DashboardReport> DashboardReportAsync(DateTime? from = null, DateTime? to = null);

    // ── Link ký công khai (Contract_ContractSignLink) ────────────────
    Task<ContractSignLink> CreateSignLinkAsync(int contractId, int partyId, int validHours, string actor);
    Task<List<ContractSignLink>> SignLinksAsync(int contractId);
    Task RevokeSignLinkAsync(int linkId, string actor);
    Task<ContractSignLink?> ResolveSignLinkAsync(string token);
    Task<(bool ok, string msg)> SignViaLinkAsync(string token, string? signerName);

    // ── Ô ký trên hợp đồng (Contract_ContractElement) ────────────────
    Task<List<ContractElement>> ElementsAsync(int contractId);
    Task<ContractElement> AddElementAsync(int contractId, ContractElement el);
    Task<(bool ok, string msg)> SignElementAsync(int elementId, string signerName, string signFrom, string? ip);
    Task<ElementStats> ElementStatsAsync(int contractId);

    // ── Người kiểm tra hợp đồng (Contract_Checker) ───────────────────
    Task<List<ContractChecker>> CheckersAsync(int contractId);
    Task<ContractChecker> AddCheckerAsync(int contractId, ContractChecker checker);
    Task<(bool ok, string msg)> AcceptCheckAsync(int contractId, int checkerId, string? remark);
    Task<CheckerStats> CheckerStatsAsync(int contractId);

    // ── Phê duyệt hợp đồng (Contract_Contract_Approved) ──────────────
    Task<(bool ok, string msg)> ApproveAsync(int contractId, string userCode, string? remark);
    Task<ApproveStats> ApproveStatsAsync(int contractId);

    // ── Lý do kết thúc hợp đồng (Mst_FinishedContractReason) ─────────
    Task<List<FinishedContractReason>> FinishReasonsAsync(bool activeOnly = false);
    Task<FinishedContractReason> AddFinishReasonAsync(FinishedContractReason reason);
    Task<(bool ok, string msg)> FinishContractAsync(int contractId, int reasonId, string? description, string actor);

    // ── Phân quyền hợp đồng (Contract_UserInContract) ────────────────
    Task<List<ContractUserInContract>> UserAssignmentsAsync(int contractId);
    Task<(bool ok, string msg)> SaveUserAssignmentsAsync(int contractId, List<ContractUserInContract> users, string actor);

    // ── Người ký của hợp đồng (Contract_ContractUser) ────────────────
    Task<List<ContractSigner>> SignersAsync(int contractId);
    Task<ContractSigner> AddSignerAsync(int contractId, ContractSigner signer, string actor);
    Task<(bool ok, string msg)> ConfirmSignerAsync(int contractId, int signerId, string actor);
    Task<(bool ok, string msg)> MarkSignerSentAsync(int contractId, int signerId, string actor);
    Task<SignerStats> SignerStatsAsync(int contractId);

    // ── Ký hợp đồng bởi một bên (Contract_Contract_PartySign) ────────
    Task<(bool ok, string msg)> PartySignAsync(int contractId, int partyId, string? userCodeSign,
        string? userNameSign, string? userToken, string? otpCode, string? fileVersion, string actor);
    Task<PartySignStats> PartySignStatsAsync(int contractId);

    // ── Lịch sử gửi hợp đồng (Contract_SendHist) ─────────────────────
    Task<List<ContractSendHist>> SendHistoryAsync(int contractId, BulletinType? bulletin = null);
    Task<ContractSendHist> AddSendHistAsync(int contractId, int? partyId, ChannelType channel, BulletinType bulletin, string? infoReceive, string? remark, string actor);
    Task<(bool ok, string msg)> ResendAsync(int contractId, List<int> sendHistIds, string actor);

    // ── Quy tắc đánh số hợp đồng theo loại (Mst_ContractTypeContractNo) ──
    Task<List<ContractNumberRule>> NumberRulesAsync();
    Task<ContractNumberRule> SaveNumberRuleAsync(ContractNumberRule rule);
    Task<List<string>> PreviewNumbersAsync(int typeId, int amount);

    // ── Hủy hợp đồng bởi một bên (Contract_ContractParty_Cancel) ─────
    Task<(bool ok, string msg)> CancelByPartyAsync(int contractId, int partyId, string? remark, string actor);

    // ── Cập nhật ghi chú của một bên (Contract_Contract_Party_UpdateRemark) ──
    Task<(bool ok, string msg)> UpdatePartyRemarkAsync(int contractId, int partyId, string? remark, string actor);

    // ── Cập nhật hợp đồng sau phê duyệt (Contract_ContractParty_UpdAfterApproved) ──
    Task<(bool ok, string msg)> UpdateAfterApprovedAsync(int contractId, int partyId, decimal valContract,
        decimal valPaymented, string? contractType, string? contractTypeName, string? remark, string actor);

    // ── Cập nhật thông tin bên tham gia (Contract_ContractParty_Update) ──
    Task<(bool ok, string msg)> UpdatePartyAsync(int contractId, int partyId, ContractParty info, string actor);
    Task<PartyInfoStats> PartyInfoStatsAsync(int contractId);

    // ── Ghi nhận gửi email cho bên (Contract_ContractParty_UpdEmailSend) ──
    Task<(bool ok, string msg)> MarkPartyEmailSentAsync(int contractId, int partyId, string? emailSend, string actor);

    // ── Mã OTP xác thực ký hợp đồng (Contract_ContractVerifyOtp) ─────
    Task<List<ContractVerifyOtp>> VerifyOtpsAsync(int contractId);
    Task<ContractVerifyOtp> GenerateVerifyOtpAsync(int contractId, int partyId, int validMinutes, string actor);
    Task<(bool ok, string msg)> VerifyOtpAsync(int contractId, string otpCode, string userCodeSign);

    // ── Hợp đồng mẫu (Contract_TempContract) ─────────────────────────
    Task<List<ContractTemplate>> TemplatesAsync(bool activeOnly = false);
    Task<ContractTemplate> SaveTemplateAsync(ContractTemplate template, string actor);
    Task<(bool ok, string msg)> DeleteTemplateAsync(int templateId, string actor);
    Task<int> CreateFromTemplateAsync(int templateId, string title, decimal value, string? body,
        List<ContractParty> parties, string actor);

    // ── Nhóm hợp đồng mẫu (Contract_TempGroup) ───────────────────────
    Task<List<ContractTemplateGroup>> TemplateGroupsAsync(bool activeOnly = false);
    Task<ContractTemplateGroup?> TemplateGroupAsync(int id);
    Task<ContractTemplateGroup> SaveTemplateGroupAsync(ContractTemplateGroup group,
        List<ContractAttributeGroup> attributes, string actor);
    Task<(bool ok, string msg)> DeleteTemplateGroupAsync(int groupId, string actor);

    // ── Chữ ký số của tổ chức (Mst_OrgCKS) ────────────────────────────
    Task<List<OrgCertificate>> CertificatesAsync(bool activeOnly = false);
    Task<OrgCertificate> SaveCertificateAsync(OrgCertificate cert, string actor);
    Task<(bool ok, string msg)> DeleteCertificateAsync(int id, string actor);

    // ── Cấu hình ký của tổ chức (Mst_OrgSignConfig) ──────────────────
    Task<List<OrgSignConfig>> SignConfigsAsync(bool activeOnly = false);
    Task<OrgSignConfig> SaveSignConfigAsync(OrgSignConfig config, string actor);
    Task<(bool ok, string msg)> DeleteSignConfigAsync(int id, string actor);

    // ── File hợp đồng (Contract_Contract_UpdateFilePath) ─────────────
    Task<(bool ok, string msg)> UpdateFileAsync(int contractId, string fileName, string? filePath, string? fileVersion, string actor);

    // ── Cấu hình loại hợp đồng (Mst_ContractTypeDtl) ─────────────────
    Task<List<ContractTypeConfig>> TypeConfigsAsync();
    Task<ContractTypeConfig> SaveTypeConfigAsync(ContractTypeConfig config, string actor);
    Task<(bool ok, string msg)> DeleteTypeConfigAsync(int id, string actor);

    // ── Cấu hình kênh gửi của tổ chức (Mst_Channel) ──────────────────
    Task<ChannelConfig?> ChannelConfigAsync();
    Task<ChannelConfig> SaveChannelConfigAsync(ChannelConfig config, string actor);
    Task<(bool ok, string msg)> DeleteChannelConfigAsync(int id, string actor);

    // ── Mẫu nội dung gửi (Mst_SubmissionForm) ────────────────────────
    Task<List<SubmissionForm>> SubmissionFormsAsync(bool activeOnly = false);
    Task<SubmissionForm?> SubmissionFormAsync(int id);
    Task<SubmissionForm> SaveSubmissionFormAsync(SubmissionForm form,
        List<SubmissionFormMessage> messages, List<SubmissionFormZns> znsParams, string actor);
    Task<(bool ok, string msg)> DeleteSubmissionFormAsync(int id, string actor);

    // ── Chi tiết hợp đồng (Contract_ContractDtl) ─────────────────────
    Task<List<ContractDetail>> DetailsAsync(int contractId);
    Task<ContractDetail> AddDetailAsync(int contractId, ContractDetail detail, string actor);
    Task<(bool ok, string msg)> DeleteDetailAsync(int contractId, int detailId, string actor);
    Task<DetailStats> DetailStatsAsync(int contractId);

    // ── Trường động của hợp đồng (Contract_Attribute_Contract) ───────
    Task<List<AttributeContract>> AttributeMastersAsync(bool activeOnly = false);
    Task<AttributeContract> SaveAttributeMasterAsync(AttributeContract master, string actor);
    Task<(bool ok, string msg)> DeleteAttributeMasterAsync(int id, string actor);
    Task<List<ContractAttribute>> AttributesAsync(int contractId);
    Task<(bool ok, string msg)> SaveAttributesAsync(int contractId, List<ContractAttribute> attributes, string actor);
    Task<List<ContractAttributeDtl>> AttributeDetailsAsync(int contractId);
    Task<(bool ok, string msg)> SaveAttributeDetailsAsync(int contractId, List<ContractAttributeDtl> attributes, string actor);

    // ── Quản lý thông báo (Mst_NotifyType + Map_UserInNotifyType) ────
    Task<List<NotifyType>> NotifyTypesAsync(bool activeOnly = false);
    Task<NotifyType> SaveNotifyTypeAsync(NotifyType type, string actor);
    Task<(bool ok, string msg)> DeleteNotifyTypeAsync(int id, string actor);
    Task<List<UserNotifyType>> UserNotifyTypesAsync(string? userCode = null);
    Task<(bool ok, string msg)> SaveUserNotifyTypesAsync(List<UserNotifyType> mappings, string actor);

    // ── Danh mục loại mẫu in (Mst_TempType) ──────────────────────────
    Task<List<TempType>> TempTypesAsync(bool activeOnly = false);
    Task<TempType> SaveTempTypeAsync(TempType type, string actor);
    Task<(bool ok, string msg)> DeleteTempTypeAsync(int id, string actor);

    // ── Tỷ giá ngoại tệ (Mst_CurrencyEx) ─────────────────────────────
    Task<List<CurrencyExchange>> CurrenciesAsync();
    Task<CurrencyExchange> SaveCurrencyAsync(CurrencyExchange currency, string actor);
    Task<(bool ok, string msg)> DeleteCurrencyAsync(int id, string actor);

    // ── Tham số hệ thống (Mst_Param) ─────────────────
    Task<List<SystemParam>> SystemParamsAsync();
    Task<SystemParam> SaveSystemParamAsync(SystemParam param, string actor);
    Task<(bool ok, string msg)> DeleteSystemParamAsync(int id, string actor);

    // ── Tham số riêng (Mst_ParamPrivate) ─────────────
    Task<List<PrivateParam>> PrivateParamsAsync();
    Task<PrivateParam> SavePrivateParamAsync(PrivateParam param, string actor);
    Task<(bool ok, string msg)> DeletePrivateParamAsync(int id, string actor);

    // ── File đính kèm hợp đồng (Contract_ContractFiles) ──────────────
    Task<List<ContractAttachment>> AttachmentsAsync(int contractId);
    Task<(bool ok, string msg)> AddAttachmentAsync(int contractId, ContractAttachment attachment, string actor);
    Task<(bool ok, string msg)> DeleteAttachmentAsync(int id, string actor);
    Task<AttachmentStats> AttachmentStatsAsync(int contractId);

    // ── Danh mục địa chỉ (Mst_Country / Mst_Province / Mst_District / Mst_Ward) ──
    Task<List<Country>> CountriesAsync(bool activeOnly = false);
    Task<Country> SaveCountryAsync(Country country, string actor);
    Task<(bool ok, string msg)> DeleteCountryAsync(int id, string actor);
    Task<List<Province>> ProvincesAsync(bool activeOnly = false);
    Task<Province> SaveProvinceAsync(Province province, string actor);
    Task<(bool ok, string msg)> DeleteProvinceAsync(int id, string actor);
    Task<List<District>> DistrictsAsync(string? provinceCode = null, bool activeOnly = false);
    Task<District> SaveDistrictAsync(District district, string actor);
    Task<(bool ok, string msg)> DeleteDistrictAsync(int id, string actor);
    Task<List<Ward>> WardsAsync(string? districtCode = null, bool activeOnly = false);
    Task<Ward> SaveWardAsync(Ward ward, string actor);
    Task<(bool ok, string msg)> DeleteWardAsync(int id, string actor);
}

public class ContractService(AppDbContext db, ISignatureService signer, OtpService otp) : IContractService
{
    public async Task<List<Contract>> ListAsync(ContractStatus? status, string? q)
    {
        var query = db.Contracts.Include(c => c.Type).Include(c => c.Parties).AsQueryable();
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(c => c.Title.Contains(q) || c.Code.Contains(q));
        var list = await query.ToListAsync();
        return list.OrderByDescending(c => c.CreatedAt).ToList();
    }

    public Task<Contract?> GetAsync(int id) =>
        db.Contracts.Include(c => c.Type).Include(c => c.Parties).Include(c => c.Signatures)
          .FirstOrDefaultAsync(c => c.Id == id);

    public Task<List<ContractType>> TypesAsync() => db.ContractTypes.OrderBy(t => t.Name).ToListAsync();

    // ── Danh mục loại hợp đồng (Mst_ContractType) ────────────────────
    // Nguồn QContract: Mst_ContractType_CheckDB / _CreateX / _UpdateX / _DeleteX (Master.cs).
    // Kèm số hợp đồng mẫu + số hợp đồng đang dùng loại này để biết loại có đang được sử dụng không.
    public async Task<List<ContractType>> ContractTypesAsync(bool activeOnly = false)
    {
        var q = db.ContractTypes.AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        var list = await q.OrderBy(x => x.Code).ThenBy(x => x.Name).ToListAsync();

        // Đếm số hợp đồng mẫu + số hợp đồng đang dùng từng loại (để chặn xóa loại đang dùng).
        var templateCounts = await db.Templates.Where(t => t.TypeId != null)
            .GroupBy(t => t.TypeId!.Value).Select(g => new { TypeId = g.Key, Count = g.Count() }).ToListAsync();
        var contractCounts = await db.Contracts.Where(c => c.TypeId != null)
            .GroupBy(c => c.TypeId!.Value).Select(g => new { TypeId = g.Key, Count = g.Count() }).ToListAsync();
        var tMap = templateCounts.ToDictionary(x => x.TypeId, x => x.Count);
        var cMap = contractCounts.ToDictionary(x => x.TypeId, x => x.Count);
        foreach (var t in list)
        {
            t.TemplateCount = tMap.TryGetValue(t.Id, out var tc) ? tc : 0;
            t.ContractCount = cMap.TryGetValue(t.Id, out var cc) ? cc : 0;
        }
        return list;
    }

    // Lưu loại hợp đồng — tạo mới (ContractType không trùng) hoặc cập nhật từng phần khi đã tồn tại.
    // Luật cốt lõi: ContractType bắt buộc & KHÔNG trùng khi tạo (FlagExistToCheck=No); khi sửa phải
    // tồn tại (FlagExistToCheck=Yes); ContractTypeName bắt buộc.
    public async Task<ContractType> SaveContractTypeAsync(ContractType type, string actor)
    {
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var code = (type.Code ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Cần mã loại hợp đồng (ContractType).");
        if (string.IsNullOrWhiteSpace(type.Name))
            throw new InvalidOperationException("Cần tên loại hợp đồng (ContractTypeName).");

        var existing = await db.ContractTypes.FirstOrDefaultAsync(x => x.Code == code);
        if (existing == null)
        {
            // Tạo mới — ContractType không được trùng (FlagExistToCheck=No).
            type.Code = code;
            type.Name = type.Name.Trim();
            type.Description = string.IsNullOrWhiteSpace(type.Description) ? null : type.Description.Trim();
            type.CreatedBy = who;
            db.ContractTypes.Add(type);
            await db.SaveChangesAsync();
            return type;
        }

        // Cập nhật từng phần (FlagExistToCheck=Yes).
        existing.Name = type.Name.Trim();
        existing.Description = string.IsNullOrWhiteSpace(type.Description) ? null : type.Description.Trim();
        existing.BodyTemplate = type.BodyTemplate;
        existing.Active = type.Active;
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa loại hợp đồng — phải tồn tại (FlagExistToCheck=Yes) và KHÔNG đang được sử dụng.
    // Nguồn QContract: Mst_ContractType_DeleteX — chặn xóa khi loại đang được hợp đồng mẫu
    // (Contract_TempContract) hoặc bên hợp đồng (Contract_ContractParty) dùng (ContractTypeUsed).
    public async Task<(bool ok, string msg)> DeleteContractTypeAsync(int id, string actor)
    {
        var t = await db.ContractTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return (false, "Không tìm thấy loại hợp đồng.");

        var templateCount = await db.Templates.CountAsync(x => x.TypeId == id);
        var contractCount = await db.Contracts.CountAsync(x => x.TypeId == id);
        if (templateCount > 0 || contractCount > 0)
            return (false, $"Loại '{t.Name}' đang được sử dụng ({templateCount} hợp đồng mẫu, {contractCount} hợp đồng) — không thể xóa.");

        db.ContractTypes.Remove(t);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa loại hợp đồng '{t.Name}'.");
    }

    public async Task<int> CreateAsync(Contract c, List<ContractParty> parties)
    {
        c.Code = await NextCodeAsync(c.TypeId);
        c.Status = ContractStatus.Draft;
        int order = 1;
        foreach (var p in parties.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            p.SignOrder = order++;
            c.Parties.Add(p);
        }
        db.Contracts.Add(c);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Created, c.CreatedBy, $"Tạo {c.Kind.ToLower()} {c.Code}");
        return c.Id;
    }

    // Tạo phụ lục cho 1 hợp đồng gốc. Nguồn QContract: FlagContractAnnex=1 + ContractRefNo = số HĐ cha.
    public async Task<int> CreateAnnexAsync(int parentId, Contract annex, List<ContractParty> parties)
    {
        var parent = await db.Contracts.FirstOrDefaultAsync(x => x.Id == parentId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng gốc.");
        if (parent.IsAnnex) throw new InvalidOperationException("Không thể tạo phụ lục của một phụ lục.");
        if (parent.Status == ContractStatus.Cancelled) throw new InvalidOperationException("Hợp đồng gốc đã hủy, không tạo phụ lục.");

        var count = await db.Contracts.CountAsync();
        annex.Code = $"PL{DateTime.Now:yyMM}-{count + 1:D4}";
        annex.Status = ContractStatus.Draft;
        annex.IsAnnex = true;
        annex.ParentContractId = parent.Id;
        annex.ParentContractCode = parent.Code;
        int order = 1;
        foreach (var p in parties.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            p.SignOrder = order++;
            annex.Parties.Add(p);
        }
        db.Contracts.Add(annex);
        await db.SaveChangesAsync();
        await LogAsync(annex.Id, HistoryAction.Created, annex.CreatedBy, $"Tạo phụ lục {annex.Code} cho hợp đồng {parent.Code}");
        await LogAsync(parent.Id, HistoryAction.Remark, annex.CreatedBy, $"Phát sinh phụ lục {annex.Code}");
        return annex.Id;
    }

    public Task<List<Contract>> AnnexesAsync(int parentId) =>
        db.Contracts.Include(c => c.Parties).Where(c => c.ParentContractId == parentId)
          .OrderByDescending(c => c.CreatedAt).ToListAsync();

    public async Task SendAsync(int id)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        if (c.Status != ContractStatus.Draft) throw new InvalidOperationException("Chỉ gửi ký hợp đồng ở trạng thái Nháp.");
        if (c.Parties.Count == 0) throw new InvalidOperationException("Hợp đồng chưa có bên tham gia.");
        c.Status = ContractStatus.Sent;
        c.SentAt = DateTime.Now;
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Sent, c.CreatedBy, $"Gửi {c.Kind.ToLower()} {c.Code} cho {c.Parties.Count} bên ký");
    }

    public async Task CancelAsync(int id)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        if (c.Status == ContractStatus.Completed) throw new InvalidOperationException("Không hủy hợp đồng đã hoàn tất.");
        c.Status = ContractStatus.Cancelled;
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Cancelled, c.CreatedBy, $"Hủy {c.Kind.ToLower()} {c.Code}");
    }

    public async Task<(bool ok, string msg)> SignCksAsync(int contractId, int partyId)
    {
        var (c, p, err) = await LoadForSign(contractId, partyId);
        if (err != null) return (false, err);

        var (sigValue, certSubject) = signer.SignContract(c!.Id, c.Title, c.Body, p!.Name);
        db.Signatures.Add(new ContractSignature
        {
            ContractId = c.Id, PartyId = p.Id, Method = SignMethod.DigitalCertificate,
            SignerName = p.Name, CertSubject = certSubject, SignatureValue = sigValue
        });
        MarkSigned(c, p);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Signed, p.Name, $"{p.Name} ký số (CKS) — {Ui.Role(p.Role)}");
        if (c.Status == ContractStatus.Completed)
            await LogAsync(c.Id, HistoryAction.Completed, "system", $"Đủ chữ ký các bên — {c.Code} hoàn tất");
        return (true, $"{p.Name} đã ký số (CKS) hợp đồng {c.Code}.");
    }

    public string OtpGenerate(int partyId) => otp.Generate(partyId);

    public async Task<(bool ok, string msg)> SignOtpAsync(int contractId, int partyId, string code)
    {
        var (c, p, err) = await LoadForSign(contractId, partyId);
        if (err != null) return (false, err);
        if (!otp.Verify(partyId, code)) return (false, "Mã OTP không đúng hoặc đã hết hạn.");

        db.Signatures.Add(new ContractSignature
        {
            ContractId = c!.Id, PartyId = p!.Id, Method = SignMethod.Otp,
            SignerName = p.Name, SignatureValue = $"OTP-VERIFIED-{DateTime.UtcNow:yyyyMMddHHmmss}"
        });
        MarkSigned(c, p);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Signed, p.Name, $"{p.Name} ký qua OTP — {Ui.Role(p.Role)}");
        if (c.Status == ContractStatus.Completed)
            await LogAsync(c.Id, HistoryAction.Completed, "system", $"Đủ chữ ký các bên — {c.Code} hoàn tất");
        return (true, $"{p.Name} đã ký qua OTP hợp đồng {c.Code}.");
    }

    public Task<List<ContractHistory>> HistoryAsync(int contractId) =>
        db.Histories.Where(h => h.ContractId == contractId).OrderByDescending(h => h.At).ToListAsync();

    public async Task AddRemarkAsync(int contractId, string actor, string remark)
    {
        if (string.IsNullOrWhiteSpace(remark)) return;
        await LogAsync(contractId, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor, remark.Trim());
    }

    public async Task<ContractDash> DashboardAsync()
    {
        var all = await db.Contracts.ToListAsync();
        var byStatus = all.GroupBy(c => c.Status).Select(g => (g.Key.ToString(), g.Count())).ToList();
        return new ContractDash(
            all.Count,
            all.Count(c => c.Status == ContractStatus.Draft),
            all.Count(c => c.Status is ContractStatus.Sent or ContractStatus.PartiallySigned),
            all.Count(c => c.Status == ContractStatus.Completed),
            all.Where(c => c.Status == ContractStatus.Completed).Sum(c => c.Value),
            byStatus);
    }

    // ── Báo cáo tổng hợp hợp đồng (Rpt_ContractForDashboard) ─────────
    // Nguồn QContract: Rpt_ContractForDashboardX (Report.cs) — gom số HĐ + tổng giá trị quy đổi
    // theo các kỳ: tạo trong ngày (ContractCreateDate), chờ ký (ContractPending),
    // theo ngày/tuần/tháng/năm (ContractDateInDay/Week/Month/Year).
    // Giá trị quy đổi lấy từ tổng ValExchange của các bên (Contract_ContractParty).
    public async Task<DashboardReport> DashboardReportAsync(DateTime? from = null, DateTime? to = null)
    {
        var fromDate = (from ?? DateTime.Today).Date;
        var toDate = (to ?? DateTime.Today).Date;
        if (toDate < fromDate) toDate = fromDate;

        var contracts = await db.Contracts.Include(c => c.Parties).ToListAsync();

        // Tổng giá trị quy đổi của 1 hợp đồng = tổng ValExchange các bên (nếu chưa có thì lấy Value).
        decimal ValExchange(Contract c)
        {
            var sum = c.Parties.Sum(p => p.ValExchange);
            return sum > 0 ? sum : c.Value;
        }

        // HĐ có ngày tạo nằm trong khoảng [from, to].
        bool InRange(DateTime d) => d.Date >= fromDate && d.Date <= toDate;

        // Tuần hiện tại (thứ 2 → chủ nhật) chứa ngày `to`.
        var weekStart = toDate.AddDays(-(((int)toDate.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(6);

        var createdToday = contracts.Where(c => c.CreatedAt.Date == toDate).ToList();
        var pending = contracts.Where(c => c.Status is ContractStatus.Sent or ContractStatus.PartiallySigned).ToList();
        var inDay = contracts.Where(c => InRange(c.CreatedAt)).ToList();
        var inWeek = contracts.Where(c => c.CreatedAt.Date >= weekStart && c.CreatedAt.Date <= weekEnd).ToList();
        var inMonth = contracts.Where(c => c.CreatedAt.Year == toDate.Year && c.CreatedAt.Month == toDate.Month).ToList();
        var inYear = contracts.Where(c => c.CreatedAt.Year == toDate.Year).ToList();

        DashboardReportRow Row(string type, string label, List<Contract> list) =>
            new(type, label, list.Count, list.Sum(ValExchange));

        var rows = new List<DashboardReportRow>
        {
            Row("ContractCreateDate", "HĐ tạo trong ngày", createdToday),
            Row("ContractPending", "HĐ chờ ký", pending),
            Row("ContractDateInDay", "HĐ trong ngày", inDay),
            Row("ContractDateInWeek", "HĐ trong tuần", inWeek),
            Row("ContractDateInMonth", "HĐ trong tháng", inMonth),
            Row("ContractDateInYear", "HĐ trong năm", inYear),
        };
        return new DashboardReport(rows, fromDate, toDate);
    }

    // ── Link ký công khai (Contract_ContractSignLink) ────────────────
    // Tạo link ký cho 1 bên: sinh token bí mật + thời hạn (SignLinkEndDate).
    // Nguồn QContract: WAS_Contract_ContractSignLink_Save (SignLink + SignLinkEndDate).
    public async Task<ContractSignLink> CreateSignLinkAsync(int contractId, int partyId, int validHours, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (c.Status is not (ContractStatus.Sent or ContractStatus.PartiallySigned))
            throw new InvalidOperationException("Chỉ tạo link ký khi hợp đồng đã gửi và đang chờ ký.");
        var p = c.Parties.FirstOrDefault(x => x.Id == partyId)
            ?? throw new KeyNotFoundException("Không tìm thấy bên tham gia.");
        if (p.HasSigned) throw new InvalidOperationException($"{p.Name} đã ký rồi.");
        if (validHours <= 0) validHours = 72;

        var link = new ContractSignLink
        {
            ContractId = c.Id, PartyId = p.Id,
            Token = "sl_" + Guid.NewGuid().ToString("N"),
            EndDate = DateTime.Now.AddHours(validHours)
        };
        db.SignLinks.Add(link);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.SignLinkCreated, actor,
            $"Tạo link ký công khai cho {p.Name} — hết hạn {link.EndDate:dd/MM/yyyy HH:mm}");
        return link;
    }

    public Task<List<ContractSignLink>> SignLinksAsync(int contractId) =>
        db.SignLinks.Include(x => x.Party).Where(x => x.ContractId == contractId)
          .OrderByDescending(x => x.CreatedAt).ToListAsync();

    public async Task RevokeSignLinkAsync(int linkId, string actor)
    {
        var link = await db.SignLinks.Include(x => x.Party).FirstOrDefaultAsync(x => x.Id == linkId)
            ?? throw new KeyNotFoundException("Không tìm thấy link ký.");
        if (link.Revoked) return;
        link.Revoked = true;
        await db.SaveChangesAsync();
        await LogAsync(link.ContractId, HistoryAction.SignLinkRevoked, actor,
            $"Thu hồi link ký của {link.Party?.Name}");
    }

    // Kiểm tra link còn hiệu lực (tồn tại + chưa hết hạn + chưa thu hồi).
    // Nguồn QContract: Contract_ContractSignLink_CheckLink (SignLinkEndDate >= now).
    public Task<ContractSignLink?> ResolveSignLinkAsync(string token) =>
        db.SignLinks.Include(x => x.Contract).Include(x => x.Party)
          .FirstOrDefaultAsync(x => x.Token == token);

    // Ký qua link công khai (không cần đăng nhập) — dùng chữ ký CKS phía server.
    public async Task<(bool ok, string msg)> SignViaLinkAsync(string token, string? signerName)
    {
        var link = await ResolveSignLinkAsync(token);
        if (link == null) return (false, "Link ký không tồn tại.");
        if (link.Revoked) return (false, "Link ký đã bị thu hồi.");
        if (DateTime.Now > link.EndDate) return (false, "Link ký đã hết hạn.");
        if (link.UsedAt != null) return (false, "Link ký đã được sử dụng.");

        var (c, p, err) = await LoadForSign(link.ContractId, link.PartyId);
        if (err != null) return (false, err);

        var name = string.IsNullOrWhiteSpace(signerName) ? p!.Name : signerName.Trim();
        var (sigValue, certSubject) = signer.SignContract(c!.Id, c.Title, c.Body, name);
        db.Signatures.Add(new ContractSignature
        {
            ContractId = c.Id, PartyId = p!.Id, Method = SignMethod.DigitalCertificate,
            SignerName = name, CertSubject = certSubject, SignatureValue = sigValue
        });
        link.UsedAt = DateTime.Now;
        MarkSigned(c, p);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Signed, name, $"{name} ký qua link công khai — {Ui.Role(p.Role)}");
        if (c.Status == ContractStatus.Completed)
            await LogAsync(c.Id, HistoryAction.Completed, "system", $"Đủ chữ ký các bên — {c.Code} hoàn tất");
        return (true, $"{name} đã ký hợp đồng {c.Code} qua link công khai.");
    }

    // ── Ô ký trên hợp đồng (Contract_ContractElement) ────────────────
    // Nguồn QContract: WAS_Contract_ContractElement_Update (thêm/sửa ô ký) +
    // WAS_Contract_ContractElement_Calc (thống kê theo ElementType).
    public Task<List<ContractElement>> ElementsAsync(int contractId) =>
        db.Elements.Include(x => x.Party).Where(x => x.ContractId == contractId)
          .OrderBy(x => x.PageIdx).ThenBy(x => x.ElementY).ThenBy(x => x.ElementX).ToListAsync();

    public async Task<ContractElement> AddElementAsync(int contractId, ContractElement el)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Completed or ContractStatus.Cancelled)
            throw new InvalidOperationException("Hợp đồng đã kết thúc, không thêm ô ký.");
        if (el.PartyId.HasValue && c.Parties.All(p => p.Id != el.PartyId.Value))
            throw new InvalidOperationException("Bên sở hữu ô ký không thuộc hợp đồng này.");
        if (string.IsNullOrWhiteSpace(el.ElementCode)) el.ElementCode = "EL" + Guid.NewGuid().ToString("N")[..6];
        if (el.ElementWidth <= 0) el.ElementWidth = 120;
        if (el.ElementHeight <= 0) el.ElementHeight = 40;
        el.ContractId = c.Id;
        el.IsSigned = false;
        db.Elements.Add(el);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, "web",
            $"Thêm ô ký '{el.ElementName}' ({el.TypeLabel}) cho {(el.PartyId.HasValue ? c.Parties.First(p => p.Id == el.PartyId).Name : "—")}");
        return el;
    }

    // Đánh dấu 1 ô ký đã được ký — port từ ElementSignStatus/ConfirmBy/ConfirmDTimeUTC (QContract).
    public async Task<(bool ok, string msg)> SignElementAsync(int elementId, string signerName, string signFrom, string? ip)
    {
        var el = await db.Elements.Include(x => x.Contract).Include(x => x.Party)
            .FirstOrDefaultAsync(x => x.Id == elementId);
        if (el == null) return (false, "Không tìm thấy ô ký.");
        if (el.IsSigned) return (false, "Ô ký này đã được ký rồi.");
        if (el.Contract.Status is ContractStatus.Completed or ContractStatus.Cancelled)
            return (false, "Hợp đồng đã kết thúc, không thể ký ô ký.");

        el.IsSigned = true;
        el.SignerName = string.IsNullOrWhiteSpace(signerName) ? (el.Party?.Name ?? "—") : signerName.Trim();
        el.SignedAt = DateTime.Now;
        el.SignFrom = string.IsNullOrWhiteSpace(signFrom) ? "web" : signFrom;
        el.ElementIP = ip;
        await db.SaveChangesAsync();
        await LogAsync(el.ContractId, HistoryAction.Signed, el.SignerName,
            $"Ký ô '{el.ElementName}' ({el.TypeLabel}) — từ {el.SignFrom}");
        return (true, $"Đã ký ô '{el.ElementName}'.");
    }

    // Thống kê ô ký theo loại — port từ Contract_ContractElementSum (QContract).
    public async Task<ElementStats> ElementStatsAsync(int contractId)
    {
        var els = await db.Elements.Where(x => x.ContractId == contractId).ToListAsync();
        return new ElementStats(
            els.Count,
            els.Count(e => e.IsSigned),
            els.Count(e => e.Type == ElementType.Electronic),
            els.Count(e => e.Type == ElementType.Short),
            els.Count(e => e.Type == ElementType.Digital));
    }

    // ── Người kiểm tra hợp đồng (Contract_Checker) ───────────────────
    // Nguồn QContract: WAS_Contract_Checker_Accept (kiểm tra tuần tự theo Idx) +
    // Contract_Checker_CheckDB (kiểm tra tồn tại/FlagChecker/FlagActive).
    public Task<List<ContractChecker>> CheckersAsync(int contractId) =>
        db.Checkers.Where(x => x.ContractId == contractId)
          .OrderBy(x => x.Idx).ThenBy(x => x.Id).ToListAsync();

    public async Task<ContractChecker> AddCheckerAsync(int contractId, ContractChecker checker)
    {
        var c = await db.Contracts.Include(x => x.Checkers).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Completed or ContractStatus.Cancelled)
            throw new InvalidOperationException("Hợp đồng đã kết thúc, không thêm người kiểm tra.");
        if (string.IsNullOrWhiteSpace(checker.UserName))
            throw new InvalidOperationException("Cần tên người kiểm tra.");
        if (string.IsNullOrWhiteSpace(checker.UserCode))
            checker.UserCode = checker.UserName.Trim().ToLower().Replace(" ", ".");
        if (checker.Idx <= 0) checker.Idx = c.Checkers.Count + 1;
        checker.ContractId = c.Id;
        checker.HasChecked = false;
        db.Checkers.Add(checker);
        await db.SaveChangesAsync();

        // Có người kiểm tra → hợp đồng chuyển sang trạng thái chờ kiểm tra (PENDING).
        if (c.CheckerStatus == CheckerStatus.None)
        {
            c.CheckerStatus = CheckerStatus.Pending;
            await db.SaveChangesAsync();
        }
        await LogAsync(c.Id, HistoryAction.Remark, "web",
            $"Thêm người kiểm tra '{checker.UserName}' (thứ tự {checker.Idx}{(checker.Sequential ? ", tuần tự" : "")})");
        return checker;
    }

    // Người kiểm tra xác nhận đã kiểm tra — port từ Contract_Checker_AcceptX (QContract).
    // Kiểm tra tuần tự: nếu FlagSeq=1 thì phải kiểm tra theo thứ tự Idx (không được nhảy cóc).
    public async Task<(bool ok, string msg)> AcceptCheckAsync(int contractId, int checkerId, string? remark)
    {
        var c = await db.Contracts.Include(x => x.Checkers).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Completed or ContractStatus.Cancelled)
            return (false, "Hợp đồng đã kết thúc, không thể kiểm tra.");
        var ck = c.Checkers.FirstOrDefault(x => x.Id == checkerId);
        if (ck == null) return (false, "Không tìm thấy người kiểm tra.");
        if (!ck.IsChecker) return (false, $"{ck.UserName} không phải người kiểm tra.");
        if (ck.HasChecked) return (false, $"{ck.UserName} đã kiểm tra rồi.");

        // Kiểm tra tuần tự: các người kiểm tra trước (Idx nhỏ hơn) phải đã kiểm tra xong.
        if (ck.Sequential)
        {
            var earlier = c.Checkers.Where(x => x.IsChecker && x.Idx < ck.Idx && !x.HasChecked).ToList();
            if (earlier.Count > 0)
                return (false, $"Phải kiểm tra theo thứ tự — còn {earlier.Count} người kiểm tra trước chưa xử lý.");
        }

        ck.HasChecked = true;
        ck.CheckedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(remark)) ck.Remark = remark.Trim();

        // Đủ người kiểm tra → hợp đồng chuyển sang ONPROCESS (đã kiểm tra xong).
        var allChecked = c.Checkers.Where(x => x.IsChecker).All(x => x.HasChecked);
        if (allChecked) c.CheckerStatus = CheckerStatus.OnProcess;
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Remark, ck.UserName,
            $"{ck.UserName} kiểm tra hợp đồng{(string.IsNullOrWhiteSpace(remark) ? "" : " — " + remark.Trim())}");
        if (allChecked)
            await LogAsync(c.Id, HistoryAction.Remark, "system", $"Đủ người kiểm tra — {c.Code} chuyển sang xử lý");
        return (true, allChecked
            ? $"{ck.UserName} đã kiểm tra. Đủ người kiểm tra — hợp đồng chuyển sang xử lý."
            : $"{ck.UserName} đã kiểm tra hợp đồng.");
    }

    // Thống kê người kiểm tra — port từ Contract_Checker (QContract).
    public async Task<CheckerStats> CheckerStatsAsync(int contractId)
    {
        var c = await db.Contracts.Include(x => x.Checkers).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return new CheckerStats(0, 0, 0, CheckerStatus.None);
        var checkers = c.Checkers.Where(x => x.IsChecker).ToList();
        return new CheckerStats(
            checkers.Count,
            checkers.Count(x => x.HasChecked),
            checkers.Count(x => !x.HasChecked),
            c.CheckerStatus);
    }

    // ── Phê duyệt hợp đồng (Contract_Contract_Approved) ──────────────
    // Nguồn QContract: WAS_Contract_Contract_Approved → Contract_Contract_ApprovedX.
    // Luật cốt lõi: chỉ người có vai trò KIỂM TRA (FlagChecker=1) mới được phê duyệt;
    // hợp đồng phải đang ở trạng thái chờ kiểm tra (PENDING/ONPROCESS); khi duyệt,
    // hợp đồng chuyển sang ONPROCESS, ghi nhận ApprDTimeUTC/ApprBy và đánh dấu
    // ApprovedDTimeUTC cho người kiểm tra.
    public async Task<(bool ok, string msg)> ApproveAsync(int contractId, string userCode, string? remark)
    {
        var c = await db.Contracts.Include(x => x.Checkers).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không thể phê duyệt.");
        if (c.IsApproved) return (false, $"Hợp đồng đã được phê duyệt bởi {c.ApprovedBy}.");

        var who = string.IsNullOrWhiteSpace(userCode) ? "web" : userCode.Trim();
        // Người phê duyệt phải là người kiểm tra (FlagChecker=1) — nếu là người ký (FlagChecker=0) thì từ chối.
        var checker = c.Checkers.FirstOrDefault(x => x.IsChecker &&
            (x.UserCode.Equals(who, StringComparison.OrdinalIgnoreCase) || x.UserName.Equals(who, StringComparison.OrdinalIgnoreCase)));
        if (checker == null)
        {
            var signer = c.Checkers.FirstOrDefault(x => !x.IsChecker &&
                (x.UserCode.Equals(who, StringComparison.OrdinalIgnoreCase) || x.UserName.Equals(who, StringComparison.OrdinalIgnoreCase)));
            if (signer != null) return (false, $"{who} là người ký, không có quyền phê duyệt hợp đồng.");
            return (false, $"{who} không nằm trong danh sách người kiểm tra của hợp đồng.");
        }

        // Phải kiểm tra xong mới được duyệt (đủ người kiểm tra trước đó).
        if (checker.Sequential)
        {
            var earlier = c.Checkers.Where(x => x.IsChecker && x.Idx < checker.Idx && !x.HasChecked).ToList();
            if (earlier.Count > 0)
                return (false, $"Phải kiểm tra theo thứ tự — còn {earlier.Count} người kiểm tra trước chưa xử lý.");
        }

        var now = DateTime.Now;
        checker.HasChecked = true;
        checker.CheckedAt ??= now;
        checker.ApprovedAt = now;
        if (!string.IsNullOrWhiteSpace(remark)) checker.Remark = remark.Trim();

        c.ApprovedAt = now;
        c.ApprovedBy = who;
        c.CheckerStatus = CheckerStatus.OnProcess;   // ONPROCESS — đã kiểm tra/duyệt xong
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Approved, who,
            $"{who} phê duyệt {c.Kind.ToLower()} {c.Code}" + (string.IsNullOrWhiteSpace(remark) ? "" : $" — {remark.Trim()}"));
        return (true, $"Đã phê duyệt {c.Kind.ToLower()} {c.Code}.");
    }

    // Thống kê phê duyệt — port từ Contract_Contract_Approved (QContract).
    public async Task<ApproveStats> ApproveStatsAsync(int contractId)
    {
        var c = await db.Contracts.Include(x => x.Checkers).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return new ApproveStats(0, 0, false, null, null);
        var checkers = c.Checkers.Where(x => x.IsChecker).ToList();
        return new ApproveStats(
            checkers.Count,
            checkers.Count(x => x.ApprovedAt != null),
            c.IsApproved, c.ApprovedBy, c.ApprovedAt);
    }

    // ── Lý do kết thúc hợp đồng (Mst_FinishedContractReason) ─────────
    // Nguồn QContract: Mst_FinishedContractReason_CreateX / _UpdateX / _CheckDB.
    public async Task<List<FinishedContractReason>> FinishReasonsAsync(bool activeOnly = false)
    {
        var q = db.FinishReasons.AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.Type).ThenBy(x => x.Name).ToListAsync();
    }

    public async Task<FinishedContractReason> AddFinishReasonAsync(FinishedContractReason reason)
    {
        if (string.IsNullOrWhiteSpace(reason.Name))
            throw new InvalidOperationException("Cần tên lý do kết thúc.");
        if (string.IsNullOrWhiteSpace(reason.Code))
            reason.Code = "LR" + Guid.NewGuid().ToString("N")[..6].ToUpper();
        if (await db.FinishReasons.AnyAsync(x => x.Code == reason.Code))
            throw new InvalidOperationException($"Mã lý do '{reason.Code}' đã tồn tại.");
        db.FinishReasons.Add(reason);
        await db.SaveChangesAsync();
        return reason;
    }

    // Kết thúc/chấm dứt hợp đồng theo 1 lý do — port từ Contract_ContractParty_FinishX (QContract).
    // Ghi nhận lý do + mô tả + thời điểm, chuyển trạng thái hợp đồng sang FINISHED.
    public async Task<(bool ok, string msg)> FinishContractAsync(int contractId, int reasonId, string? description, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc.");
        var reason = await db.FinishReasons.FirstOrDefaultAsync(x => x.Id == reasonId);
        if (reason == null) return (false, "Không tìm thấy lý do kết thúc.");
        if (!reason.Active) return (false, $"Lý do '{reason.Name}' đã ngừng hiệu lực.");

        c.Status = ContractStatus.Finished;
        c.FinishReasonCode = reason.Code;
        c.FinishReasonName = reason.Name;
        c.FinishDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        c.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();

        var desc = $"{c.Kind} {c.Code} kết thúc — lý do: {reason.Name} ({reason.TypeLabel})"
            + (c.FinishDescription != null ? $" — {c.FinishDescription}" : "");
        await LogAsync(c.Id, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor, desc);
        return (true, $"Đã kết thúc {c.Kind.ToLower()} {c.Code} — {reason.Name}.");
    }

    // ── Phân quyền hợp đồng (Contract_UserInContract) ────────────────
    // Nguồn QContract: WAS_Contract_UserInContract_Save → Contract_UserInContract_SaveX.
    // Danh sách người dùng được phân quyền trên hợp đồng (theo thứ tự thêm).
    public Task<List<ContractUserInContract>> UserAssignmentsAsync(int contractId) =>
        db.UserAssignments.Where(x => x.ContractId == contractId)
          .OrderBy(x => x.Id).ToListAsync();

    // Lưu phân quyền hợp đồng — GHI ĐÈ TOÀN BỘ (full replace): xoá hết phân quyền cũ của
    // hợp đồng rồi ghi lại danh sách mới, đúng như Contract_UserInContract_SaveX (delete all + insert all).
    public async Task<(bool ok, string msg)> SaveUserAssignmentsAsync(int contractId, List<ContractUserInContract> users, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không phân quyền.");

        // Chuẩn hoá + loại trùng theo UserCode (giữ bản ghi đầu tiên).
        var clean = new List<ContractUserInContract>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in users ?? [])
        {
            var code = (u.UserCode ?? "").Trim();
            var name = (u.UserName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name)) continue;
            if (string.IsNullOrWhiteSpace(code)) code = name.ToLower().Replace(" ", ".");
            if (!seen.Add(code)) continue;
            clean.Add(new ContractUserInContract
            {
                ContractId = c.Id, UserCode = code, UserName = string.IsNullOrWhiteSpace(name) ? code : name,
                Email = string.IsNullOrWhiteSpace(u.Email) ? null : u.Email.Trim(),
                AssignedBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor
            });
        }

        // Xoá toàn bộ phân quyền cũ của hợp đồng (delete all).
        var old = await db.UserAssignments.Where(x => x.ContractId == c.Id).ToListAsync();
        db.UserAssignments.RemoveRange(old);
        // Ghi lại danh sách mới (insert all).
        db.UserAssignments.AddRange(clean);
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor,
            $"Phân quyền hợp đồng cho {clean.Count} người dùng" + (clean.Count > 0 ? ": " + string.Join(", ", clean.Select(x => x.UserName)) : ""));
        return (true, $"Đã cập nhật phân quyền hợp đồng — {clean.Count} người dùng.");
    }

    // ── Người ký của hợp đồng (Contract_ContractUser) ────────────────
    // Nguồn QContract: Contract_ContractUser_CheckDB (khóa nghiệp vụ là bộ ba
    // (ContractCode, PartyCode, UserCodeSysSign)) + Contract_ContractUser_ConfirmX
    // (xác nhận ký → CONFIRMED) + Contract_ContractUser_UpdateFlagSendUserX (đánh dấu đã gửi).
    public Task<List<ContractSigner>> SignersAsync(int contractId) =>
        db.Signers.Include(x => x.Party).Where(x => x.ContractId == contractId)
          .OrderBy(x => x.Idx).ThenBy(x => x.Id).ToListAsync();

    // Thêm 1 người ký cho một bên — port từ Contract_ContractUser_CheckDB (FlagExistToCheck=No).
    // Luật cốt lõi: bộ ba (hợp đồng, bên, mã người ký) KHÔNG được trùng; bên phải thuộc hợp đồng.
    public async Task<ContractSigner> AddSignerAsync(int contractId, ContractSigner signer, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).Include(x => x.Signers)
            .FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            throw new InvalidOperationException("Hợp đồng đã hủy hoặc đã kết thúc, không thêm người ký.");
        if (string.IsNullOrWhiteSpace(signer.UserNameSign))
            throw new InvalidOperationException("Cần tên người ký.");

        // Bên sở hữu người ký phải thuộc hợp đồng (nếu có chọn).
        ContractParty? party = null;
        if (signer.PartyId.HasValue)
        {
            party = c.Parties.FirstOrDefault(x => x.Id == signer.PartyId.Value)
                ?? throw new InvalidOperationException("Bên của người ký không thuộc hợp đồng này.");
        }
        var partyCode = party != null ? $"P{party.Id}" : "";

        // Mã người ký (UserCodeSysSign) bắt buộc — mặc định suy ra từ tên/email.
        if (string.IsNullOrWhiteSpace(signer.UserCodeSysSign))
            signer.UserCodeSysSign = string.IsNullOrWhiteSpace(signer.UserEmail)
                ? signer.UserNameSign.Trim().ToLower().Replace(" ", ".")
                : signer.UserEmail.Trim().ToLower();
        signer.UserCodeSysSign = signer.UserCodeSysSign.Trim();

        // Bộ ba (hợp đồng, bên, mã người ký) không trùng — port từ Contract_ContractUser_CheckDB (Flag.No).
        var dup = c.Signers.FirstOrDefault(x =>
            x.PartyCode == partyCode &&
            x.UserCodeSysSign.Equals(signer.UserCodeSysSign, StringComparison.OrdinalIgnoreCase));
        if (dup != null)
            throw new InvalidOperationException($"Người ký '{signer.UserCodeSysSign}' đã tồn tại cho bên này.");

        if (string.IsNullOrWhiteSpace(signer.UserCodeSign)) signer.UserCodeSign = signer.UserCodeSysSign;
        if (signer.Idx <= 0) signer.Idx = c.Signers.Count + 1;
        signer.ContractId = c.Id;
        signer.PartyId = party?.Id;
        signer.PartyCode = partyCode;
        signer.SignStatus = UserSignStatus.Pending;
        signer.FlagSendUser = false;
        signer.UserToken ??= "ut_" + Guid.NewGuid().ToString("N")[..12];
        signer.CreatedBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        db.Signers.Add(signer);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, signer.CreatedBy,
            $"Thêm người ký '{signer.UserNameSign}' cho {(party != null ? party.Name : "—")} (thứ tự {signer.Idx})");
        return signer;
    }

    // Xác nhận ký của 1 người ký — port từ Contract_ContractUser_ConfirmX (QContract).
    // Luật cốt lõi: người ký phải tồn tại và đang ở trạng thái PENDING; khi xác nhận,
    // UserSignSatus → CONFIRMED, ghi ConfirmDTimeUTC/ConfirmBy; đủ người ký → hợp đồng ONPROCESS.
    public async Task<(bool ok, string msg)> ConfirmSignerAsync(int contractId, int signerId, string actor)
    {
        var c = await db.Contracts.Include(x => x.Signers).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không thể xác nhận ký.");
        var s = c.Signers.FirstOrDefault(x => x.Id == signerId);
        if (s == null) return (false, "Không tìm thấy người ký.");
        if (s.IsConfirmed) return (false, $"{s.UserNameSign} đã xác nhận ký rồi.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var now = DateTime.Now;
        s.SignStatus = UserSignStatus.Confirmed;
        s.ConfirmDTimeUTC = now;
        s.ConfirmBy = who;
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Signed, who,
            $"{s.UserNameSign} xác nhận ký hợp đồng {c.Code}");
        var allConfirmed = c.Signers.All(x => x.IsConfirmed);
        if (allConfirmed)
            await LogAsync(c.Id, HistoryAction.Remark, "system", $"Đủ người ký xác nhận — {c.Code} chuyển sang xử lý");
        return (true, allConfirmed
            ? $"{s.UserNameSign} đã xác nhận ký. Đủ người ký — hợp đồng chuyển sang xử lý."
            : $"{s.UserNameSign} đã xác nhận ký hợp đồng.");
    }

    // Đánh dấu đã gửi yêu cầu ký cho 1 người ký — port từ Contract_ContractUser_UpdateFlagSendUserX (QContract).
    // Luật cốt lõi: FlagSendUser → 1, ghi SendDateUTC/SendBy; nếu hợp đồng đang PENDING thì chuyển ONPROCESS.
    public async Task<(bool ok, string msg)> MarkSignerSentAsync(int contractId, int signerId, string actor)
    {
        var c = await db.Contracts.Include(x => x.Signers).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không gửi yêu cầu ký.");
        var s = c.Signers.FirstOrDefault(x => x.Id == signerId);
        if (s == null) return (false, "Không tìm thấy người ký.");
        if (s.FlagSendUser) return (false, $"Đã gửi yêu cầu ký cho {s.UserNameSign} rồi.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var now = DateTime.Now;
        s.FlagSendUser = true;
        s.SendDateUTC = now;
        s.SendBy = who;
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Sent, who,
            $"Gửi yêu cầu ký cho {s.UserNameSign}" + (s.UserEmail != null ? $" → {s.UserEmail}" : ""));
        return (true, $"Đã gửi yêu cầu ký cho {s.UserNameSign}.");
    }

    // Thống kê người ký — port từ Contract_ContractUser (QContract).
    public async Task<SignerStats> SignerStatsAsync(int contractId)
    {
        var signers = await db.Signers.Where(x => x.ContractId == contractId).ToListAsync();
        return new SignerStats(
            signers.Count,
            signers.Count(x => x.IsConfirmed),
            signers.Count(x => x.SignStatus == UserSignStatus.Pending),
            signers.Count(x => x.FlagSendUser));
    }

    // ── Ký hợp đồng bởi một bên (Contract_Contract_PartySign) ────────
    // Nguồn QContract: WAS_Contract_Contract_PartySign → Contract_Contract_PartySignX.
    // Luật cốt lõi:
    //  - bên ký phải thuộc hợp đồng (Contract_ContractParty_CheckDB, FlagExistToCheck=Yes);
    //  - hợp đồng phải đang ở trạng thái chờ ký (PENDING/ONPROCESS);
    //  - nếu hợp đồng KHÔNG ở PENDING thì phải khớp ContractFileVersion (chống ký bản cũ);
    //  - nếu không có access token thì UserToken phải khớp token của bên;
    //  - ContractFileName bắt buộc;
    //  - nếu bật kiểm OTP thì mã OTP phải tồn tại và còn hiệu lực (EndDate >= now);
    //  - khi ký: bên → CONFIRMED (SignDateUTC/SignBy/UserCodeSign/UserNameSign/InfoToken),
    //    hợp đồng → ONPROCESS + cập nhật file/phiên bản; đủ TẤT CẢ bên CONFIRMED → hợp đồng CONFIRMED.
    public async Task<(bool ok, string msg)> PartySignAsync(int contractId, int partyId, string? userCodeSign,
        string? userNameSign, string? userToken, string? otpCode, string? fileVersion, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không thể ký.");
        if (c.Status is not (ContractStatus.Sent or ContractStatus.PartiallySigned))
            return (false, "Chỉ ký khi hợp đồng đã gửi và đang chờ ký.");

        var p = c.Parties.FirstOrDefault(x => x.Id == partyId);
        if (p == null) return (false, "Bên ký không thuộc hợp đồng này.");
        if (p.IsCancelled) return (false, $"{p.Name} đã hủy hợp đồng, không thể ký.");
        if (p.IsConfirmed) return (false, $"{p.Name} đã ký hợp đồng rồi.");

        // Kiểm tra phiên bản file: nếu hợp đồng không ở PENDING thì phải khớp phiên bản hiện tại.
        if (!string.IsNullOrWhiteSpace(fileVersion) && !string.IsNullOrWhiteSpace(c.FileVersion)
            && !string.Equals(fileVersion.Trim(), c.FileVersion, StringComparison.Ordinal))
            return (false, $"Phiên bản file không khớp (hiện tại: {c.FileVersion}). Tải lại bản mới nhất trước khi ký.");

        // Kiểm tra token người ký: nếu bên có token thì token nhập phải khớp.
        if (!string.IsNullOrWhiteSpace(p.UserToken)
            && !string.Equals(p.UserToken, userToken?.Trim(), StringComparison.Ordinal))
            return (false, "Token người ký không hợp lệ.");

        // Kiểm tra OTP: nếu có mã OTP đang hiệu lực cho hợp đồng thì bắt buộc nhập đúng.
        var activeOtp = await db.VerifyOtps
            .Where(o => o.ContractId == c.Id && o.Active && o.EndDate >= DateTime.Now)
            .OrderByDescending(o => o.CreateDate).FirstOrDefaultAsync();
        if (activeOtp != null)
        {
            if (string.IsNullOrWhiteSpace(otpCode))
                return (false, "Hợp đồng yêu cầu mã OTP xác thực — vui lòng nhập mã OTP.");
            if (!string.Equals(activeOtp.OtpCode, otpCode.Trim(), StringComparison.OrdinalIgnoreCase))
                return (false, "Mã OTP không đúng hoặc đã hết hạn.");
            activeOtp.UsedAt = DateTime.Now;
        }

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var now = DateTime.Now;
        var signerCode = string.IsNullOrWhiteSpace(userCodeSign)
            ? (string.IsNullOrWhiteSpace(p.Email) ? p.Name : p.Email) : userCodeSign.Trim();
        var signerName = string.IsNullOrWhiteSpace(userNameSign) ? p.Name : userNameSign.Trim();

        // Cập nhật bên ký → CONFIRMED.
        p.Status = PartyStatus.Confirmed;
        p.HasSigned = true;
        p.SignedAt = now;
        p.UserCodeSign = signerCode;
        p.UserNameSign = signerName;
        p.InfoToken = userToken?.Trim();
        p.SignDateUTC = now;
        p.SignBy = who;
        p.ContractFileVersion = c.FileVersion;

        // Hợp đồng chuyển sang ONPROCESS (đang xử lý) + ghi nhận người ký.
        c.Status = ContractStatus.PartiallySigned;

        // Đủ TẤT CẢ bên đã ký → hợp đồng CONFIRMED (hoàn tất).
        var allConfirmed = c.Parties.All(x => x.IsConfirmed);
        if (allConfirmed)
        {
            c.Status = ContractStatus.Completed;
            c.CompletedAt = now;
        }
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.PartySigned, signerCode,
            $"{signerName} ({Ui.Role(p.Role)}) ký hợp đồng {c.Code}");
        if (allConfirmed)
            await LogAsync(c.Id, HistoryAction.Completed, "system", $"Đủ chữ ký các bên — {c.Code} hoàn tất");
        return (true, allConfirmed
            ? $"{signerName} đã ký. Đủ chữ ký các bên — hợp đồng {c.Code} hoàn tất."
            : $"{signerName} đã ký hợp đồng {c.Code}.");
    }

    // Thống kê ký theo bên — port từ Contract_Contract_PartySign (QContract).
    public async Task<PartySignStats> PartySignStatsAsync(int contractId)
    {
        var parties = await db.Parties.Where(x => x.ContractId == contractId).ToListAsync();
        return new PartySignStats(
            parties.Count,
            parties.Count(x => x.IsConfirmed),
            parties.Count(x => !x.IsConfirmed && !x.IsCancelled),
            parties.Count > 0 && parties.All(x => x.IsConfirmed));
    }

    // ── Lịch sử gửi hợp đồng (Contract_SendHist) ─────────────────────
    // Nguồn QContract: WAS_Contract_SendHist_Get → Contract_SendHist_GetX (tra cứu theo
    // ContractCode + BulletinType, sắp theo thời gian gửi tăng dần).
    public Task<List<ContractSendHist>> SendHistoryAsync(int contractId, BulletinType? bulletin = null)
    {
        var q = db.SendHistory.Include(x => x.Party).Where(x => x.ContractId == contractId);
        if (bulletin.HasValue) q = q.Where(x => x.Bulletin == bulletin.Value);
        return q.OrderBy(x => x.SentAt).ThenBy(x => x.Id).ToListAsync();
    }

    // Ghi 1 bản ghi lịch sử gửi — port từ WAS_Contract_SendHist_Add → Contract_SendHist_SaveX.
    // Thông tin nhận (InfoReceive) mặc định lấy từ bên nhận theo kênh nếu không truyền vào.
    public async Task<ContractSendHist> AddSendHistAsync(int contractId, int? partyId, ChannelType channel,
        BulletinType bulletin, string? infoReceive, string? remark, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        var p = partyId.HasValue ? c.Parties.FirstOrDefault(x => x.Id == partyId.Value) : null;
        if (partyId.HasValue && p == null)
            throw new InvalidOperationException("Bên nhận không thuộc hợp đồng này.");

        var info = string.IsNullOrWhiteSpace(infoReceive)
            ? (channel == ChannelType.Email ? p?.Email : p?.Phone)
            : infoReceive.Trim();

        var h = new ContractSendHist
        {
            ContractId = c.Id, PartyId = p?.Id, PartyName = p?.Name ?? "",
            UserName = p?.Name ?? "", UserToken = p != null ? $"ut_{p.Id}" : null,
            Channel = channel, Bulletin = bulletin, InfoReceive = info,
            Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim(),
            SentBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor
        };
        db.SendHistory.Add(h);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, h.SentBy,
            $"Gửi {h.BulletinLabel} cho {h.PartyName} qua {h.ChannelLabel}" + (info != null ? $" → {info}" : ""));
        return h;
    }

    // Gửi lại (resend) các bản ghi đã chọn — port từ ContractReSendHist → ResendChannel (QContract).
    // Mỗi lần gửi lại tạo 1 bản ghi lịch sử mới (giữ nguyên bản ghi cũ) để có vết gửi đầy đủ.
    public async Task<(bool ok, string msg)> ResendAsync(int contractId, List<int> sendHistIds, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không gửi lại.");
        if (sendHistIds == null || sendHistIds.Count == 0)
            return (false, "Chưa chọn bản ghi để gửi lại.");

        var src = await db.SendHistory.Where(x => x.ContractId == contractId && sendHistIds.Contains(x.Id)).ToListAsync();
        if (src.Count == 0) return (false, "Không tìm thấy bản ghi gửi để gửi lại.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        foreach (var s in src)
            db.SendHistory.Add(new ContractSendHist
            {
                ContractId = c.Id, PartyId = s.PartyId, PartyName = s.PartyName,
                UserName = s.UserName, UserToken = s.UserToken, Channel = s.Channel,
                Bulletin = s.Bulletin, InfoReceive = s.InfoReceive,
                Remark = "Gửi lại", SentBy = who
            });
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, who, $"Gửi lại {src.Count} bản tin cho các bên");
        return (true, $"Đã gửi lại {src.Count} bản tin.");
    }

    // ── Quy tắc đánh số hợp đồng theo loại (Mst_ContractTypeContractNo) ──
    // Nguồn QContract: Mst_ContractTypeContractNo + Seq_ContractNo_Get (ghép tiền/hậu tố + số zero-pad).
    public Task<List<ContractNumberRule>> NumberRulesAsync() =>
        db.NumberRules.Include(x => x.Type).OrderBy(x => x.TypeId).ToListAsync();

    // Lưu (thêm/cập nhật) quy tắc đánh số cho 1 loại hợp đồng — mỗi loại chỉ có 1 quy tắc.
    public async Task<ContractNumberRule> SaveNumberRuleAsync(ContractNumberRule rule)
    {
        var type = await db.ContractTypes.FirstOrDefaultAsync(t => t.Id == rule.TypeId)
            ?? throw new InvalidOperationException("Không tìm thấy loại hợp đồng.");
        if (rule.SeqNumberLength <= 0) rule.SeqNumberLength = 4;
        if (rule.NumberStart <= 0) rule.NumberStart = 1;
        rule.TypefixInput ??= "";

        var existing = await db.NumberRules.FirstOrDefaultAsync(x => x.TypeId == rule.TypeId);
        if (existing == null)
        {
            db.NumberRules.Add(rule);
            existing = rule;
        }
        else
        {
            existing.TypefixCode = rule.TypefixCode;
            existing.TypefixInput = rule.TypefixInput;
            existing.SeqNumberLength = rule.SeqNumberLength;
            existing.NumberStart = rule.NumberStart;
            existing.Active = rule.Active;
        }
        await db.SaveChangesAsync();
        return existing;
    }

    // Xem trước các số hợp đồng kế tiếp của 1 loại — port từ Seq_ContractNo_Get (sinh N số liên tiếp).
    public async Task<List<string>> PreviewNumbersAsync(int typeId, int amount)
    {
        if (amount <= 0) amount = 5;
        var rule = await db.NumberRules.FirstOrDefaultAsync(x => x.TypeId == typeId);
        if (rule == null) return [];
        var start = await NextNumberAsync(rule);
        return Enumerable.Range(0, amount).Select(i => rule.Build(start + i)).ToList();
    }

    // Sinh số hợp đồng kế tiếp cho 1 loại: nếu có quy tắc thì dùng quy tắc, ngược lại dùng mã mặc định HDyyMM-nnnn.
    // Nguồn QContract: Contract_Contract_CountByContractType_Get (đếm theo loại) + Seq_ContractNo_Get.
    private async Task<string> NextCodeAsync(int? typeId)
    {
        if (typeId.HasValue)
        {
            var rule = await db.NumberRules.FirstOrDefaultAsync(x => x.TypeId == typeId.Value && x.Active);
            if (rule != null)
            {
                var start = await NextNumberAsync(rule);
                var code = rule.Build(start);
                // Tránh trùng nếu số đã tồn tại (VD do xóa/tạo lại) — tăng dần cho tới khi trống.
                while (await db.Contracts.AnyAsync(c => c.Code == code))
                    code = rule.Build(++start);
                return code;
            }
        }
        var count = await db.Contracts.CountAsync();
        return $"HD{DateTime.Now:yyMM}-{count + 1:D4}";
    }

    // Số thứ tự kế tiếp = số lớn nhất đã dùng của loại + 1 (tối thiểu NumberStart).
    private async Task<long> NextNumberAsync(ContractNumberRule rule)
    {
        var codes = await db.Contracts.Where(c => c.TypeId == rule.TypeId).Select(c => c.Code).ToListAsync();
        long max = rule.NumberStart - 1;
        foreach (var code in codes)
        {
            var digits = new string(code.Where(char.IsDigit).ToArray());
            if (long.TryParse(digits, out var n) && n > max) max = n;
        }
        return max + 1;
    }

    // ── helpers ──────────────────────────────────────────────────────
    // ── Hủy hợp đồng bởi một bên (Contract_ContractParty_Cancel) ─────
    // Nguồn QContract: WAS_Contract_ContractParty_Cancel → Contract_ContractParty_CancelX.
    // Luật cốt lõi: hợp đồng phải đang xử lý (chưa hủy/kết thúc); bên hủy phải thuộc hợp đồng.
    // Khi hủy: hợp đồng chuyển CANCELED (CancelDTimeUTC), bên đó chuyển CANCELED
    // (CancelDTimeUTC + CancelBy), ghi nhật ký thao tác "PartyCancel".
    public async Task<(bool ok, string msg)> CancelByPartyAsync(int contractId, int partyId, string? remark, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc.");
        var p = c.Parties.FirstOrDefault(x => x.Id == partyId);
        if (p == null) return (false, "Bên hủy không thuộc hợp đồng này.");
        if (p.IsCancelled) return (false, $"{p.Name} đã hủy hợp đồng rồi.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var now = DateTime.Now;

        // Hợp đồng → CANCELED.
        c.Status = ContractStatus.Cancelled;
        // Bên hủy → CANCELED (ghi nhận người hủy + thời điểm).
        p.Status = PartyStatus.Cancelled;
        p.CancelledAt = now;
        p.CancelledBy = who;
        if (!string.IsNullOrWhiteSpace(remark)) p.Remark = remark.Trim();
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.PartyCancelled, who,
            $"{p.Name} ({Ui.Role(p.Role)}) hủy {c.Kind.ToLower()} {c.Code}"
            + (string.IsNullOrWhiteSpace(remark) ? "" : $" — {remark.Trim()}"));
        return (true, $"{p.Name} đã hủy {c.Kind.ToLower()} {c.Code}.");
    }

    // ── Cập nhật ghi chú của một bên (Contract_Contract_Party_UpdateRemark) ──
    // Nguồn QContract: WAS_Contract_Contract_Party_UpdateRemark → Contract_Contract_Party_UpdateRemarkX.
    // Luật cốt lõi: cập nhật trường Remark của một bên (định danh theo hợp đồng + bên), ghi
    // LogLUDTimeUTC/LogLUBy và ghi nhật ký thao tác "UpdateRemark" (TConst.FunctionActionType.UpdateRemark).
    public async Task<(bool ok, string msg)> UpdatePartyRemarkAsync(int contractId, int partyId, string? remark, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        var p = c.Parties.FirstOrDefault(x => x.Id == partyId);
        if (p == null) return (false, "Bên cần cập nhật không thuộc hợp đồng này.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var newRemark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        p.Remark = newRemark;
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.UpdateRemark, who,
            $"Cập nhật ghi chú cho {p.Name} ({Ui.Role(p.Role)})"
            + (newRemark != null ? $" — {newRemark}" : " — (xóa ghi chú)"));
        return (true, $"Đã cập nhật ghi chú cho {p.Name}.");
    }

    // ── Cập nhật hợp đồng sau phê duyệt (Contract_ContractParty_UpdAfterApproved) ──
    // Nguồn QContract: WAS_Contract_ContractParty_UpdAfterApproved → Contract_ContractParty_UpdAfterApprovedX.
    // Luật cốt lõi:
    //  - hợp đồng phải tồn tại và đang ở trạng thái ONPROCESS/CONFIRMED (đã gửi/đã duyệt, chưa hủy/kết thúc);
    //  - bên cập nhật phải thuộc hợp đồng (Contract_ContractParty_CheckDB, FlagExistToCheck=Yes);
    //  - ValContract >= 0 và ValPaymented >= 0 (nếu âm → lỗi InvalidValContract/InvalidValPaymented);
    //  - cập nhật ValContract, ValExchange (= ValContract * CurrencyRate), ValPaymented,
    //    ContractType/ContractTypeName, Remark; tính lại ValRemain = ValContract - ValPaymented;
    //  - ghi nhật ký thao tác "PartyUpdAfterApproved".
    public async Task<(bool ok, string msg)> UpdateAfterApprovedAsync(int contractId, int partyId, decimal valContract,
        decimal valPaymented, string? contractType, string? contractTypeName, string? remark, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        // Hợp đồng phải đang xử lý (đã gửi/đã duyệt) — port từ Contract_Contract_CheckDB (ONPROCESS, CONFIRMED).
        if (c.Status is not (ContractStatus.Sent or ContractStatus.PartiallySigned or ContractStatus.Completed))
            return (false, "Chỉ cập nhật giá trị hợp đồng sau khi hợp đồng đã gửi/đã phê duyệt.");

        var p = c.Parties.FirstOrDefault(x => x.Id == partyId);
        if (p == null) return (false, "Bên cần cập nhật không thuộc hợp đồng này.");

        // Kiểm tra giá trị không âm — port từ Contract_ContractParty_UpdAfterApprovedX_InvalidValContract/ValPaymented.
        if (valContract < 0) return (false, "Giá trị hợp đồng không được âm.");
        if (valPaymented < 0) return (false, "Giá trị đã thanh toán không được âm.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var rate = c.CurrencyRate <= 0 ? 1 : c.CurrencyRate;
        p.ValContract = valContract;
        p.ValExchange = valContract * rate;                 // ValExchange = ValContract * CurrencyRate
        p.ValPaymented = valPaymented;
        p.ValRemain = valContract - valPaymented;           // ValRemain = ValContract - ValPaymented
        p.ContractType = string.IsNullOrWhiteSpace(contractType) ? null : contractType.Trim();
        p.ContractTypeName = string.IsNullOrWhiteSpace(contractTypeName) ? null : contractTypeName.Trim();
        p.Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        p.ValueUpdatedAt = DateTime.Now;
        p.ValueUpdatedBy = who;
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.PartyUpdAfterApproved, who,
            $"Cập nhật giá trị hợp đồng cho {p.Name} ({Ui.Role(p.Role)}): "
            + $"giá trị {p.ValContract:N0} đ, đã thanh toán {p.ValPaymented:N0} đ, còn lại {p.ValRemain:N0} đ"
            + (p.ContractTypeName != null ? $" — loại: {p.ContractTypeName}" : ""));
        return (true, $"Đã cập nhật giá trị hợp đồng cho {p.Name}.");
    }

    // ── Cập nhật thông tin bên tham gia (Contract_ContractParty_Update) ──
    // Nguồn QContract: WAS_Contract_ContractParty_Update → Contract_ContractParty_UpdateX.
    // Luật cốt lõi: ContractCode bắt buộc (rỗng → lỗi Contract_ContractParty_UpdateX); bên phải
    // thuộc hợp đồng (Contract_ContractParty_CheckDB, FlagExistToCheck=Yes); cập nhật thông tin
    // pháp lý + liên hệ của bên (địa chỉ, MST, email, SĐT, website, ngân hàng, người đại diện).
    public async Task<(bool ok, string msg)> UpdatePartyAsync(int contractId, int partyId, ContractParty info, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không cập nhật thông tin bên.");

        // ContractCode bắt buộc — port từ Contract_ContractParty_UpdateX (strContractCode rỗng → lỗi).
        if (string.IsNullOrWhiteSpace(c.Code))
            return (false, "Hợp đồng chưa có số (ContractCode), không cập nhật được.");

        // Bên phải thuộc hợp đồng — port từ Contract_ContractParty_CheckDB (FlagExistToCheck=Yes).
        var p = c.Parties.FirstOrDefault(x => x.Id == partyId);
        if (p == null) return (false, "Bên cần cập nhật không thuộc hợp đồng này.");
        if (string.IsNullOrWhiteSpace(info.Name))
            return (false, "Cần tên bên tham gia.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        p.Name = info.Name.Trim();
        p.TaxCode = string.IsNullOrWhiteSpace(info.TaxCode) ? null : info.TaxCode.Trim();
        p.Email = string.IsNullOrWhiteSpace(info.Email) ? null : info.Email.Trim();
        p.Phone = string.IsNullOrWhiteSpace(info.Phone) ? null : info.Phone.Trim();
        p.Address = string.IsNullOrWhiteSpace(info.Address) ? null : info.Address.Trim();
        p.Website = string.IsNullOrWhiteSpace(info.Website) ? null : info.Website.Trim();
        p.BankCode = string.IsNullOrWhiteSpace(info.BankCode) ? null : info.BankCode.Trim();
        p.BankName = string.IsNullOrWhiteSpace(info.BankName) ? null : info.BankName.Trim();
        p.BankAccountNo = string.IsNullOrWhiteSpace(info.BankAccountNo) ? null : info.BankAccountNo.Trim();
        p.RepresentName = string.IsNullOrWhiteSpace(info.RepresentName) ? null : info.RepresentName.Trim();
        p.RepresentPosition = string.IsNullOrWhiteSpace(info.RepresentPosition) ? null : info.RepresentPosition.Trim();
        p.InfoUpdatedAt = DateTime.Now;
        p.InfoUpdatedBy = who;
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Remark, who,
            $"Cập nhật thông tin {Ui.Role(p.Role)} '{p.Name}'"
            + (p.TaxCode != null ? $" — MST {p.TaxCode}" : "")
            + (p.RepresentName != null ? $" — người đại diện {p.RepresentName}" : ""));
        return (true, $"Đã cập nhật thông tin {Ui.Role(p.Role)} '{p.Name}'.");
    }

    // Thống kê thông tin bên tham gia — port từ Contract_ContractParty (QContract).
    public async Task<PartyInfoStats> PartyInfoStatsAsync(int contractId)
    {
        var parties = await db.Parties.Where(x => x.ContractId == contractId).ToListAsync();
        return new PartyInfoStats(
            parties.Count,
            parties.Count(p => p.HasInfo),
            parties.Count(p => p.EmailSent));
    }

    // ── Ghi nhận gửi email cho bên (Contract_ContractParty_UpdEmailSend) ──
    // Nguồn QContract: WAS_Contract_ContractParty_UpdEmailSend → Contract_ContractParty_UpdEmailSendX.
    // Luật cốt lõi: bên phải tồn tại (Contract_ContractParty_CheckDB, FlagExistToCheck=Yes);
    // khi gửi mail, ghi nhận EmailSend (email đã dùng) + SendEmailDTimeUTC + SendEmailBy.
    public async Task<(bool ok, string msg)> MarkPartyEmailSentAsync(int contractId, int partyId, string? emailSend, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không gửi mail.");

        // Bên phải tồn tại — port từ Contract_ContractParty_CheckDB (FlagExistToCheck=Yes).
        var p = c.Parties.FirstOrDefault(x => x.Id == partyId);
        if (p == null) return (false, "Bên nhận mail không thuộc hợp đồng này.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var email = string.IsNullOrWhiteSpace(emailSend) ? p.Email : emailSend.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return (false, $"Bên '{p.Name}' chưa có email nhận thông báo.");

        p.EmailSend = email;
        p.SendEmailDTimeUTC = DateTime.Now;
        p.SendEmailBy = who;
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Sent, who,
            $"Gửi mail thông báo cho {Ui.Role(p.Role)} '{p.Name}' → {email}");
        return (true, $"Đã ghi nhận gửi mail cho {p.Name} → {email}.");
    }

    // ── Mã OTP xác thực ký hợp đồng (Contract_ContractVerifyOtp) ─────
    // Nguồn QContract: WAS_Contract_ContractVerifyOtp_Save → Contract_ContractVerifyOtp_SaveX
    // + Contract_ContractVerifyOtp_GetX (sinh mã ngẫu nhiên 6 ký tự hex, hạn 2 phút).
    public Task<List<ContractVerifyOtp>> VerifyOtpsAsync(int contractId) =>
        db.VerifyOtps.Where(x => x.ContractId == contractId)
          .OrderByDescending(x => x.CreateDate).ThenByDescending(x => x.Id).ToListAsync();

    // Sinh mã OTP mới cho (hợp đồng, người ký) — port từ Contract_ContractVerifyOtp_SaveX.
    // Luật cốt lõi: XOÁ toàn bộ mã cũ của cặp (ContractCode, UserCodeSign) rồi ghi mã mới
    // (delete all + insert); mã ngẫu nhiên 6 ký tự hex, hạn mặc định 2 phút.
    public async Task<ContractVerifyOtp> GenerateVerifyOtpAsync(int contractId, int partyId, int validMinutes, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            throw new InvalidOperationException("Hợp đồng đã hủy hoặc đã kết thúc, không sinh OTP.");
        var p = c.Parties.FirstOrDefault(x => x.Id == partyId)
            ?? throw new KeyNotFoundException("Không tìm thấy bên tham gia.");
        if (p.HasSigned) throw new InvalidOperationException($"{p.Name} đã ký rồi.");
        if (validMinutes <= 0) validMinutes = 2;   // mặc định 2 phút như QContract
        var userCodeSign = string.IsNullOrWhiteSpace(p.Email) ? p.Name : p.Email;
        var now = DateTime.Now;

        // Xoá toàn bộ mã OTP cũ của cặp (hợp đồng, người ký) — delete all.
        var old = await db.VerifyOtps
            .Where(x => x.ContractId == c.Id && x.UserCodeSign == userCodeSign).ToListAsync();
        db.VerifyOtps.RemoveRange(old);

        // Ghi mã mới — insert.
        var otp = new ContractVerifyOtp
        {
            ContractId = c.Id, ContractCode = c.Code,
            OtpCode = RandomHex(6), UserCodeSign = userCodeSign,
            CreateDate = now, EndDate = now.AddMinutes(validMinutes),
            Active = true, CreatedBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor
        };
        db.VerifyOtps.Add(otp);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, otp.CreatedBy,
            $"Sinh mã OTP xác thực cho {p.Name} — hết hạn {otp.EndDate:HH:mm:ss}");
        return otp;
    }

    // Xác thực mã OTP — port từ Contract_ContractVerifyOtp_SaveX (kiểm tra EndDate >= now).
    // Mã phải còn hiệu lực (FlagActive) và chưa hết hạn; khi hợp lệ thì đánh dấu đã dùng.
    public async Task<(bool ok, string msg)> VerifyOtpAsync(int contractId, string otpCode, string userCodeSign)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (string.IsNullOrWhiteSpace(otpCode)) return (false, "Cần nhập mã OTP.");

        var code = otpCode.Trim();
        var who = (userCodeSign ?? "").Trim();
        var now = DateTime.Now;

        // Tìm mã còn hiệu lực + chưa hết hạn của hợp đồng (và người ký nếu có).
        var otp = await db.VerifyOtps
            .Where(x => x.ContractId == c.Id && x.OtpCode == code && x.Active && x.EndDate >= now)
            .Where(x => string.IsNullOrWhiteSpace(who) || x.UserCodeSign == who)
            .OrderByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync();
        if (otp == null) return (false, "Mã OTP không đúng hoặc đã hết hạn.");
        if (otp.UsedAt != null) return (false, "Mã OTP này đã được sử dụng.");

        otp.UsedAt = now;
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, who == "" ? "web" : who,
            $"Xác thực OTP thành công cho {otp.UserCodeSign}");
        return (true, $"Xác thực OTP thành công cho {otp.UserCodeSign}.");
    }

    // ── Hợp đồng mẫu (Contract_TempContract) ─────────────────────────
    // Nguồn QContract: Contract_TempContract_SaveX / _CheckDB / _GetX.
    // Luật cốt lõi: mã mẫu + tên mẫu bắt buộc; tên mẫu KHÔNG trùng trong cùng Org;
    // loại hợp đồng phải tồn tại & đang hiệu lực; không xóa mẫu đang được hợp đồng dùng.
    public async Task<List<ContractTemplate>> TemplatesAsync(bool activeOnly = false)
    {
        var q = db.Templates.Include(x => x.Type).AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.Name).ToListAsync();
    }

    // Lưu (thêm/cập nhật) hợp đồng mẫu — port từ Contract_TempContract_SaveX (QContract).
    public async Task<ContractTemplate> SaveTemplateAsync(ContractTemplate template, string actor)
    {
        if (string.IsNullOrWhiteSpace(template.Name))
            throw new InvalidOperationException("Cần tên hợp đồng mẫu.");
        if (string.IsNullOrWhiteSpace(template.Code))
            template.Code = "M" + Guid.NewGuid().ToString("N")[..6].ToUpper();

        // Loại hợp đồng phải tồn tại (nếu có chọn) — port từ Mst_ContractType_CheckDB.
        if (template.TypeId.HasValue &&
            !await db.ContractTypes.AnyAsync(t => t.Id == template.TypeId.Value))
            throw new InvalidOperationException("Loại hợp đồng không tồn tại.");

        // Tên mẫu không trùng trong cùng Org — port từ Contract_TempContract_SaveX (TContracNameExist).
        var dup = await db.Templates.FirstOrDefaultAsync(x => x.Name == template.Name);
        if (dup != null && dup.Id != template.Id)
            throw new InvalidOperationException($"Tên mẫu '{template.Name}' đã tồn tại.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var existing = template.Id > 0 ? await db.Templates.FirstOrDefaultAsync(x => x.Id == template.Id) : null;
        if (existing == null)
        {
            template.CreatedBy = who;
            db.Templates.Add(template);
            existing = template;
        }
        else
        {
            existing.Name = template.Name.Trim();
            existing.TypeId = template.TypeId;
            existing.Body = template.Body ?? "";
            existing.Remark = template.Remark;
            existing.Active = template.Active;
        }
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa hợp đồng mẫu — port từ Contract_TempContract_SaveX (nhánh delete).
    // Chặn xóa nếu mẫu đang được hợp đồng sử dụng (Contract_Contract.TContractCode).
    public async Task<(bool ok, string msg)> DeleteTemplateAsync(int templateId, string actor)
    {
        var t = await db.Templates.FirstOrDefaultAsync(x => x.Id == templateId);
        if (t == null) return (false, "Không tìm thấy hợp đồng mẫu.");
        var used = await db.Contracts.CountAsync(c => c.TemplateId == t.Id);
        if (used > 0)
            return (false, $"Mẫu '{t.Name}' đang được {used} hợp đồng sử dụng, không thể xóa.");
        db.Templates.Remove(t);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa hợp đồng mẫu '{t.Name}'.");
    }

    // Soạn hợp đồng mới từ mẫu — port từ luồng Create(tcontractcode) (QContract).
    // Copy loại + nội dung mẫu (nếu không nhập nội dung riêng), ghi nhận TemplateCode.
    public async Task<int> CreateFromTemplateAsync(int templateId, string title, decimal value, string? body,
        List<ContractParty> parties, string actor)
    {
        var t = await db.Templates.FirstOrDefaultAsync(x => x.Id == templateId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng mẫu.");
        if (!t.Active) throw new InvalidOperationException($"Mẫu '{t.Name}' đã ngừng hiệu lực.");

        var c = new Contract
        {
            Title = string.IsNullOrWhiteSpace(title) ? t.Name : title.Trim(),
            TypeId = t.TypeId,
            Body = string.IsNullOrWhiteSpace(body) ? t.Body : body,
            Value = value,
            CreatedBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor,
            TemplateId = t.Id,
            TemplateCode = t.Code
        };
        var id = await CreateAsync(c, parties);
        await LogAsync(id, HistoryAction.Remark, c.CreatedBy, $"Soạn hợp đồng từ mẫu '{t.Name}' ({t.Code})");
        return id;
    }

    // ── Nhóm hợp đồng mẫu (Contract_TempGroup) ───────────────────────
    // Nguồn QContract: Contract_TempGroup_GetX / _CreateX / _UpdateX / _DeleteX / _CheckDB.
    public async Task<List<ContractTemplateGroup>> TemplateGroupsAsync(bool activeOnly = false)
    {
        var q = db.TemplateGroups.Include(x => x.Attributes).AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.Name).ToListAsync();
    }

    public Task<ContractTemplateGroup?> TemplateGroupAsync(int id) =>
        db.TemplateGroups.Include(x => x.Attributes).FirstOrDefaultAsync(x => x.Id == id);

    // Lưu (thêm/cập nhật) nhóm hợp đồng mẫu + thuộc tính — port từ Contract_TempGroup_CreateX/_UpdateX.
    // Luật cốt lõi: mã nhóm bắt buộc & KHÔNG trùng khi tạo; mỗi thuộc tính phải có mã + giá trị;
    // khi cập nhật thì GHI ĐÈ toàn bộ thuộc tính cũ của nhóm (delete all + insert all).
    public async Task<ContractTemplateGroup> SaveTemplateGroupAsync(ContractTemplateGroup group,
        List<ContractAttributeGroup> attributes, string actor)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
            throw new InvalidOperationException("Cần tên nhóm hợp đồng mẫu.");
        if (string.IsNullOrWhiteSpace(group.Code))
            group.Code = "G" + Guid.NewGuid().ToString("N")[..6].ToUpper();

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var existing = group.Id > 0 ? await db.TemplateGroups.Include(x => x.Attributes)
            .FirstOrDefaultAsync(x => x.Id == group.Id) : null;

        // Mã nhóm không trùng trong cùng Org — port từ Contract_TempGroup_CheckDB (Flag.No khi tạo).
        var dup = await db.TemplateGroups.FirstOrDefaultAsync(x => x.Code == group.Code);
        if (dup != null && dup.Id != group.Id)
            throw new InvalidOperationException($"Mã nhóm '{group.Code}' đã tồn tại.");

        // Chuẩn hoá + kiểm tra thuộc tính — port từ Contract_TempGroup_CreateX (mã + giá trị bắt buộc).
        var clean = new List<ContractAttributeGroup>();
        foreach (var a in attributes ?? [])
        {
            var code = (a.AttributeCode ?? "").Trim();
            var val = (a.AttributeValue ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(val)) continue;
            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException("Thuộc tính phải có mã (AttributeContractCode).");
            if (string.IsNullOrWhiteSpace(val))
                throw new InvalidOperationException($"Thuộc tính '{code}' phải có giá trị (AttributeValue).");
            clean.Add(new ContractAttributeGroup { AttributeCode = code, AttributeValue = val, CreatedBy = who });
        }

        if (existing == null)
        {
            group.CreatedBy = who;
            group.Attributes = clean;
            db.TemplateGroups.Add(group);
            existing = group;
        }
        else
        {
            existing.Name = group.Name.Trim();
            existing.Body = group.Body;
            existing.ContractName = group.ContractName;
            existing.Remark = group.Remark;
            existing.Active = group.Active;
            // Ghi đè toàn bộ thuộc tính cũ (delete all + insert all).
            db.AttributeGroups.RemoveRange(existing.Attributes);
            existing.Attributes = clean;
        }
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa nhóm hợp đồng mẫu — port từ Contract_TempGroup_DeleteX (xóa kèm toàn bộ thuộc tính của nhóm).
    public async Task<(bool ok, string msg)> DeleteTemplateGroupAsync(int groupId, string actor)
    {
        var g = await db.TemplateGroups.Include(x => x.Attributes).FirstOrDefaultAsync(x => x.Id == groupId);
        if (g == null) return (false, "Không tìm thấy nhóm hợp đồng mẫu.");
        db.AttributeGroups.RemoveRange(g.Attributes);   // delete all attributes
        db.TemplateGroups.Remove(g);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa nhóm hợp đồng mẫu '{g.Name}'.");
    }

    // ── Chữ ký số của tổ chức (Mst_OrgCKS) ────────────────────────────
    // Nguồn QContract: Mst_OrgCKS_GetX / _CreateX / _UpdateX / _DeleteX / _CheckDB.
    public async Task<List<OrgCertificate>> CertificatesAsync(bool activeOnly = false)
    {
        var q = db.OrgCertificates.AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.CANumber).ToListAsync();
    }

    // Lưu (thêm/cập nhật) chứng thư số — port từ Mst_OrgCKS_CreateX / _UpdateX (QContract).
    // Luật cốt lõi: CANumber bắt buộc; cặp (OrgID, CANumber) KHÔNG trùng khi tạo và phải tồn tại khi sửa.
    public async Task<OrgCertificate> SaveCertificateAsync(OrgCertificate cert, string actor)
    {
        if (string.IsNullOrWhiteSpace(cert.CANumber))
            throw new InvalidOperationException("Cần số chứng thư (CANumber).");
        cert.CANumber = cert.CANumber.Trim();

        // Hiệu lực: nếu có cả hai mốc thì mốc kết thúc phải sau mốc bắt đầu.
        if (cert.EffectiveFrom.HasValue && cert.EffectiveTo.HasValue && cert.EffectiveTo < cert.EffectiveFrom)
            throw new InvalidOperationException("Hiệu lực đến phải sau hiệu lực từ.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var existing = cert.Id > 0 ? await db.OrgCertificates.FirstOrDefaultAsync(x => x.Id == cert.Id) : null;

        // Cặp (OrgID, CANumber) không trùng — port từ Mst_OrgCKS_CheckDB (Flag.No khi tạo).
        var dup = await db.OrgCertificates.FirstOrDefaultAsync(x => x.CANumber == cert.CANumber);
        if (dup != null && dup.Id != cert.Id)
            throw new InvalidOperationException($"Chứng thư '{cert.CANumber}' đã tồn tại.");

        if (existing == null)
        {
            cert.CreatedBy = who;
            db.OrgCertificates.Add(cert);
            existing = cert;
        }
        else
        {
            // Cập nhật từng phần — port từ Mst_OrgCKS_UpdateX (Ft_Cols_Upd).
            existing.CANumber = cert.CANumber;
            existing.CAOrg = cert.CAOrg;
            existing.EffectiveFrom = cert.EffectiveFrom;
            existing.EffectiveTo = cert.EffectiveTo;
            existing.CtsPath = cert.CtsPath;
            existing.CtsPwd = cert.CtsPwd;
            existing.Active = cert.Active;
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = who;
        }
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa chứng thư số — port từ Mst_OrgCKS_DeleteX (phải tồn tại mới xóa được).
    public async Task<(bool ok, string msg)> DeleteCertificateAsync(int id, string actor)
    {
        var c = await db.OrgCertificates.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return (false, "Không tìm thấy chứng thư số.");
        db.OrgCertificates.Remove(c);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa chứng thư '{c.CANumber}'.");
    }

    // ── Cấu hình ký của tổ chức (Mst_OrgSignConfig) ──────────────────
    // Nguồn QContract: Mst_OrgSignConfig_GetX / _CreateX / _UpdateX / _DeleteX / _CheckDB.
    public async Task<List<OrgSignConfig>> SignConfigsAsync(bool activeOnly = false)
    {
        var q = db.SignConfigs.AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.SignType).ThenBy(x => x.OrgCode).ToListAsync();
    }

    // Lưu (thêm/cập nhật) cấu hình ký — port từ Mst_OrgSignConfig_CreateX / _UpdateX (QContract).
    // Luật cốt lõi: với mỗi cặp (SignType, OrgID) chỉ được có TỐI ĐA 1 bản ghi đang hiệu lực
    // (FlagActive=1) — nếu nhiều hơn 1 thì lỗi MoreThanOneActive.
    public async Task<OrgSignConfig> SaveSignConfigAsync(OrgSignConfig config, string actor)
    {
        if (string.IsNullOrWhiteSpace(config.OrgCode))
            throw new InvalidOperationException("Cần mã tổ chức (OrgID).");
        config.OrgCode = config.OrgCode.Trim();

        // Hiệu lực: nếu có cả hai mốc thì mốc kết thúc phải sau mốc bắt đầu.
        if (config.EffectiveFrom.HasValue && config.EffectiveTo.HasValue && config.EffectiveTo < config.EffectiveFrom)
            throw new InvalidOperationException("Hiệu lực đến phải sau hiệu lực từ.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var existing = config.Id > 0 ? await db.SignConfigs.FirstOrDefaultAsync(x => x.Id == config.Id) : null;

        // Chỉ 1 bản ghi đang hiệu lực cho mỗi cặp (SignType, OrgID) — port từ Mst_OrgSignConfig_CheckDB_MoreThanOneActive.
        if (config.Active)
        {
            var dup = await db.SignConfigs.FirstOrDefaultAsync(x =>
                x.Active && x.SignType == config.SignType && x.OrgCode == config.OrgCode && x.Id != config.Id);
            if (dup != null)
                throw new InvalidOperationException(
                    $"Đã có cấu hình ký '{config.SignTypeLabel}' đang hiệu lực cho tổ chức '{config.OrgCode}'.");
        }

        if (existing == null)
        {
            config.CreatedBy = who;
            config.CreatedAt = DateTime.Now;
            db.SignConfigs.Add(config);
            existing = config;
        }
        else
        {
            // Cập nhật từng phần — port từ Mst_OrgSignConfig_UpdateX (Ft_Cols_Upd).
            existing.SignType = config.SignType;
            existing.NetworkID = config.NetworkID;
            existing.OrgCode = config.OrgCode;
            existing.CANumber = config.CANumber;
            existing.CAOrg = config.CAOrg;
            existing.EffectiveFrom = config.EffectiveFrom;
            existing.EffectiveTo = config.EffectiveTo;
            existing.ServerSignFilePath = config.ServerSignFilePath;
            existing.ServerSignPassword = config.ServerSignPassword;
            existing.SupplierCode = config.SupplierCode;
            existing.RemoteSignAgreementUUID = config.RemoteSignAgreementUUID;
            existing.RemoteSignPassCode = config.RemoteSignPassCode;
            existing.AuthenCode = config.AuthenCode;
            existing.Active = config.Active;
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = who;
        }
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa cấu hình ký — port từ Mst_OrgSignConfig_DeleteX (phải tồn tại mới xóa được).
    public async Task<(bool ok, string msg)> DeleteSignConfigAsync(int id, string actor)
    {
        var cfg = await db.SignConfigs.FirstOrDefaultAsync(x => x.Id == id);
        if (cfg == null) return (false, "Không tìm thấy cấu hình ký.");
        db.SignConfigs.Remove(cfg);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa cấu hình ký '{cfg.SignTypeLabel}' của tổ chức '{cfg.OrgCode}'.");
    }

    // ── File hợp đồng (Contract_Contract_UpdateFilePath) ─────────────
    // Nguồn QContract: WAS_Contract_Contract_UpdateFilePath → Contract_Contract_UpdateFilePathX.
    // Luật cốt lõi: ContractFileName bắt buộc (nếu rỗng → lỗi Contract_Contract_SaveX_Invalid_ContractFileName);
    // hợp đồng phải tồn tại; cập nhật ContractFilePath + LogLUDTimeUTC/LogLUBy.
    public async Task<(bool ok, string msg)> UpdateFileAsync(int contractId, string fileName, string? filePath, string? fileVersion, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (string.IsNullOrWhiteSpace(fileName))
            return (false, "Cần tên file hợp đồng (ContractFileName).");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        c.FileName = fileName.Trim();
        c.FilePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath.Trim();
        c.FileVersion = string.IsNullOrWhiteSpace(fileVersion) ? null : fileVersion.Trim();
        c.FileUpdatedAt = DateTime.Now;
        c.FileUpdatedBy = who;
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Remark, who,
            $"Cập nhật file hợp đồng: {c.FileName}"
            + (c.FileVersion != null ? $" (phiên bản {c.FileVersion})" : ""));
        return (true, $"Đã cập nhật file hợp đồng {c.Code} — {c.FileName}.");
    }

    // ── Cấu hình loại hợp đồng (Mst_ContractTypeDtl) ─────────────────
    // Nguồn QContract: Mst_ContractTypeDtl_GetX (đọc cấu hình theo loại) +
    // Mst_ContractTypeDtl_SaveX (lưu cấu hình: kênh gửi HĐ + kênh gửi OTP + tự sinh số).
    public Task<List<ContractTypeConfig>> TypeConfigsAsync() =>
        db.TypeConfigs.Include(x => x.Type).OrderBy(x => x.TypeId).ToListAsync();

    // Lưu (thêm/cập nhật) cấu hình cho 1 loại hợp đồng — mỗi loại chỉ có 1 cấu hình.
    // Luật cốt lõi (Mst_ContractTypeDtl_SaveX): loại hợp đồng phải tồn tại & đang hiệu lực;
    // khi tạo cấu hình của loại KHÔNG được trùng; khi sửa thì cập nhật từng phần.
    public async Task<ContractTypeConfig> SaveTypeConfigAsync(ContractTypeConfig config, string actor)
    {
        var type = await db.ContractTypes.FirstOrDefaultAsync(t => t.Id == config.TypeId)
            ?? throw new InvalidOperationException("Không tìm thấy loại hợp đồng.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var existing = await db.TypeConfigs.FirstOrDefaultAsync(x => x.TypeId == config.TypeId);
        if (existing == null)
        {
            config.CreatedBy = who;
            config.CreatedAt = DateTime.Now;
            db.TypeConfigs.Add(config);
            existing = config;
        }
        else
        {
            // Cập nhật từng phần — port từ Mst_ContractTypeDtl_SaveX (Ft_Cols_Upd).
            existing.GenContractNo = config.GenContractNo;
            existing.EmailContract = config.EmailContract;
            existing.SmsContract = config.SmsContract;
            existing.ZaloContract = config.ZaloContract;
            existing.EmailOtp = config.EmailOtp;
            existing.SmsOtp = config.SmsOtp;
            existing.ZaloOtp = config.ZaloOtp;
            existing.Active = config.Active;
            existing.Remark = config.Remark;
        }
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa cấu hình loại hợp đồng — port từ Mst_ContractTypeDtl_SaveX (bIsDelete).
    public async Task<(bool ok, string msg)> DeleteTypeConfigAsync(int id, string actor)
    {
        var cfg = await db.TypeConfigs.Include(x => x.Type).FirstOrDefaultAsync(x => x.Id == id);
        if (cfg == null) return (false, "Không tìm thấy cấu hình loại hợp đồng.");
        db.TypeConfigs.Remove(cfg);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa cấu hình loại '{cfg.TypeName}'.");
    }

    // ── Cấu hình kênh gửi của tổ chức (Mst_Channel) ──────────────────
    // Nguồn QContract: Mst_Channel_SaveX_New20240312 + Mst_Channel_CheckDB +
    // Mst_ChannelType_CheckDB. Khóa nghiệp vụ là OrgID (mỗi tổ chức 1 cấu hình kênh).
    public Task<ChannelConfig?> ChannelConfigAsync() =>
        db.ChannelConfigs.Include(x => x.Email).Include(x => x.Sms).Include(x => x.Zalo)
          .FirstOrDefaultAsync();

    // Lưu cấu hình kênh gửi — upsert theo OrgID (chưa có thì tạo, đã có thì cập nhật).
    // Luật cốt lõi: ChannelTypeContract/ChannelTypeOTP phải tồn tại & đang hiệu lực;
    // ChannelTypeAccessKey luôn bị ép về EMAIL.
    public async Task<ChannelConfig> SaveChannelConfigAsync(ChannelConfig config, string actor)
    {
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var existing = await db.ChannelConfigs
            .Include(x => x.Email).Include(x => x.Sms).Include(x => x.Zalo)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            config.AccessKeyChannel = ChannelType.Email;   // luôn EMAIL
            config.CreatedBy = who;
            config.CreatedAt = DateTime.Now;
            db.ChannelConfigs.Add(config);
            await db.SaveChangesAsync();
            return config;
        }

        // Cập nhật từng phần — port từ Mst_Channel_SaveX_New20240312 (Ft_Cols_Upd).
        existing.NetworkID = config.NetworkID;
        existing.ContractChannel = config.ContractChannel;
        existing.OtpChannel = config.OtpChannel;
        existing.AccessKeyChannel = ChannelType.Email;     // luôn EMAIL
        existing.Active = config.Active;

        // Cấu hình con Email (Mst_ChannelEmail) — tạo mới nếu chưa có.
        if (config.Email != null)
        {
            if (existing.Email == null)
            {
                config.Email.ChannelConfigId = existing.Id;
                config.Email.CreatedBy = who;
                db.ChannelEmails.Add(config.Email);
            }
            else
            {
                existing.Email.SubFormCodeEmailContract = config.Email.SubFormCodeEmailContract;
                existing.Email.SubFormCodeEmailOtp = config.Email.SubFormCodeEmailOtp;
                existing.Email.SubFormCodeEmailAccessKey = config.Email.SubFormCodeEmailAccessKey;
                existing.Email.MailFrom = config.Email.MailFrom;
                existing.Email.APIsSendMail = config.Email.APIsSendMail;
                existing.Email.ApiKeySendMail = config.Email.ApiKeySendMail;
                existing.Email.SolutionCodeSendMail = config.Email.SolutionCodeSendMail;
                existing.Email.DisplayNameMailFrom = config.Email.DisplayNameMailFrom;
                existing.Email.Active = config.Email.Active;
            }
        }

        // Cấu hình con SMS (Mst_ChannelSMS).
        if (config.Sms != null)
        {
            if (existing.Sms == null)
            {
                config.Sms.ChannelConfigId = existing.Id;
                config.Sms.CreatedBy = who;
                db.ChannelSms.Add(config.Sms);
            }
            else
            {
                existing.Sms.SubFormCodeContractSms = config.Sms.SubFormCodeContractSms;
                existing.Sms.SubFormCodeSmsOtp = config.Sms.SubFormCodeSmsOtp;
                existing.Sms.SubFormCodeSmsAccessKey = config.Sms.SubFormCodeSmsAccessKey;
                existing.Sms.SmsBrandName = config.Sms.SmsBrandName;
                existing.Sms.Active = config.Sms.Active;
            }
        }

        // Cấu hình con Zalo (Mst_ChannelZalo).
        if (config.Zalo != null)
        {
            if (existing.Zalo == null)
            {
                config.Zalo.ChannelConfigId = existing.Id;
                config.Zalo.CreatedBy = who;
                db.ChannelZalo.Add(config.Zalo);
            }
            else
            {
                existing.Zalo.SubFormCodeContractZaloUserId = config.Zalo.SubFormCodeContractZaloUserId;
                existing.Zalo.SubFormCodeContractPhone = config.Zalo.SubFormCodeContractPhone;
                existing.Zalo.SubFormCodeOtp = config.Zalo.SubFormCodeOtp;
                existing.Zalo.SubFormCodeAccessKeyZaloUserId = config.Zalo.SubFormCodeAccessKeyZaloUserId;
                existing.Zalo.SubFormCodeAccessKeyPhone = config.Zalo.SubFormCodeAccessKeyPhone;
                existing.Zalo.AppId = config.Zalo.AppId;
                existing.Zalo.ZaloOaId = config.Zalo.ZaloOaId;
                existing.Zalo.RefreshToken = config.Zalo.RefreshToken;
                existing.Zalo.AccessToken = config.Zalo.AccessToken;
                existing.Zalo.AppSecret = config.Zalo.AppSecret;
                existing.Zalo.AccessCode = config.Zalo.AccessCode;
                existing.Zalo.Active = config.Zalo.Active;
            }
        }

        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa cấu hình kênh gửi — port từ Mst_Channel_SaveX_New20240312 (bIsDelete).
    public async Task<(bool ok, string msg)> DeleteChannelConfigAsync(int id, string actor)
    {
        var cfg = await db.ChannelConfigs
            .Include(x => x.Email).Include(x => x.Sms).Include(x => x.Zalo)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (cfg == null) return (false, "Không tìm thấy cấu hình kênh gửi.");
        db.ChannelConfigs.Remove(cfg);
        await db.SaveChangesAsync();
        return (true, "Đã xóa cấu hình kênh gửi.");
    }

    // ── Mẫu nội dung gửi (Mst_SubmissionForm) ────────────────────────
    // Nguồn QContract: Mst_SubmissionForm_CheckDB + Mst_SubmissionForm_SaveX.
    // Luật cốt lõi: SubFormCode bắt buộc & KHÔNG trùng khi tạo; ChannelType/BulletinType
    // phải tồn tại & đang hiệu lực; lưu là GHI ĐÈ toàn bộ nội dung (Message) + tham số ZNS
    // của mẫu (delete all + insert all); xóa mẫu thì xóa kèm nội dung + tham số ZNS.
    public async Task<List<SubmissionForm>> SubmissionFormsAsync(bool activeOnly = false)
    {
        var q = db.SubmissionForms.Include(x => x.Messages).Include(x => x.ZnsParams).AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.SubFormCode).ToListAsync();
    }

    public Task<SubmissionForm?> SubmissionFormAsync(int id) =>
        db.SubmissionForms.Include(x => x.Messages).Include(x => x.ZnsParams)
          .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<SubmissionForm> SaveSubmissionFormAsync(SubmissionForm form,
        List<SubmissionFormMessage> messages, List<SubmissionFormZns> znsParams, string actor)
    {
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor.Trim();
        var code = (form.SubFormCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Cần mã mẫu gửi (SubFormCode).");
        if (string.IsNullOrWhiteSpace(form.SubFormName))
            throw new InvalidOperationException("Cần tên mẫu gửi.");
        form.SubFormCode = code;

        var existing = await db.SubmissionForms
            .Include(x => x.Messages).Include(x => x.ZnsParams)
            .FirstOrDefaultAsync(x => x.SubFormCode == code);

        // Chuẩn hoá nội dung mẫu (bỏ dòng rỗng) + tham số ZNS (bỏ dòng thiếu mã tham số).
        var cleanMsgs = (messages ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m.Message) || !string.IsNullOrWhiteSpace(m.SubTitle))
            .Select(m => new SubmissionFormMessage
            {
                SubFormCode = code, SubTitle = string.IsNullOrWhiteSpace(m.SubTitle) ? null : m.SubTitle.Trim(),
                Message = (m.Message ?? "").Trim(), Active = true, CreatedBy = who
            }).ToList();
        var cleanZns = (znsParams ?? [])
            .Where(z => !string.IsNullOrWhiteSpace(z.ParamContractCode))
            .Select(z => new SubmissionFormZns
            {
                SubFormCode = code, ParamContractCodeZns = (z.ParamContractCodeZns ?? "").Trim(),
                SourceDataType = string.IsNullOrWhiteSpace(z.SourceDataType) ? null : z.SourceDataType.Trim(),
                ParamContractCode = z.ParamContractCode.Trim(),
                ParamValue = string.IsNullOrWhiteSpace(z.ParamValue) ? null : z.ParamValue.Trim(),
                Active = true, CreatedBy = who
            }).ToList();

        if (existing == null)
        {
            // Tạo mới — SubFormCode chưa tồn tại (FlagExistToCheck=No).
            form.CreatedBy = who;
            form.CreatedAt = DateTime.Now;
            form.Messages = cleanMsgs;
            form.ZnsParams = cleanZns;
            db.SubmissionForms.Add(form);
            await db.SaveChangesAsync();
            return form;
        }

        // Cập nhật — SubFormCode đã tồn tại (FlagExistToCheck=Yes).
        existing.SubFormName = form.SubFormName.Trim();
        existing.ChannelType = form.ChannelType;
        existing.BulletinType = form.BulletinType;
        existing.IdZns = string.IsNullOrWhiteSpace(form.IdZns) ? null : form.IdZns.Trim();
        existing.Active = form.Active;

        // Ghi đè toàn bộ nội dung + tham số ZNS cũ của mẫu (delete all + insert all).
        db.SubmissionFormMessages.RemoveRange(existing.Messages);
        db.SubmissionFormZns.RemoveRange(existing.ZnsParams);
        existing.Messages = cleanMsgs;
        existing.ZnsParams = cleanZns;
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa mẫu gửi — port từ Mst_SubmissionForm_SaveX (bIsDelete): xóa kèm nội dung + tham số ZNS.
    public async Task<(bool ok, string msg)> DeleteSubmissionFormAsync(int id, string actor)
    {
        var form = await db.SubmissionForms
            .Include(x => x.Messages).Include(x => x.ZnsParams)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (form == null) return (false, "Không tìm thấy mẫu gửi.");
        db.SubmissionFormMessages.RemoveRange(form.Messages);
        db.SubmissionFormZns.RemoveRange(form.ZnsParams);
        db.SubmissionForms.Remove(form);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa mẫu gửi '{form.SubFormCode}'.");
    }

    // ── Chi tiết hợp đồng (Contract_ContractDtl) ─────────────────────
    // Nguồn QContract: Contract_ContractDtl (insert trong Contract_Contract_SaveX) +
    // luồng tính toán ở Website Contract_Contract/SignMulti.cshtml.
    public Task<List<ContractDetail>> DetailsAsync(int contractId) =>
        db.Details.Where(x => x.ContractId == contractId)
          .OrderBy(x => x.Idx).ThenBy(x => x.Id).ToListAsync();

    // Thêm 1 dòng chi tiết (hàng hóa/dịch vụ) — port từ Contract_ContractDtl (QContract).
    // Luật cốt lõi: hợp đồng phải tồn tại và chưa kết thúc; tên hàng hóa bắt buộc;
    // các giá trị tiền được TÍNH LẠI theo công thức QContract (không tin số client gửi lên):
    //  - ValContract = Qty × UnitPrice;
    //  - ValDiscount = ValContract × DiscountRate / 100;
    //  - ValTax = (ValContract − ValDiscount) × VATRate / 100.
    public async Task<ContractDetail> AddDetailAsync(int contractId, ContractDetail detail, string actor)
    {
        var c = await db.Contracts.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            throw new InvalidOperationException("Hợp đồng đã hủy hoặc đã kết thúc, không thêm chi tiết.");
        if (string.IsNullOrWhiteSpace(detail.SpecName))
            throw new InvalidOperationException("Cần tên hàng hóa/dịch vụ.");

        // Tính lại các giá trị tiền theo công thức QContract (SignMulti.cshtml).
        var qty = detail.Qty <= 0 ? 1 : detail.Qty;
        var valContract = qty * detail.UnitPrice;
        var valDiscount = valContract * detail.DiscountRate / 100m;
        var valTax = (valContract - valDiscount) * detail.VATRate / 100m;

        detail.ContractId = c.Id;
        detail.Qty = qty;
        detail.ValContract = valContract;
        detail.ValDiscount = valDiscount;
        detail.ValTax = valTax;
        if (string.IsNullOrWhiteSpace(detail.SpecCode)) detail.SpecCode = "SP" + Guid.NewGuid().ToString("N")[..6].ToUpper();
        if (detail.Idx <= 0) detail.Idx = c.Details.Count + 1;
        detail.CreatedBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        db.Details.Add(detail);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, detail.CreatedBy,
            $"Thêm dòng chi tiết '{detail.SpecName}' — SL {detail.Qty:N0} × {detail.UnitPrice:N0} đ = {detail.ValContract:N0} đ"
            + (detail.ValTax > 0 ? $", thuế {detail.ValTax:N0} đ" : ""));
        return detail;
    }

    // Xóa 1 dòng chi tiết — port từ Contract_ContractDtl (QContract).
    public async Task<(bool ok, string msg)> DeleteDetailAsync(int contractId, int detailId, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không xóa chi tiết.");
        var d = await db.Details.FirstOrDefaultAsync(x => x.Id == detailId && x.ContractId == contractId);
        if (d == null) return (false, "Không tìm thấy dòng chi tiết.");
        db.Details.Remove(d);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor,
            $"Xóa dòng chi tiết '{d.SpecName}'");
        return (true, $"Đã xóa dòng chi tiết '{d.SpecName}'.");
    }

    // Thống kê chi tiết hợp đồng — port từ Contract_ContractDtl (QContract).
    public async Task<DetailStats> DetailStatsAsync(int contractId)
    {
        var ds = await db.Details.Where(x => x.ContractId == contractId).ToListAsync();
        var total = ds.Sum(d => d.ValContract);
        var tax = ds.Sum(d => d.ValTax);
        var discount = ds.Sum(d => d.ValDiscount);
        return new DetailStats(ds.Count, total, tax, discount, total - discount + tax);
    }

    // ── Trường động của hợp đồng (Contract_Attribute_Contract) ───────
    // Nguồn QContract: Mst_Attribute_Contract_CheckDB / _CreateX / _UpdateX / _DeleteX
    // + Contract_Contract_SaveX (lưu trường động cấp hợp đồng/chi tiết).
    public async Task<List<AttributeContract>> AttributeMastersAsync(bool activeOnly = false)
    {
        var q = db.AttributeContracts.AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.Code).ToListAsync();
    }

    // Lưu 1 trường động (danh mục) — port từ Mst_Attribute_Contract_CreateX / _UpdateX.
    // Luật cốt lõi: mã trường bắt buộc; khi tạo mã KHÔNG được trùng; khi sửa mã phải tồn tại.
    public async Task<AttributeContract> SaveAttributeMasterAsync(AttributeContract master, string actor)
    {
        if (string.IsNullOrWhiteSpace(master.Code))
            throw new InvalidOperationException("Cần mã trường động (AttributeContractCode).");
        master.Code = master.Code.Trim();
        master.Name = string.IsNullOrWhiteSpace(master.Name) ? master.Code : master.Name.Trim();
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;

        if (master.Id > 0)
        {
            var existing = await db.AttributeContracts.FirstOrDefaultAsync(x => x.Id == master.Id)
                ?? throw new InvalidOperationException("Không tìm thấy trường động.");
            // Mã trường không được trùng với bản ghi khác.
            if (await db.AttributeContracts.AnyAsync(x => x.Id != master.Id && x.Code == master.Code))
                throw new InvalidOperationException($"Mã trường động '{master.Code}' đã tồn tại.");
            existing.Code = master.Code;
            existing.Name = master.Name;
            existing.DefaultValues = master.DefaultValues;
            existing.Active = master.Active;
            await db.SaveChangesAsync();
            return existing;
        }

        // Tạo mới: mã trường KHÔNG được trùng (FlagExistToCheck = No).
        if (await db.AttributeContracts.AnyAsync(x => x.Code == master.Code))
            throw new InvalidOperationException($"Mã trường động '{master.Code}' đã tồn tại.");
        master.CreatedBy = who;
        db.AttributeContracts.Add(master);
        await db.SaveChangesAsync();
        return master;
    }

    public async Task<(bool ok, string msg)> DeleteAttributeMasterAsync(int id, string actor)
    {
        var m = await db.AttributeContracts.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return (false, "Không tìm thấy trường động.");
        db.AttributeContracts.Remove(m);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa trường động '{m.Name}'.");
    }

    public Task<List<ContractAttribute>> AttributesAsync(int contractId) =>
        db.ContractAttributes.Where(x => x.ContractId == contractId)
          .OrderBy(x => x.Id).ToListAsync();

    // Lưu trường động cấp hợp đồng — GHI ĐÈ TOÀN BỘ (delete all + insert all), đúng như
    // Contract_Contract_SaveX (QContract). Mỗi trường: mã bắt buộc + phải tồn tại & đang hiệu lực
    // trong Mst_Attribute_Contract; giá trị bắt buộc (không rỗng).
    public async Task<(bool ok, string msg)> SaveAttributesAsync(int contractId, List<ContractAttribute> attributes, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không cập nhật trường động.");

        var masters = await db.AttributeContracts.Where(x => x.Active).ToListAsync();
        var clean = new List<ContractAttribute>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in attributes ?? [])
        {
            var code = (a.AttributeContractCode ?? "").Trim();
            var value = (a.AttributeValue ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(value)) continue;
            if (string.IsNullOrWhiteSpace(code))
                return (false, "Mỗi trường động phải có mã trường (AttributeContractCode).");
            if (string.IsNullOrWhiteSpace(value))
                return (false, $"Trường động '{code}' phải có giá trị (AttributeValue).");
            var master = masters.FirstOrDefault(m => m.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (master == null)
                return (false, $"Mã trường động '{code}' không tồn tại hoặc đã ngừng hiệu lực.");
            if (!seen.Add(code)) continue;   // loại trùng theo mã trường
            clean.Add(new ContractAttribute
            {
                ContractId = c.Id, AttributeContractCode = master.Code, AttributeName = master.Name,
                AttributeValue = value, CreatedBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor
            });
        }

        var old = await db.ContractAttributes.Where(x => x.ContractId == c.Id).ToListAsync();
        db.ContractAttributes.RemoveRange(old);
        db.ContractAttributes.AddRange(clean);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor,
            $"Cập nhật trường động hợp đồng — {clean.Count} trường");
        return (true, $"Đã cập nhật trường động hợp đồng — {clean.Count} trường.");
    }

    public Task<List<ContractAttributeDtl>> AttributeDetailsAsync(int contractId) =>
        db.ContractAttributeDtls.Where(x => x.ContractId == contractId)
          .OrderBy(x => x.Idx).ThenBy(x => x.Id).ToListAsync();

    // Lưu trường động cấp chi tiết — GHI ĐÈ TOÀN BỘ (delete all + insert all).
    public async Task<(bool ok, string msg)> SaveAttributeDetailsAsync(int contractId, List<ContractAttributeDtl> attributes, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không cập nhật trường động.");

        var masters = await db.AttributeContracts.Where(x => x.Active).ToListAsync();
        var clean = new List<ContractAttributeDtl>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in attributes ?? [])
        {
            var code = (a.AttributeContractCode ?? "").Trim();
            var value = (a.AttributeValue ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(value)) continue;
            if (string.IsNullOrWhiteSpace(code))
                return (false, "Mỗi trường động phải có mã trường (AttributeContractCode).");
            if (string.IsNullOrWhiteSpace(value))
                return (false, $"Trường động '{code}' phải có giá trị (AttributeValue).");
            var master = masters.FirstOrDefault(m => m.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (master == null)
                return (false, $"Mã trường động '{code}' không tồn tại hoặc đã ngừng hiệu lực.");
            var key = $"{a.Idx}|{code}";
            if (!seen.Add(key)) continue;
            clean.Add(new ContractAttributeDtl
            {
                ContractId = c.Id, Idx = a.Idx <= 0 ? 1 : a.Idx,
                AttributeContractCode = master.Code, AttributeName = master.Name,
                AttributeValue = value, CreatedBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor
            });
        }

        var old = await db.ContractAttributeDtls.Where(x => x.ContractId == c.Id).ToListAsync();
        db.ContractAttributeDtls.RemoveRange(old);
        db.ContractAttributeDtls.AddRange(clean);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor,
            $"Cập nhật trường động chi tiết hợp đồng — {clean.Count} trường");
        return (true, $"Đã cập nhật trường động chi tiết — {clean.Count} trường.");
    }

    // ── Quản lý thông báo (Mst_NotifyType + Map_UserInNotifyType) ────
    // Nguồn QContract: Mst_NotifyType_CheckDB/_CreateX/_UpdateX/_DeleteX +
    // Map_UserInNotifyType_SaveX (ghi đè theo cặp UserCode+NotifyType).
    public async Task<List<NotifyType>> NotifyTypesAsync(bool activeOnly = false)
    {
        var q = db.NotifyTypes.Include(x => x.UserMappings).AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.NotifyTypeCode).ToListAsync();
    }

    // Lưu 1 loại thông báo — port từ Mst_NotifyType_CreateX/_UpdateX (QContract).
    // Luật cốt lõi: NotifyType bắt buộc; khi tạo KHÔNG trùng, khi sửa phải tồn tại.
    public async Task<NotifyType> SaveNotifyTypeAsync(NotifyType type, string actor)
    {
        var code = (type.NotifyTypeCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Cần mã loại thông báo (NotifyType).");
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;

        if (type.Id > 0)
        {
            var existing = await db.NotifyTypes.FirstOrDefaultAsync(x => x.Id == type.Id)
                ?? throw new InvalidOperationException($"Loại thông báo '{code}' không tồn tại.");
            // Đổi mã sang mã đã có của bản ghi khác → chặn trùng.
            if (await db.NotifyTypes.AnyAsync(x => x.Id != type.Id && x.NotifyTypeCode == code))
                throw new InvalidOperationException($"Mã loại thông báo '{code}' đã tồn tại.");
            existing.NotifyTypeCode = code;
            existing.NotifyDesc = string.IsNullOrWhiteSpace(type.NotifyDesc) ? null : type.NotifyDesc.Trim();
            existing.DefaultActive = type.DefaultActive;
            existing.Active = type.Active;
            existing.CreatedBy = who;
            await db.SaveChangesAsync();
            return existing;
        }

        // Tạo mới — NotifyType không được trùng (FlagExistToCheck = No).
        if (await db.NotifyTypes.AnyAsync(x => x.NotifyTypeCode == code))
            throw new InvalidOperationException($"Mã loại thông báo '{code}' đã tồn tại.");
        var nt = new NotifyType
        {
            NotifyTypeCode = code,
            NotifyDesc = string.IsNullOrWhiteSpace(type.NotifyDesc) ? null : type.NotifyDesc.Trim(),
            DefaultActive = type.DefaultActive, Active = type.Active, CreatedBy = who
        };
        db.NotifyTypes.Add(nt);
        await db.SaveChangesAsync();
        return nt;
    }

    // Xóa 1 loại thông báo — port từ Mst_NotifyType_DeleteX (QContract).
    // Xóa kèm các bản ghi bật/tắt thông báo (Map_UserInNotifyType) của loại đó.
    public async Task<(bool ok, string msg)> DeleteNotifyTypeAsync(int id, string actor)
    {
        var nt = await db.NotifyTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (nt == null) return (false, "Không tìm thấy loại thông báo.");
        var maps = await db.UserNotifyTypes.Where(x => x.NotifyTypeCode == nt.NotifyTypeCode).ToListAsync();
        db.UserNotifyTypes.RemoveRange(maps);
        db.NotifyTypes.Remove(nt);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa loại thông báo '{nt.NotifyTypeCode}'.");
    }

    // Danh sách bật/tắt thông báo theo người dùng — port từ Map_UserInNotifyType (QContract).
    public async Task<List<UserNotifyType>> UserNotifyTypesAsync(string? userCode = null)
    {
        var q = db.UserNotifyTypes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(userCode)) q = q.Where(x => x.UserCode == userCode.Trim());
        return await q.OrderBy(x => x.UserCode).ThenBy(x => x.NotifyTypeCode).ToListAsync();
    }

    // Lưu bật/tắt thông báo — GHI ĐÈ theo cặp (UserCode, NotifyType): xóa bản ghi trùng cặp
    // rồi insert lại — port từ Map_UserInNotifyType_SaveX (delete matching + insert all).
    public async Task<(bool ok, string msg)> SaveUserNotifyTypesAsync(List<UserNotifyType> mappings, string actor)
    {
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var clean = new List<UserNotifyType>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in mappings ?? [])
        {
            var user = (m.UserCode ?? "").Trim();
            var type = (m.NotifyTypeCode ?? "").Trim();
            if (string.IsNullOrWhiteSpace(user) && string.IsNullOrWhiteSpace(type)) continue;
            if (string.IsNullOrWhiteSpace(user))
                return (false, "Mỗi dòng phải có mã người dùng (UserCode).");
            if (string.IsNullOrWhiteSpace(type))
                return (false, $"Người dùng '{user}' phải chọn loại thông báo (NotifyType).");
            if (!seen.Add($"{user}|{type}")) continue;
            clean.Add(new UserNotifyType
            {
                UserCode = user, NotifyTypeCode = type, FlagNotify = m.FlagNotify, CreatedBy = who
            });
        }

        // Xóa các bản ghi trùng cặp (UserCode, NotifyType) rồi insert lại (delete matching + insert all).
        foreach (var c in clean)
        {
            var old = await db.UserNotifyTypes
                .Where(x => x.UserCode == c.UserCode && x.NotifyTypeCode == c.NotifyTypeCode).ToListAsync();
            db.UserNotifyTypes.RemoveRange(old);
        }
        db.UserNotifyTypes.AddRange(clean);
        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật cài đặt thông báo — {clean.Count} dòng.");
    }

    // ── Danh mục loại mẫu in (Mst_TempType) ──────────────────────────
    // Nguồn QContract: Mst_TempType_CheckDB / _CreateX / _UpdateX / _DeleteX (Temp.cs).
    public async Task<List<TempType>> TempTypesAsync(bool activeOnly = false)
    {
        var q = db.TempTypes.AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.Code).ToListAsync();
    }

    // Lưu loại mẫu in — tạo mới (TempType không trùng) hoặc cập nhật từng phần khi đã tồn tại.
    // Luật cốt lõi: TempType bắt buộc & KHÔNG trùng khi tạo (FlagExistToCheck=No); khi sửa phải
    // tồn tại (FlagExistToCheck=Yes); TempTypeName, TempSize, ImageFilePath đều bắt buộc.
    public async Task<TempType> SaveTempTypeAsync(TempType type, string actor)
    {
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var code = (type.Code ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Cần mã loại mẫu in (TempType).");
        if (string.IsNullOrWhiteSpace(type.Name))
            throw new InvalidOperationException("Cần tên loại mẫu in (TempTypeName).");
        if (string.IsNullOrWhiteSpace(type.Size))
            throw new InvalidOperationException("Cần khổ/định dạng mẫu (TempSize).");
        if (string.IsNullOrWhiteSpace(type.ImageFilePath))
            throw new InvalidOperationException("Cần đường dẫn ảnh xem trước (ImageFilePath).");

        var existing = await db.TempTypes.FirstOrDefaultAsync(x => x.Code == code);
        if (existing == null)
        {
            // Tạo mới — TempType không được trùng (FlagExistToCheck=No).
            type.Code = code;
            type.Name = type.Name.Trim();
            type.Size = type.Size.Trim();
            type.ImageFilePath = type.ImageFilePath.Trim();
            type.CreatedBy = who;
            db.TempTypes.Add(type);
            await db.SaveChangesAsync();
            return type;
        }

        // Cập nhật từng phần (FlagExistToCheck=Yes).
        existing.Name = type.Name.Trim();
        existing.Description = type.Description;
        existing.Size = type.Size.Trim();
        existing.ImageFilePath = type.ImageFilePath.Trim();
        existing.Remark = type.Remark;
        existing.Active = type.Active;
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa loại mẫu in — phải tồn tại (FlagExistToCheck=Yes).
    public async Task<(bool ok, string msg)> DeleteTempTypeAsync(int id, string actor)
    {
        var t = await db.TempTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return (false, "Không tìm thấy loại mẫu in.");
        db.TempTypes.Remove(t);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa loại mẫu in '{t.Name}'.");
    }

    // ── Tỷ giá ngoại tệ (Mst_CurrencyEx) ─────────────────────────────
    // Nguồn QContract: Mst_CurrencyEx_CheckDB / _Create / _Update / _Delete (Master.cs).
    public Task<List<CurrencyExchange>> CurrenciesAsync() =>
        db.Currencies.OrderBy(x => x.CurrencyCode).ToListAsync();

    // Lưu tỷ giá ngoại tệ — tạo mới (CurrencyCode không trùng) hoặc cập nhật từng phần khi đã tồn tại.
    // Luật cốt lõi: CurrencyCode bắt buộc & KHÔNG trùng khi tạo (FlagExistToCheck=No); khi sửa phải
    // tồn tại (FlagExistToCheck=Yes); CurrencyName bắt buộc; nếu có BaseCurrencyCode thì đồng tiền
    // gốc phải tồn tại (Mst_CurrencyEx_CheckDB).
    public async Task<CurrencyExchange> SaveCurrencyAsync(CurrencyExchange currency, string actor)
    {
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var code = (currency.CurrencyCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Cần mã ngoại tệ (CurrencyCode).");
        if (string.IsNullOrWhiteSpace(currency.CurrencyName))
            throw new InvalidOperationException("Cần tên ngoại tệ (CurrencyName).");

        var baseCode = (currency.BaseCurrencyCode ?? "").Trim();
        var existing = await db.Currencies.FirstOrDefaultAsync(x => x.CurrencyCode == code);
        if (existing == null)
        {
            // Tạo mới — CurrencyCode không được trùng (FlagExistToCheck=No).
            // Nếu có đồng tiền gốc thì phải tồn tại (Mst_CurrencyEx_CheckDB, FlagExistToCheck=Yes).
            if (!string.IsNullOrEmpty(baseCode) &&
                !await db.Currencies.AnyAsync(x => x.CurrencyCode == baseCode))
                throw new InvalidOperationException($"Đồng tiền gốc '{baseCode}' không tồn tại.");

            currency.CurrencyCode = code;
            currency.CurrencyName = currency.CurrencyName.Trim();
            currency.BaseCurrencyCode = string.IsNullOrEmpty(baseCode) ? null : baseCode;
            currency.UpdatedTime = DateTime.Now;
            currency.CreatedBy = who;
            db.Currencies.Add(currency);
            await db.SaveChangesAsync();
            return currency;
        }

        // Cập nhật từng phần (FlagExistToCheck=Yes).
        if (!string.IsNullOrEmpty(baseCode) &&
            !await db.Currencies.AnyAsync(x => x.CurrencyCode == baseCode))
            throw new InvalidOperationException($"Đồng tiền gốc '{baseCode}' không tồn tại.");

        existing.CurrencyName = currency.CurrencyName.Trim();
        existing.BaseCurrencyCode = string.IsNullOrEmpty(baseCode) ? null : baseCode;
        existing.BuyRate = currency.BuyRate;
        existing.SellRate = currency.SellRate;
        existing.InterEx = currency.InterEx;
        existing.Remark = currency.Remark;
        existing.UpdatedTime = DateTime.Now;
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa tỷ giá ngoại tệ — phải tồn tại (FlagExistToCheck=Yes).
    public async Task<(bool ok, string msg)> DeleteCurrencyAsync(int id, string actor)
    {
        var c = await db.Currencies.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return (false, "Không tìm thấy tỷ giá ngoại tệ.");
        db.Currencies.Remove(c);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa tỷ giá '{c.CurrencyCode}'.");
    }

    // ── File đính kèm hợp đồng (Contract_ContractFiles) ──────────────
    // Nguồn QContract: Contract_ContractFiles (insert trong Contract_Contract_SaveX) +
    // luồng Website Contract_ContractController (model.FileList → Lst_Contract_ContractFiles).
    public Task<List<ContractAttachment>> AttachmentsAsync(int contractId) =>
        db.Attachments.Where(x => x.ContractId == contractId)
          .OrderBy(x => x.Idx).ThenBy(x => x.Id).ToListAsync();

    // Thêm 1 file đính kèm cho hợp đồng — port từ Contract_ContractFiles (QContract).
    // Luật cốt lõi: hợp đồng phải tồn tại và chưa hủy/kết thúc; tên file (ContractFileName) bắt buộc;
    // thứ tự (Idx) tự tăng theo số file hiện có; ghi nhật ký thao tác.
    public async Task<(bool ok, string msg)> AddAttachmentAsync(int contractId, ContractAttachment attachment, string actor)
    {
        var c = await db.Contracts.Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không thêm file đính kèm.");
        if (string.IsNullOrWhiteSpace(attachment.FileName))
            return (false, "Cần tên file (ContractFileName).");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        attachment.ContractId = c.Id;
        attachment.FileName = attachment.FileName.Trim();
        attachment.FilePath = string.IsNullOrWhiteSpace(attachment.FilePath) ? null : attachment.FilePath.Trim();
        if (attachment.Idx <= 0) attachment.Idx = c.Attachments.Count + 1;
        attachment.CreatedBy = who;
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, who,
            $"Thêm file đính kèm '{attachment.FileName}' ({attachment.RefDocTypeLabel}, {attachment.PublicLabel})");
        return (true, $"Đã thêm file đính kèm '{attachment.FileName}'.");
    }

    // Xóa 1 file đính kèm — port từ Contract_ContractFiles (QContract).
    public async Task<(bool ok, string msg)> DeleteAttachmentAsync(int id, string actor)
    {
        var a = await db.Attachments.FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return (false, "Không tìm thấy file đính kèm.");
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == a.ContractId);
        if (c != null && c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không xóa file đính kèm.");
        db.Attachments.Remove(a);
        await db.SaveChangesAsync();
        if (c != null)
            await LogAsync(c.Id, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor,
                $"Xóa file đính kèm '{a.FileName}'");
        return (true, $"Đã xóa file đính kèm '{a.FileName}'.");
    }

    // Thống kê file đính kèm hợp đồng — port từ Contract_ContractFiles (QContract).
    public async Task<AttachmentStats> AttachmentStatsAsync(int contractId)
    {
        var list = await db.Attachments.Where(x => x.ContractId == contractId).ToListAsync();
        return new AttachmentStats(list.Count, list.Count(a => a.IsPublic),
            list.Count(a => !a.IsPublic), list.Count(a => a.HasFile));
    }

    // ── Tham số hệ thống (Mst_Param) ─────────────────
    // Nguồn QContract: Mst_Param_CheckDB / _Create / _Update / _Delete (MasterData.cs).
    public Task<List<SystemParam>> SystemParamsAsync() =>
        db.SystemParams.OrderBy(x => x.ParamCode).ToListAsync();

    // Lưu tham số hệ thống — tạo mới (ParamCode không trùng) hoặc cập nhật từng phần khi đã tồn tại.
    // Luật cốt lõi: ParamCode bắt buộc & KHÔNG trùng khi tạo (FlagExistToCheck=No); khi sửa phải
    // tồn tại (FlagExistToCheck=Yes); sửa là cập nhật từng phần (ParamValue/NetworkID).
    public async Task<SystemParam> SaveSystemParamAsync(SystemParam param, string actor)
    {
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var code = (param.ParamCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Cần mã tham số hệ thống (ParamCode).");

        var existing = await db.SystemParams.FirstOrDefaultAsync(x => x.ParamCode == code);
        if (existing == null)
        {
            // Tạo mới — ParamCode không được trùng (FlagExistToCheck=No).
            param.ParamCode = code;
            param.ParamValue = (param.ParamValue ?? "").Trim();
            param.NetworkID = string.IsNullOrWhiteSpace(param.NetworkID) ? null : param.NetworkID.Trim();
            param.CreatedBy = who;
            db.SystemParams.Add(param);
            await db.SaveChangesAsync();
            return param;
        }

        // Cập nhật từng phần (FlagExistToCheck=Yes).
        existing.ParamValue = (param.ParamValue ?? "").Trim();
        existing.NetworkID = string.IsNullOrWhiteSpace(param.NetworkID) ? null : param.NetworkID.Trim();
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa tham số hệ thống — phải tồn tại (FlagExistToCheck=Yes).
    public async Task<(bool ok, string msg)> DeleteSystemParamAsync(int id, string actor)
    {
        var p = await db.SystemParams.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy tham số hệ thống.");
        db.SystemParams.Remove(p);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa tham số '{p.ParamCode}'.");
    }

    // ── Tham số riêng (Mst_ParamPrivate) ─────────────
    // Nguồn QContract: Mst_ParamPrivate_CheckDB / _Create / _Update / _Delete (MasterData.cs).
    public Task<List<PrivateParam>> PrivateParamsAsync() =>
        db.PrivateParams.OrderBy(x => x.ParamCode).ToListAsync();

    // Lưu tham số riêng — tạo mới (ParamCode không trùng) hoặc cập nhật từng phần khi đã tồn tại.
    // Luật cốt lõi: ParamCode bắt buộc & KHÔNG trùng khi tạo (FlagExistToCheck=No); khi sửa phải
    // tồn tại (FlagExistToCheck=Yes); sửa là cập nhật từng phần (ParamValue/NetworkID).
    public async Task<PrivateParam> SavePrivateParamAsync(PrivateParam param, string actor)
    {
        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        var code = (param.ParamCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Cần mã tham số riêng (ParamCode).");

        var existing = await db.PrivateParams.FirstOrDefaultAsync(x => x.ParamCode == code);
        if (existing == null)
        {
            // Tạo mới — ParamCode không được trùng (FlagExistToCheck=No).
            param.ParamCode = code;
            param.ParamValue = (param.ParamValue ?? "").Trim();
            param.NetworkID = string.IsNullOrWhiteSpace(param.NetworkID) ? null : param.NetworkID.Trim();
            param.CreatedBy = who;
            db.PrivateParams.Add(param);
            await db.SaveChangesAsync();
            return param;
        }

        // Cập nhật từng phần (FlagExistToCheck=Yes).
        existing.ParamValue = (param.ParamValue ?? "").Trim();
        existing.NetworkID = string.IsNullOrWhiteSpace(param.NetworkID) ? null : param.NetworkID.Trim();
        await db.SaveChangesAsync();
        return existing;
    }

    // Xóa tham số riêng — phải tồn tại (FlagExistToCheck=Yes).
    public async Task<(bool ok, string msg)> DeletePrivateParamAsync(int id, string actor)
    {
        var p = await db.PrivateParams.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return (false, "Không tìm thấy tham số riêng.");
        db.PrivateParams.Remove(p);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa tham số '{p.ParamCode}'.");
    }

    // Sinh chuỗi hex ngẫu nhiên độ dài n — port từ CUtils.GetRandomHexNumber (QContract).
    private static string RandomHex(int n)
    {
        var bytes = new byte[(n + 1) / 2];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes)[..n].ToLowerInvariant();
    }

    private async Task<(Contract? c, ContractParty? p, string? err)> LoadForSign(int contractId, int partyId)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (null, null, "Không tìm thấy hợp đồng.");
        if (c.Status is not (ContractStatus.Sent or ContractStatus.PartiallySigned))
            return (null, null, "Hợp đồng chưa được gửi ký hoặc đã kết thúc.");
        var p = c.Parties.FirstOrDefault(x => x.Id == partyId);
        if (p == null) return (null, null, "Không tìm thấy bên tham gia.");
        if (p.HasSigned) return (null, null, $"{p.Name} đã ký rồi.");
        return (c, p, null);
    }

    private static void MarkSigned(Contract c, ContractParty p)
    {
        p.HasSigned = true;
        p.SignedAt = DateTime.Now;
        var allSigned = c.Parties.All(x => x.HasSigned);
        if (allSigned) { c.Status = ContractStatus.Completed; c.CompletedAt = DateTime.Now; }
        else c.Status = ContractStatus.PartiallySigned;
    }

    // Ghi 1 dòng nhật ký thao tác (audit trail) — port từ Contract_Contract_HistAction (QContract).
    private async Task LogAsync(int contractId, HistoryAction action, string actor, string description)
    {
        db.Histories.Add(new ContractHistory
        {
            ContractId = contractId, Action = action,
            Actor = string.IsNullOrWhiteSpace(actor) ? "system" : actor,
            Description = description
        });
        await db.SaveChangesAsync();
    }
}
