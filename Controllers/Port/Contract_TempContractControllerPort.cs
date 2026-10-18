using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port
{
    [ApiController]
    [Route("api/contract_tempcontract")]
    public class Contract_TempContractControllerPort : ControllerBase
    {
        private readonly AppDbContext _db;

        public Contract_TempContractControllerPort(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("create_bk")]
        public async Task<ActionResult> Create_BK([FromBody] Contract_TempCreateReq? req)
        {
            req ??= new Contract_TempCreateReq();
            var networkID = req.NetworkID ?? "1001";
            var orgID = req.OrgID ?? "ROOT";
            var tcontractcode = req.TContractCode ?? string.Empty;
            var copy = req.Copy;

            var seqVal = await PortSeq.NextAsync<Contract_TempContract>(_db);
            var newCode = $"TCC{seqVal:D6}";

            object model;
            if (!string.IsNullOrEmpty(copy))
            {
                var existing = await _db.Set<Contract_TempContract>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like(x.TContractCode.ToLower(), PortQuery.Equal(copy), PortQuery.Esc));
                if (existing != null)
                {
                    existing.TContracName = (existing.TContracName ?? "") + " Copy";
                    existing.TContractCode = newCode;
                }
                model = new
                {
                    Lst_Contract_TempContract = existing != null ? new List<Contract_TempContract> { existing } : new List<Contract_TempContract>(),
                    Lst_Contract_Attribute_Temp = new List<Contract_Attribute_Temp>(),
                    Lst_Contract_TempChecker = new List<Contract_TempChecker>(),
                    Lst_Contract_TempContractParty = new List<Contract_TempContractParty>()
                };
            }
            else if (!string.IsNullOrEmpty(tcontractcode))
            {
                var existing = await _db.Set<Contract_TempContract>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like(x.TContractCode.ToLower(), PortQuery.Equal(tcontractcode), PortQuery.Esc));
                if (existing != null)
                {
                    existing.TContractCode = newCode;
                }
                model = new
                {
                    Lst_Contract_TempContract = existing != null ? new List<Contract_TempContract> { existing } : new List<Contract_TempContract>(),
                    Lst_Contract_Attribute_Temp = new List<Contract_Attribute_Temp>(),
                    Lst_Contract_TempChecker = new List<Contract_TempChecker>(),
                    Lst_Contract_TempContractParty = new List<Contract_TempContractParty>()
                };
            }
            else
            {
                model = new
                {
                    Lst_Contract_Attribute_Temp = new List<Contract_Attribute_Temp>(),
                    Lst_Contract_TempChecker = new List<Contract_TempChecker>(),
                    Lst_Contract_TempContract = new List<Contract_TempContract>
                    {
                        new Contract_TempContract { TContractCode = newCode, NetworkID = networkID, OrgID = orgID, FlagActive = "1" }
                    },
                    Lst_Contract_TempContractParty = new List<Contract_TempContractParty>
                    {
                        new Contract_TempContractParty { PartyCode = "PARTY_A", Description = "Chủ đầu tư", CustomerCodeSys = "OWN" }
                    }
                };
            }

            var typeList = await _db.Set<Mst_ContractType>().Where(x => x.NetworkID == networkID && x.FlagActive == "1").ToListAsync();
            var attList = await _db.Set<Mst_Attribute_Contract>().Where(x => x.NetworkID == networkID && x.FlagActive == "1").ToListAsync();
            var allUser = await _db.Set<Sys_User>().Where(x => x.NetworkID == networkID && x.FlagActive == "1").ToListAsync();
            var paramList = await _db.Set<Mst_ParamContractElement>().Where(x => x.FlagActive == "1").ToListAsync();

            var listTContractTemp = await _db.Set<Contract_TempContract>()
                .Where(x => x.NetworkID == networkID && x.OrgID == orgID && x.FlagActive == "1")
                .ToListAsync();

            var resultData = new
            {
                Model = model,
                TypeList = typeList,
                AttList = attList,
                AllUser = allUser,
                ListMst_ParamContractElement = paramList,
                ListTContractTemp = listTContractTemp,
                IsUpdate = string.IsNullOrEmpty(copy) && string.IsNullOrEmpty(tcontractcode) ? "0" : "1"
            };

            return Ok(resultData);
        }
    }

    public class Contract_TempCreateReq
    {
        public string? Copy { get; set; }
        public string? TContractCode { get; set; }
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
    }
}
