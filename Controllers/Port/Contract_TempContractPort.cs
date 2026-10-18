using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port
{
    [ApiController]
    [Route("api/contract_tempcontract")]
    public class Contract_TempContractPortController : ControllerBase
    {
        private readonly AppDbContext _db;

        public Contract_TempContractPortController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("index")]
        public async Task<IActionResult> Index([FromBody] Contract_TempContractSearchReq? req)
        {
            req ??= new Contract_TempContractSearchReq();
            var page = Math.Max(0, req.Page);
            var pageSize = req.RecordCount > 0 ? req.RecordCount : 10;

            var query = _db.Set<Contract_TempContract>().AsQueryable();

            if (!string.IsNullOrEmpty(req.NetworkID))
            {
                var pat = PortQuery.Equal(req.NetworkID);
                query = query.Where(x => x.NetworkID != null && EF.Functions.Like(x.NetworkID.ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.OrgID))
            {
                var pat = PortQuery.Equal(req.OrgID);
                query = query.Where(x => x.OrgID != null && EF.Functions.Like(x.OrgID.ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.ContractType))
            {
                var pat = PortQuery.Equal(req.ContractType);
                query = query.Where(x => x.ContractType != null && EF.Functions.Like(x.ContractType.ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.TContracName))
            {
                var pat = PortQuery.Contains(req.TContracName);
                query = query.Where(x => x.TContracName != null && EF.Functions.Like(x.TContracName.ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.Status))
            {
                var pat = PortQuery.Equal(req.Status);
                query = query.Where(x => x.FlagActive != null && EF.Functions.Like(x.FlagActive.ToLower(), pat, PortQuery.Esc));
            }

            var itemCount = await query.CountAsync();
            var pageCount = itemCount == 0 ? 0 : (int)Math.Ceiling(itemCount / (double)pageSize);

            var list = itemCount == 0 || pageSize <= 0
                ? new List<Contract_TempContract>()
                : await query.OrderByDescending(x => x.TContractCode)
                    .Skip((int)Math.Min((long)page * pageSize, int.MaxValue))
                    .Take((int)Math.Min(pageSize, int.MaxValue))
                    .ToListAsync();

            return Ok(new
            {
                Success = true,
                PageInfo = new
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    ItemCount = itemCount,
                    PageCount = pageCount,
                    DataList = list
                }
            });
        }
    }

    public class Contract_TempContractSearchReq
    {
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
        public string? ContractType { get; set; }
        public string? TContracName { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 0;
        public int RecordCount { get; set; } = 10;
        public string? Init { get; set; }
    }
}
