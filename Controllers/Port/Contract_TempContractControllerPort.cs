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

        [HttpPost("detail")]
        public async Task<ActionResult> Detail([FromBody] Contract_TempDetailReq? req)
        {
            req ??= new Contract_TempDetailReq();
            var tcontractcode = req.TContractCode ?? string.Empty;
            var networkID = req.NetworkID ?? "1001";
            var orgID = req.OrgID ?? "ROOT";

            var contract = await _db.Set<Contract_TempContract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.TContractCode.ToLower(), PortQuery.Equal(tcontractcode), PortQuery.Esc));

            var lstContractTempContract = new List<Contract_TempContract>();
            var lstAttributeTemp = new List<Contract_Attribute_Temp>();
            var lstContractTempContractParty = new List<Contract_TempContractParty>();
            var lstContractTempElement = new List<Contract_TempElement>();
            var lstContractTempChecker = new List<Contract_TempChecker>();

            if (contract != null)
            {
                lstContractTempContract.Add(contract);
                lstAttributeTemp = await _db.Set<Contract_Attribute_Temp>()
                    .Where(x => x.TContractCode == contract.TContractCode)
                    .ToListAsync();
                lstContractTempContractParty = await _db.Set<Contract_TempContractParty>()
                    .Where(x => x.TContractCode == contract.TContractCode)
                    .ToListAsync();
                lstContractTempElement = await _db.Set<Contract_TempElement>()
                    .Where(x => x.TContractCode == contract.TContractCode)
                    .ToListAsync();
                lstContractTempChecker = await _db.Set<Contract_TempChecker>()
                    .Where(x => x.TContractCode == contract.TContractCode)
                    .ToListAsync();
            }

            if (lstContractTempChecker == null)
            {
                lstContractTempChecker = new List<Contract_TempChecker>();
            }

            var listMstParamContractElement = await _db.Set<Mst_ParamContractElement>()
                .Where(x => x.FlagActive == "1")
                .ToListAsync();

            var listMstContractType = await _db.Set<Mst_ContractType>()
                .Where(x => x.NetworkID == networkID && x.FlagActive == "1")
                .ToListAsync();

            var allUser = await _db.Set<Sys_User>()
                .Where(x => x.NetworkID == networkID && x.FlagActive == "1")
                .ToListAsync();

            var attData = await _db.Set<Mst_Attribute_Contract>()
                .Where(x => x.NetworkID == networkID && x.FlagActive == "1")
                .ToListAsync();

            // Process ElementJson deserialize like source
            var processedElements = new List<Contract_TempElement>();
            foreach (var item in lstContractTempElement)
            {
                var obj = item;
                if (!string.IsNullOrEmpty(item.ElementJson))
                {
                    try
                    {
                        var deserialized = System.Text.Json.JsonSerializer.Deserialize<Contract_TempElement>(item.ElementJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (deserialized != null) obj = deserialized;
                    }
                    catch { }
                }
                processedElements.Add(obj);
            }

            var model = new
            {
                Lst_Contract_TempContract = lstContractTempContract,
                Lst_Contract_Attribute_Temp = lstAttributeTemp,
                Lst_Contract_TempContractParty = lstContractTempContractParty,
                Lst_Contract_TempContractFiles = new List<object>(),
                Lst_Contract_TempElement = processedElements,
                Lst_Contract_TempChecker = lstContractTempChecker
            };

            var resultData = new
            {
                Model = model,
                TypeList = listMstContractType,
                AttList = attData,
                ListMst_ParamContractElement = listMstParamContractElement,
                AllUser = allUser,
                IsUpdate = "1",
                tContractCodeCur = tcontractcode
            };

            return Ok(resultData);
        }

        [HttpPost("update")]
        public async Task<ActionResult> Update([FromBody] Contract_TempUpdateReq? req)
        {
            req ??= new Contract_TempUpdateReq();
            var tcontractcode = req.TContractCode ?? string.Empty;
            var networkID = req.NetworkID ?? "1001";
            var orgID = req.OrgID ?? "ROOT";

            var contract = await _db.Set<Contract_TempContract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.TContractCode.ToLower(), PortQuery.Equal(tcontractcode), PortQuery.Esc));

            var lstContractTempContract = new List<Contract_TempContract>();
            var lstAttributeTemp = new List<Contract_Attribute_Temp>();
            var lstContractTempContractParty = new List<Contract_TempContractParty>();

            if (contract != null)
            {
                lstContractTempContract.Add(contract);
                lstAttributeTemp = await _db.Set<Contract_Attribute_Temp>()
                    .Where(x => x.TContractCode == contract.TContractCode)
                    .ToListAsync();
                lstContractTempContractParty = await _db.Set<Contract_TempContractParty>()
                    .Where(x => x.TContractCode == contract.TContractCode)
                    .ToListAsync();
            }

            var listMstParamContractElement = await _db.Set<Mst_ParamContractElement>()
                .Where(x => x.FlagActive == "1")
                .ToListAsync();

            var listMstContractType = await _db.Set<Mst_ContractType>()
                .Where(x => x.NetworkID == networkID && x.FlagActive == "1")
                .ToListAsync();

            var model = new
            {
                Lst_Contract_TempContract = lstContractTempContract,
                Lst_Contract_Attribute_Temp = lstAttributeTemp,
                Lst_Contract_TempContractParty = lstContractTempContractParty,
                Lst_Contract_TempContractFiles = new List<object>(),
                Lst_Contract_TempElement = new List<object>(),
                Lst_Contract_TempChecker = new List<object>()
            };

            var resultData = new
            {
                Model = model,
                ListMst_ParamContractElement = listMstParamContractElement,
                TypeList = listMstContractType,
                IsUpdate = "1"
            };

            return Ok(resultData);
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
                    Lst_Contract_TempChecker = new List<object>(),
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
                    Lst_Contract_TempChecker = new List<object>(),
                    Lst_Contract_TempContractParty = new List<Contract_TempContractParty>()
                };
            }
            else
            {
                model = new
                {
                    Lst_Contract_Attribute_Temp = new List<Contract_Attribute_Temp>(),
                    Lst_Contract_TempChecker = new List<object>(),
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

        [HttpPost("searchuser")]
        public async Task<ActionResult> SearchUser([FromBody] Contract_SearchUserReq? req)
        {
            req ??= new Contract_SearchUserReq();
            var q = req.Q ?? string.Empty;
            if (string.IsNullOrEmpty(q))
            {
                return Ok(new
                {
                    Success = true,
                    Data = new List<object>()
                });
            }

            var networkID = req.NetworkID ?? "1001";
            var orgID = req.OrgID ?? "ROOT";
            var pattern = $"%{q.ToLower()}%";

            var users = await _db.Set<Sys_User>()
                .Where(u => u.FlagActive == "1" && (
                    EF.Functions.Like(u.UserCode.ToLower(), pattern, "\\") ||
                    EF.Functions.Like(u.UserName.ToLower(), pattern, "\\") ||
                    EF.Functions.Like(u.Position.ToLower(), pattern, "\\") ||
                    EF.Functions.Like(u.PhoneNo.ToLower(), pattern, "\\") ||
                    EF.Functions.Like(u.EMail.ToLower(), pattern, "\\") ||
                    EF.Functions.Like(u.MST.ToLower(), pattern, "\\")
                ))
                .Take(10)
                .ToListAsync();

            var ret = users.Select(u => new
            {
                id = u.UserCode ?? "",
                text = u.UserName ?? "",
                UUID = u.ACId ?? "",
                Code = u.UserCode ?? "",
                Name = u.UserName ?? "",
                Email = u.EMail ?? "",
                Avatar = !string.IsNullOrEmpty(u.ACAvatar) && u.ACAvatar.Contains("~") 
                    ? u.ACAvatar.Split('~')[1] 
                    : (u.ACAvatar ?? ""),
                Pos = u.Position ?? "",
                Dep = "",
                DateTimeCreated = ""
            });

            return Ok(new
            {
                Success = true,
                Data = ret
            });
        }
    }

    public class Contract_TempDetailReq
    {
        public string? TContractCode { get; set; }
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
    }

    public class Contract_TempUpdateReq
    {
        public string? TContractCode { get; set; }
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
    }

    public class Contract_TempCreateReq
    {
        public string? Copy { get; set; }
        public string? TContractCode { get; set; }
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
    }

    public class Contract_SearchUserReq
    {
        public string? Q { get; set; }
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
    }
}
