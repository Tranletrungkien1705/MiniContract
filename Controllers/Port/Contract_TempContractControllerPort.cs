using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port
{
    // port 1:1 Contract_TempContractController.cs — Contract_TempContract (hạng mục #78: DoSearch)
    // Gate:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Contract_TempContractController.cs:186-273 (DoSearch)
    //        + :589-654 (strWhereClause_Contract_TempContractSearch)
    // API:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\ContractTempContractController.cs:91-159 (WA_Contract_TempContract_Get)
    // BIZ:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:1853-1999 (WAS_Contract_TempContract_Get)
    //        → 2000-2164 (Contract_TempContract_Get) → 2166-2544 (Contract_TempContract_GetX — bản LIVE, SQL dựng chuỗi)
    [ApiController]
    [Route("api/contract_tempcontract")]
    public class Contract_TempContractControllerPort : ControllerBase
    {
        private readonly AppDbContext _db;

        public Contract_TempContractControllerPort(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("dosearch")]
        public async Task<IActionResult> DoSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_TempContract_DoSearchReq? req)
        {
            try
            {
                // Gate :194-195 — chữ ký nguồn: int page = 0, int recordcount = 10
                var page = req?.Page ?? 0;
                var recordCount = req?.RecordCount ?? 10;

                // Gate :214 — NetworkID = CUtils.StrValue(UserState.Mst_NNT.NetworkID) (phiên đăng nhập).
                // GAP: đích không có phiên -> nhận NetworkID tuỳ chọn trong body; thiếu thì KHÔNG lọc (A.14).
                var networkID = StrValue(req?.NetworkID);
                // Gate :215 — OrgID = orgid (query param, mặc định "")
                var orgID = StrValue(req?.OrgID);
                // Gate :216-218 — CUtils.StrValue (KHÔNG Trim/ToUpper)
                var tcontracname = StrValue(req?.TContracName);
                var contracttype = StrValue(req?.ContractType);
                var status = StrValue(req?.Status);
                // Gate :219-220 — createdtimefrom / createdtimeto
                var createdFrom = StrValue(req?.CreateDTimeUTCFrom);
                var createdTo = StrValue(req?.CreateDTimeUTCTo);

                // Gate :212 — strWhereClause_Contract_TempContractSearch
                var query = _db.Set<Contract_TempContract>().AsNoTracking();

                // :594-597 — Contract_TempContract.NetworkID = NetworkID
                if (!string.IsNullOrEmpty(networkID))
                    query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkID), PortQuery.Esc));
                // :598-601 — Contract_TempContract.OrgID = OrgID
                if (!string.IsNullOrEmpty(orgID))
                    query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgID), PortQuery.Esc));
                // :602-605 — Contract_TempContract.TContracName like '%x%'
                if (!string.IsNullOrEmpty(tcontracname))
                    query = query.Where(x => EF.Functions.Like((x.TContracName ?? "").ToLower(), PortQuery.Contains(tcontracname), PortQuery.Esc));
                // :606-609 — Contract_TempContract.ContractType = ContractType
                if (!string.IsNullOrEmpty(contracttype))
                    query = query.Where(x => EF.Functions.Like((x.ContractType ?? "").ToLower(), PortQuery.Equal(contracttype), PortQuery.Esc));
                // :610-613 — Contract_TempContract.FlagActive = FlagActive (tham số `status` của gate)
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(status), PortQuery.Esc));
                // :614-625 — CreateDTimeUTC >= from / <= to (local -> UTC theo UserState.UtcOffset)
                // GAP: UtcOffset lấy từ phiên nguồn; đích nhận tuỳ chọn, thiếu thì offset 0 (không đổi).
                var fromUtc = ToUtc(createdFrom, " 00:00:00", req?.UtcOffset);
                if (fromUtc.HasValue) query = query.Where(x => x.CreateDTimeUTC >= fromUtc.Value);
                var toUtc = ToUtc(createdTo, " 23:59:59", req?.UtcOffset);
                if (toUtc.HasValue) query = query.Where(x => x.CreateDTimeUTC <= toUtc.Value);

                // QContract.cs:2200-2201 — Ft_RecordStart = page * recordcount, Ft_RecordCount = recordcount
                long recordStart = (long)page * recordCount;
                long recordCountL = recordCount;

                // QContract.cs:2255 — MyCount = Count(0) của #tbl_Contract_TempContract_Filter_Draft (distinct TContractCode)
                // GAP: nguồn lọc thêm `inner join #tbl_Mst_Org_ViewAbility` (phân quyền xem theo user, QContract.cs:2236-2237);
                //      đích không có bảng view-ability -> KHÔNG lọc quyền (khai GAP, không bịa bảng).
                var itemCount = await query.CountAsync();

                // QContract.cs:2229-2252 — draft: select distinct ctc.TContractCode ... order by ctc.TContractCode desc
                var codes = (recordCountL <= 0 || recordStart < 0)
                    ? new List<string>()
                    : await query.OrderByDescending(x => x.TContractCode)
                                 .Skip((int)Math.Min(recordStart, int.MaxValue))
                                 .Take((int)Math.Min(recordCountL, int.MaxValue))
                                 .Select(x => x.TContractCode)
                                 .ToListAsync();

                var dataList = new List<Contract_TempContract_DoSearchItem>();
                var pageCount = 0;

                if (codes.Count > 0)
                {
                    // QContract.cs:2307-2326 — select t.MyIdxSeq, ctc.*, mct.ContractTypeName, mnnt.MST, mnnt.NNTFullName
                    //   inner join Mst_Org mo on ctc.OrgID = mo.OrgID
                    //   inner join Mst_NNT mnnt on mo.OrgID = mnnt.OrgID      (nguồn cũng có thể nhân dòng: 1 Org nhiều NNT)
                    //   left  join Mst_ContractType mct on ctc.ContractType = mct.ContractType and ctc.NetworkID = mct.NetworkID
                    var joined = from ctc in _db.Set<Contract_TempContract>().AsNoTracking().Where(x => codes.Contains(x.TContractCode))
                                 join mo in _db.Set<Mst_Org>().AsNoTracking() on ctc.OrgID equals mo.OrgID
                                 join mnnt in _db.Set<Mst_NNT>().AsNoTracking() on mo.OrgID equals mnnt.OrgID
                                 join mct in _db.Set<Mst_ContractType>().AsNoTracking()
                                     on new { ctc.ContractType, ctc.NetworkID } equals new { mct.ContractType, mct.NetworkID } into mctj
                                 from mct in mctj.DefaultIfEmpty()
                                 select new { ctc, mct, mnnt };

                    var joinedRows = await joined.ToListAsync();

                    // QContract.cs:2325 — order by t.MyIdxSeq asc (MyIdxSeq = identity của draft, tức thứ tự codes)
                    foreach (var r in joinedRows.OrderBy(r => codes.IndexOf(r.ctc.TContractCode)))
                    {
                        dataList.Add(new Contract_TempContract_DoSearchItem
                        {
                            MyIdxSeq = recordStart + codes.IndexOf(r.ctc.TContractCode),
                            TContractCode = r.ctc.TContractCode,
                            NetworkID = r.ctc.NetworkID,
                            OrgID = r.ctc.OrgID,
                            TContracName = r.ctc.TContracName,
                            ContractType = r.ctc.ContractType,
                            ContractName = r.ctc.ContractName,
                            TContractBody = r.ctc.TContractBody,
                            Remark = r.ctc.Remark,
                            FlagActive = r.ctc.FlagActive,
                            FlagIsFilePath = r.ctc.FlagIsFilePath,
                            TContractFileName = r.ctc.TContractFileName,
                            TContractFilePath = r.ctc.TContractFilePath,
                            TContractFileVersion = r.ctc.TContractFileVersion,
                            FlagCreatePartnerSignFirst = r.ctc.FlagCreatePartnerSignFirst,
                            FlagCheckOrderSign = r.ctc.FlagCheckOrderSign,
                            FlagSysGenAccessKey = r.ctc.FlagSysGenAccessKey,
                            CreateDTimeUTC = r.ctc.CreateDTimeUTC,
                            CreateBy = r.ctc.CreateBy,
                            CreateLUBy = r.ctc.CreateLUBy,
                            LogLUDTimeUTC = r.ctc.LogLUDTimeUTC,
                            LogLUBy = r.ctc.LogLUBy,
                            mct_ContractTypeName = r.mct != null ? r.mct.ContractTypeName : null,
                            mnnt_MST = r.mnnt.MST,
                            mnnt_NNTFullName = r.mnnt.NNTFullName,
                        });
                    }

                    // Gate :246-250 — chỉ tính PageCount khi có dòng
                    if (dataList.Count > 0)
                        pageCount = (itemCount % recordCount == 0) ? (itemCount / recordCount) : (itemCount / recordCount + 1);
                }

                // Gate :252-255 — pageInfo.PageSize/PageIndex/ItemCount/PageCount
                var pageInfo = new PageInfo<Contract_TempContract_DoSearchItem>(dataList, itemCount, pageCount, page, recordCount);

                // Gate :264 — JsonView("BindDataContract_TempContract", pageInfo)
                return Ok(new { Success = true, Data = pageInfo });
            }
            catch (Exception ex)
            {
                // Gate :266-271 — JsonViewError; mã lỗi mặc định của biz: TError.ErridQContract.Contract_TempContract_Get
                // (QContract.cs:1866 strErrorCodeDefault của WAS_Contract_TempContract_Get).
                return PortResult.Fail("Contract_TempContract_Get", "Exception", ex.Message);
            }
        }

        // Gate :617/623 — CUtils.ConvertingLocalTimeToUTC(local, UserState.UtcOffset)
        // GAP: đích không có phiên -> UtcOffset tuỳ chọn; thiếu thì coi offset 0 (không đổi).
        private static DateTime? ToUtc(string? date, string timeSuffix, string? utcOffset)
        {
            if (string.IsNullOrEmpty(date)) return null;
            var s = date + timeSuffix;
            var local = DateTime.ParseExact(s, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var offset = ParseUtcOffset(utcOffset);
            return DateTime.SpecifyKind(local - offset, DateTimeKind.Utc);
        }

        private static TimeSpan ParseUtcOffset(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return TimeSpan.Zero;
            s = s.Trim();
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts)) return ts;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h)) return TimeSpan.FromHours(h);
            throw new FormatException("UtcOffset không hợp lệ: " + s);
        }

        // CUtils.StrValue — nguồn KHÔNG Trim, KHÔNG ToUpper
        private static string StrValue(string? s) => s ?? string.Empty;
    }

    // Gate :186-195 — tham số của DoSearch (nguồn nhận query/form; đích nhận JSON body — A.14)
    public class Contract_TempContract_DoSearchReq
    {
        public string? ContractType { get; set; }        // :187 contracttype
        public string? TContracName { get; set; }        // :188 tcontracname
        public string? CreateDTimeUTCFrom { get; set; }  // :189 createdtimefrom = ""
        public string? CreateDTimeUTCTo { get; set; }    // :190 createdtimeto = ""
        public string? Status { get; set; }              // :191 status = ""
        public string? OrgID { get; set; }               // :192 orgid = ""
        public string? Init { get; set; }                // :193 init = "init" (nguồn không dùng trong where)
        public int? Page { get; set; }                   // :194 page = 0
        public int? RecordCount { get; set; }            // :195 recordcount = 10
        // GAP: lấy từ phiên nguồn (UserState) — đích nhận tuỳ chọn trong body
        public string? NetworkID { get; set; }           // :214 NetworkID = UserState.Mst_NNT.NetworkID
        public string? UtcOffset { get; set; }            // :233 UtcOffset = UserState.UtcOffset
    }

    // QContract.cs:2307-2326 — t.MyIdxSeq, ctc.*, mct.ContractTypeName, mnnt.MST, mnnt.NNTFullName
    public class Contract_TempContract_DoSearchItem
    {
        public long MyIdxSeq { get; set; }
        public string TContractCode { get; set; } = string.Empty;
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
        public string? TContracName { get; set; }
        public string? ContractType { get; set; }
        public string? ContractName { get; set; }
        public string? TContractBody { get; set; }
        public string? Remark { get; set; }
        public string? FlagActive { get; set; }
        public string? FlagIsFilePath { get; set; }
        public string? TContractFileName { get; set; }
        public string? TContractFilePath { get; set; }
        public string? TContractFileVersion { get; set; }
        public string? FlagCreatePartnerSignFirst { get; set; }
        public string? FlagCheckOrderSign { get; set; }
        public string? FlagSysGenAccessKey { get; set; }
        public DateTime? CreateDTimeUTC { get; set; }
        public string? CreateBy { get; set; }
        public string? CreateLUBy { get; set; }
        public DateTime? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
        public string? mct_ContractTypeName { get; set; }
        public string? mnnt_MST { get; set; }
        public string? mnnt_NNTFullName { get; set; }
    }
}
