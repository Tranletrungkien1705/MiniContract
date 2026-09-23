using Microsoft.EntityFrameworkCore;
using MiniContract.Models;

namespace MiniContract.Data;

public class AppDbContext : DbContext
{
    private readonly Guid _orgId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options)
        => _orgId = tenant.OrgId;

    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<ContractType> ContractTypes => Set<ContractType>();
    public DbSet<ContractTemplate> Templates => Set<ContractTemplate>();
    public DbSet<ContractTemplateGroup> TemplateGroups => Set<ContractTemplateGroup>();
    public DbSet<ContractAttributeGroup> AttributeGroups => Set<ContractAttributeGroup>();
    public DbSet<ContractNumberRule> NumberRules => Set<ContractNumberRule>();
    public DbSet<ContractTypeConfig> TypeConfigs => Set<ContractTypeConfig>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractParty> Parties => Set<ContractParty>();
    public DbSet<ContractSignature> Signatures => Set<ContractSignature>();
    public DbSet<ContractHistory> Histories => Set<ContractHistory>();
    public DbSet<ContractSignLink> SignLinks => Set<ContractSignLink>();
    public DbSet<ContractElement> Elements => Set<ContractElement>();
    public DbSet<ContractChecker> Checkers => Set<ContractChecker>();
    public DbSet<ContractUserInContract> UserAssignments => Set<ContractUserInContract>();
    public DbSet<ContractSigner> Signers => Set<ContractSigner>();
    public DbSet<ContractSendHist> SendHistory => Set<ContractSendHist>();
    public DbSet<ContractDetail> Details => Set<ContractDetail>();
    public DbSet<AttributeContract> AttributeContracts => Set<AttributeContract>();
    public DbSet<ContractAttribute> ContractAttributes => Set<ContractAttribute>();
    public DbSet<ContractAttributeDtl> ContractAttributeDtls => Set<ContractAttributeDtl>();
    public DbSet<ContractAttachment> Attachments => Set<ContractAttachment>();
    public DbSet<FinishedContractReason> FinishReasons => Set<FinishedContractReason>();
    public DbSet<ContractVerifyOtp> VerifyOtps => Set<ContractVerifyOtp>();
    public DbSet<OrgCertificate> OrgCertificates => Set<OrgCertificate>();
    public DbSet<OrgSignConfig> SignConfigs => Set<OrgSignConfig>();
    public DbSet<ChannelConfig> ChannelConfigs => Set<ChannelConfig>();
    public DbSet<ChannelEmailConfig> ChannelEmails => Set<ChannelEmailConfig>();
    public DbSet<ChannelSmsConfig> ChannelSms => Set<ChannelSmsConfig>();
    public DbSet<ChannelZaloConfig> ChannelZalo => Set<ChannelZaloConfig>();
    public DbSet<SubmissionForm> SubmissionForms => Set<SubmissionForm>();
    public DbSet<SubmissionFormMessage> SubmissionFormMessages => Set<SubmissionFormMessage>();
    public DbSet<SubmissionFormZns> SubmissionFormZns => Set<SubmissionFormZns>();
    public DbSet<NotifyType> NotifyTypes => Set<NotifyType>();
    public DbSet<UserNotifyType> UserNotifyTypes => Set<UserNotifyType>();
    public DbSet<TempType> TempTypes => Set<TempType>();
    public DbSet<CurrencyExchange> Currencies => Set<CurrencyExchange>();
    public DbSet<SystemParam> SystemParams => Set<SystemParam>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("minicontract");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();

        b.Entity<ContractType>(e =>
        {
            e.Ignore(x => x.InUse);
            e.Ignore(x => x.ActiveLabel);
            e.Ignore(x => x.TemplateCount);
            e.Ignore(x => x.ContractCount);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractTemplate>(e =>
        {
            e.Ignore(x => x.TypeName);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractTemplateGroup>(e =>
        {
            e.Ignore(x => x.AttributeCount);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractAttributeGroup>(e =>
        {
            e.HasOne(x => x.Group).WithMany(x => x.Attributes).HasForeignKey(x => x.GroupId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractNumberRule>(e =>
        {
            e.Ignore(x => x.TypefixLabel);
            e.HasIndex(x => new { x.OrgId, x.TypeId }).IsUnique();
            e.HasOne(x => x.Type).WithOne(x => x.NumberRule).HasForeignKey<ContractNumberRule>(x => x.TypeId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractTypeConfig>(e =>
        {
            e.Ignore(x => x.TypeName);
            e.Ignore(x => x.ContractChannels);
            e.Ignore(x => x.OtpChannels);
            e.Ignore(x => x.ContractChannelsLabel);
            e.Ignore(x => x.OtpChannelsLabel);
            e.HasIndex(x => new { x.OrgId, x.TypeId }).IsUnique();
            e.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Contract>(e =>
        {
            e.Property(x => x.Value).HasPrecision(18, 2);
            e.Property(x => x.CurrencyRate).HasPrecision(18, 4);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.IsOpen);
            e.Ignore(x => x.SignedCount);
            e.Ignore(x => x.Kind);
            e.Ignore(x => x.ElementSignedCount);
            e.Ignore(x => x.CheckedCount);
            e.Ignore(x => x.AllChecked);
            e.Ignore(x => x.SignerConfirmedCount);
            e.Ignore(x => x.AllSignersConfirmed);
            e.Ignore(x => x.PartyConfirmedCount);
            e.Ignore(x => x.AllPartiesConfirmed);
            e.Ignore(x => x.IsApproved);
            e.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
            e.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Parent).WithMany(x => x.Annexes).HasForeignKey(x => x.ParentContractId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractParty>(e =>
        {
            e.Ignore(x => x.IsCancelled);
            e.Ignore(x => x.IsConfirmed);
            e.Ignore(x => x.HasValue);
            e.Property(x => x.ValContract).HasPrecision(18, 2);
            e.Property(x => x.ValPaymented).HasPrecision(18, 2);
            e.Property(x => x.ValRemain).HasPrecision(18, 2);
            e.Property(x => x.ValExchange).HasPrecision(18, 2);
            e.HasOne(x => x.Contract).WithMany(x => x.Parties).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractSignature>().HasQueryFilter(x => x.OrgId == _orgId);
        b.Entity<ContractHistory>(e =>
        {
            e.HasOne(x => x.Contract).WithMany(x => x.History).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractSignLink>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.Ignore(x => x.State);
            e.Ignore(x => x.IsUsable);
            e.HasOne(x => x.Contract).WithMany(x => x.SignLinks).HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractElement>(e =>
        {
            e.Ignore(x => x.TypeLabel);
            e.HasOne(x => x.Contract).WithMany(x => x.Elements).HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractChecker>(e =>
        {
            e.Ignore(x => x.RoleLabel);
            e.Ignore(x => x.HasApproved);
            e.HasOne(x => x.Contract).WithMany(x => x.Checkers).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractUserInContract>(e =>
        {
            e.HasOne(x => x.Contract).WithMany(x => x.UserAssignments).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractSigner>(e =>
        {
            e.Ignore(x => x.IsConfirmed);
            e.Ignore(x => x.SignStatusLabel);
            e.HasIndex(x => new { x.OrgId, x.ContractId, x.PartyCode, x.UserCodeSysSign });
            e.HasOne(x => x.Contract).WithMany(x => x.Signers).HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractSendHist>(e =>
        {
            e.Ignore(x => x.ChannelLabel);
            e.Ignore(x => x.BulletinLabel);
            e.HasOne(x => x.Contract).WithMany(x => x.SendHistory).HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractDetail>(e =>
        {
            e.Ignore(x => x.LineTotal);
            e.Ignore(x => x.UnitLabel);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.Qty).HasPrecision(18, 2);
            e.Property(x => x.ValContract).HasPrecision(18, 2);
            e.Property(x => x.ValTax).HasPrecision(18, 2);
            e.Property(x => x.DiscountRate).HasPrecision(18, 2);
            e.Property(x => x.ValDiscount).HasPrecision(18, 2);
            e.Property(x => x.VATRate).HasPrecision(18, 2);
            e.HasOne(x => x.Contract).WithMany(x => x.Details).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<AttributeContract>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractAttribute>(e =>
        {
            e.HasOne(x => x.Contract).WithMany(x => x.Attributes).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractAttributeDtl>(e =>
        {
            e.HasOne(x => x.Contract).WithMany(x => x.AttributeDetails).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractAttachment>(e =>
        {
            e.Ignore(x => x.RefDocTypeLabel);
            e.Ignore(x => x.PublicLabel);
            e.Ignore(x => x.HasFile);
            e.HasOne(x => x.Contract).WithMany(x => x.Attachments).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<FinishedContractReason>(e =>
        {
            e.Ignore(x => x.TypeLabel);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractVerifyOtp>(e =>
        {
            e.Ignore(x => x.State);
            e.Ignore(x => x.IsUsable);
            e.Ignore(x => x.StateLabel);
            e.HasIndex(x => new { x.OrgId, x.ContractCode, x.UserCodeSign });
            e.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<OrgCertificate>(e =>
        {
            e.Ignore(x => x.State);
            e.Ignore(x => x.IsUsable);
            e.Ignore(x => x.StateLabel);
            e.Ignore(x => x.ValidityLabel);
            e.HasIndex(x => new { x.OrgId, x.CANumber }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<OrgSignConfig>(e =>
        {
            e.Ignore(x => x.SignTypeLabel);
            e.Ignore(x => x.ValidityLabel);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ChannelConfig>(e =>
        {
            e.Ignore(x => x.ContractChannelLabel);
            e.Ignore(x => x.OtpChannelLabel);
            e.Ignore(x => x.AccessKeyChannelLabel);
            e.Ignore(x => x.HasEmail);
            e.Ignore(x => x.HasSms);
            e.Ignore(x => x.HasZalo);
            e.HasIndex(x => x.OrgId).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ChannelEmailConfig>(e =>
        {
            e.HasOne(x => x.ChannelConfig).WithOne(x => x.Email).HasForeignKey<ChannelEmailConfig>(x => x.ChannelConfigId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ChannelSmsConfig>(e =>
        {
            e.HasOne(x => x.ChannelConfig).WithOne(x => x.Sms).HasForeignKey<ChannelSmsConfig>(x => x.ChannelConfigId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ChannelZaloConfig>(e =>
        {
            e.HasOne(x => x.ChannelConfig).WithOne(x => x.Zalo).HasForeignKey<ChannelZaloConfig>(x => x.ChannelConfigId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<SubmissionForm>(e =>
        {
            e.Ignore(x => x.ChannelLabel);
            e.Ignore(x => x.BulletinLabel);
            e.Ignore(x => x.MessageCount);
            e.Ignore(x => x.ZnsParamCount);
            e.HasIndex(x => new { x.OrgId, x.SubFormCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<SubmissionFormMessage>(e =>
        {
            e.HasOne(x => x.SubmissionForm).WithMany(x => x.Messages).HasForeignKey(x => x.SubmissionFormId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<SubmissionFormZns>(e =>
        {
            e.HasOne(x => x.SubmissionForm).WithMany(x => x.ZnsParams).HasForeignKey(x => x.SubmissionFormId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<NotifyType>(e =>
        {
            e.Ignore(x => x.SubscriberCount);
            e.HasIndex(x => new { x.OrgId, x.NotifyTypeCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<UserNotifyType>(e =>
        {
            e.Ignore(x => x.FlagLabel);
            e.HasIndex(x => new { x.OrgId, x.UserCode, x.NotifyTypeCode });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TempType>(e =>
        {
            e.Ignore(x => x.HasImage);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CurrencyExchange>(e =>
        {
            e.Ignore(x => x.BaseLabel);
            e.Ignore(x => x.RateLabel);
            e.Ignore(x => x.HasRate);
            e.Property(x => x.BuyRate).HasPrecision(18, 4);
            e.Property(x => x.SellRate).HasPrecision(18, 4);
            e.Property(x => x.InterEx).HasPrecision(18, 4);
            e.HasIndex(x => new { x.OrgId, x.CurrencyCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<SystemParam>(e =>
        {
            e.Ignore(x => x.HasValue);
            e.HasIndex(x => new { x.OrgId, x.ParamCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
    }

    public override int SaveChanges() { StampOrg(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { StampOrg(); return base.SaveChangesAsync(ct); }

    private void StampOrg()
    {
        foreach (var entry in ChangeTracker.Entries<IOrgOwned>())
            if (entry.State == EntityState.Added && entry.Entity.OrgId == Guid.Empty)
                entry.Entity.OrgId = _orgId;
    }
}
