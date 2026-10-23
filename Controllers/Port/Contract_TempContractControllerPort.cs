using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

        // port 1:1 Contract_TempContractController.cs:1289-1465 — Contract_TempContract
        // Nguồn gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Contract_TempContractController.cs:1289-1465
        // Nguồn API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\ContractTempContractController.cs:20-88
        // Nguồn BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:7005-7154 (WAS), 6822-7003 (Save), 5423-6821 (SaveX_New20240312 LIVE)
        [HttpPost("save")]
        public async Task<ActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_TempContractSaveReq? req)
        {
            // Gate nguồn: TempContractModel model, string act
            // Body rỗng / thiếu Model → mã mặc định Contract_TempContract_Save (QContract.cs:7018, 6861)
            if (req?.Model == null)
                return PortResult.Fail("Contract_TempContract_Save", "Check.strJson", null);

            var model = req.Model;

            // 1299-1303 gate: danh sách con null -> List rỗng
            model.AttList ??= new List<Contract_TempSaveAttReq>();
            model.PartyList ??= new List<Contract_TempSavePartyReq>();
            model.TempContractElementList ??= new List<Contract_TempSaveElementReq>();
            model.TempContractUserList ??= new List<Contract_TempSaveUserReq>();

            var networkID = StdParam(req.NetworkID);
            var orgID = StdParam(req.OrgID);
            var waUserCode = req.WAUserCode;
            var bIsDelete = string.Equals(StdParam(req.FlagIsDelete), "1", StringComparison.Ordinal);

            // 5477-5506 Refine and Check Input
            var strTContractCode = StdParam(model.TContractCode);
            var strNetworkID = networkID;
            var strOrgID = orgID;
            var strContractType = StdParam(model.ContractType);
            var strTContracName = StrValue(model.TContracName);
            var strTContractBody = StrValue(model.TContractBody);
            var strContractName = StrValue(model.ContractName);
            var strRemark = StrValue(model.Remark);
            var strTContractFileName = (model.AttFileName ?? "").Trim();          // 5490
            var strFlagActive = StdFlag(model.FlagActive);                        // 5496
            var strFlagIsFilePath = StdFlag(model.AttIsFilePath ? "1" : "0");     // 5497 (gate 1332)
            var strFlagCheckOrderSign = StdFlag(model.FlagCheckOrderSign);        // 5504
            var strFlagSysGenAccessKey = "1";                                     // 5506 — nguồn ghi cứng TConst.Flag.Active

            // 5515-5526 TContractCode rỗng
            if (string.IsNullOrEmpty(strTContractCode))
                return PortResult.Fail("Contract_TempContract_SaveX_InvalidTContractCode", "Check.strTContractCode", strTContractCode);

            // 5528-5534 Contract_TempContract_CheckDB (QContract.cs:1787-1850) — flag "" nên chỉ lấy dòng
            var dtDB = await _db.Set<Contract_TempContract>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.TContractCode.ToLower(), PortQuery.Equal(strTContractCode), PortQuery.Esc));

            // 5536-5541 chưa tồn tại + delete -> thành công (goto Done)
            if (dtDB == null && bIsDelete)
                return Ok(new { Success = true, RedirectUrl = (string?)null });

            // 5547-5553 giữ CreateDTimeUTC/CreateBy của bản ghi cũ
            var createDTimeUTC = dtDB?.CreateDTimeUTC ?? DateTime.UtcNow;
            var createBy = !string.IsNullOrEmpty(dtDB?.CreateBy) ? dtDB!.CreateBy! : (waUserCode ?? "");

            // 5555-5566 TContracName rỗng
            if (string.IsNullOrEmpty(strTContracName))
                return PortResult.Fail("Contract_TempContract_SaveX_InvalidTContracName", "Check.strTContracName", strTContracName);

            // 5569-5601 delete: kiểm TContractCode đã dùng trong Contract_Contract
            if (bIsDelete)
            {
                var used = await _db.Set<Contract_Contract>().AsNoTracking()
                    .AnyAsync(c => EF.Functions.Like(c.TContractCode.ToLower(), PortQuery.Equal(strTContractCode), PortQuery.Esc));
                if (used)
                    return PortResult.Fail("Contract_TempContract_SaveX_TContractCodeIsUsed", "Check.strTContractCode", strTContractCode);
            }

            // 5605-5612 Mst_ContractType_CheckDB (Master.cs:43978-44048) flag "1", "1"
            var ctRow = await _db.Set<Mst_ContractType>().AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.NetworkID.ToLower(), PortQuery.Equal(strNetworkID ?? ""), PortQuery.Esc)
                                       && EF.Functions.Like(x.ContractType.ToLower(), PortQuery.Equal(strContractType ?? ""), PortQuery.Esc));
            if (ctRow == null)
                return PortResult.Fail("Mst_ContractType_CheckDB_OrganNotFound", "Check.NetworkID", strNetworkID, "Check.ContractType", strContractType);
            if (!(ctRow.FlagActive ?? "").Contains("1"))
                return PortResult.Fail("Mst_ContractType_CheckDB_FlagActiveNotMatched",
                    "Check.NetworkID", strNetworkID, "Check.ContractType", strContractType,
                    "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", ctRow.FlagActive);

            // 5616-5657 Tên trùng (TContracName + OrgID, khác TContractCode)
            if (!bIsDelete)
            {
                var dup = await _db.Set<Contract_TempContract>().AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like(x.TContracName.ToLower(), PortQuery.Equal(strTContracName), PortQuery.Esc)
                                           && EF.Functions.Like(x.OrgID.ToLower(), PortQuery.Equal(strOrgID ?? ""), PortQuery.Esc));
                if (dup != null && !string.Equals(dup.TContractCode, strTContractCode, StringComparison.Ordinal))
                    return PortResult.Fail("Contract_TempContract_SaveX_TContracNameExist", "Check.strTContracName", strTContracName);
            }

            // 5659-5671 ContractFileName rỗng
            if (string.IsNullOrEmpty(strTContractFileName))
                return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName", "Check.ContractCode", strTContractCode, "Check.ContractFileName", strTContractFileName);

            // GAP: 5673-5700 OS_FileCenter_CreateAndMoveFX (file center ngoài) — khi FlagIsFilePath="0" source ghi file, lấy URL
            // Ở port: bỏ qua, dùng AttFilePath của client nếu có (khớp nhánh 1411 gate FlagIsFilePath="1" -> model.AttFilePath)
            string? strTContractFilePath = (strFlagIsFilePath == "1") ? model.AttFilePath : null;

            // 5760-5852 Attribute loop — Mst_Attribute_Contract_CheckDB (QContract.cs:7908-7970)
            var attributeRows = new List<Contract_Attribute_Temp>();
            if (!bIsDelete)
            {
                foreach (var att in model.AttList)
                {
                    var attrCode = StdParam(att.Code);
                    var attrValue = (att.Value ?? "").Trim();

                    var mstAttr = await _db.Set<Mst_Attribute_Contract>().AsNoTracking()
                        .FirstOrDefaultAsync(x => EF.Functions.Like(x.AttributeContractCode.ToLower(), PortQuery.Equal(attrCode ?? ""), PortQuery.Esc));
                    if (mstAttr == null)
                        return PortResult.Fail("Mst_Attribute_Contract_CheckDB_OrganNotFound", "Check.AttributeContractCode", attrCode);
                    if (!(mstAttr.FlagActive ?? "").Contains("1"))
                        return PortResult.Fail("Mst_Attribute_Contract_CheckDB_FlagActiveNotMatched",
                            "Check.AttributeContractCode", attrCode,
                            "Check.strFlagActiveListToCheck", "1", "DB.FlagActive", mstAttr.FlagActive);

                    if (string.IsNullOrEmpty(attrCode))
                        return PortResult.Fail("Contract_TempContract_SaveX_InvalidAttributeContractCode",
                            "Check.strTContractCode", strTContractCode, "Check.strAttributeContractCode", attrCode);
                    if (string.IsNullOrEmpty(attrValue))
                        return PortResult.Fail("Contract_TempContract_SaveX_InvalidAttributeValue",
                            "Check.strTContractCode", strTContractCode, "Check.strAttributeContractCode", attrCode, "Check.strAttributeValue", attrValue);

                    attributeRows.Add(new Contract_Attribute_Temp
                    {
                        TContractCode = strTContractCode,
                        AttributeContractCode = attrCode!,
                        NetworkID = strNetworkID,
                        OrgID = strOrgID,
                        AttributeValue = attrValue,
                        FlagActive = "1",
                        LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        LogLUBy = waUserCode,
                    });
                }
            }

            // 5874-5934 Party — chỉ std transform, không có guard
            var partyRows = new List<Contract_TempContractParty>();
            if (!bIsDelete)
            {
                foreach (var p in model.PartyList)
                {
                    partyRows.Add(new Contract_TempContractParty
                    {
                        TContractCode = strTContractCode,
                        PartyCode = StdParam(p.Code),
                        OrgID = strOrgID,
                        MST = StdParam(p.MST),
                        CustomerCode = StdParam(p.CustomerCode),
                        CustomerCodeSys = StdParam(p.CustomerCodeSys),
                        BankAccountNo = StdParam(p.BankAccountNo),
                        CustomerName = p.CustomerName,
                        CustomerAddress = p.CustomerAddress,
                        CustomerEmail = p.CustomerEmail,
                        CustomerPhoneNo = p.CustomerPhoneNo,
                        CustomerWebsite = p.CustomerWebsite,
                        BankCode = StdParam(p.BankCode),
                        BankName = StdParam(p.BankName),
                        RepresentName = p.RepresentName,
                        RepresentPosition = p.RepresentPosition,
                        Description = p.Description,
                        FlagActive = "1",
                        LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        LogLUBy = waUserCode,
                    });
                }
            }

            // 5969-6077 User — AccessKey length 6-50 (6038) + trùng (6053-6068)
            var userRows = new List<Contract_TempContractUser>();
            if (!bIsDelete)
            {
                var seenKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var u in model.TempContractUserList)
                {
                    var accessKey = StdParam(u.AccessKey);
                    if (!string.IsNullOrEmpty(accessKey))
                    {
                        if (accessKey.Length > 50 || accessKey.Length < 6)
                            return PortResult.Fail("Contract_TempContract_Save_Contract_TempContractUserInvalidAccessKeyLength",
                                "Check.Idx", u.Idx, "Check.UserCodeSign", u.UserCodeSign, "Check.AccessKey", accessKey);
                        if (!seenKeys.Add(accessKey))
                            return PortResult.Fail("Contract_TempContract_Save_Contract_TempContractUserDuplicateAccessKey", "Check.AccessKey", accessKey);
                    }

                    userRows.Add(new Contract_TempContractUser
                    {
                        TContractCode = strTContractCode,
                        PartyCode = StdParam(u.PartyCode),
                        UserCodeSysSign = u.UserCodeSysSign,
                        NetworkID = strNetworkID,
                        OrgID = strOrgID,
                        Idx = u.Idx,
                        UserCodeSign = u.UserCodeSign,
                        UserNameSign = u.UserNameSign,
                        UserEmail = u.UserEmail,
                        UserPhone = u.UserPhone,
                        UserZalo = u.UserZalo,
                        SignDetailJson = null,           // không có trong gate
                        FlagActive = "1",
                        AccessKey = accessKey,
                        LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        LogLUBy = waUserCode,
                    });
                }
            }

            // 6223-6348 Elements — ELECTRONIC/DIGITAL/SHORT cần SignByCode (6329-6341)
            var elementRows = new List<Contract_TempElement>();
            if (!bIsDelete)
            {
                foreach (var el in model.TempContractElementList)
                {
                    var elementType = StdParam(el.ElementType);
                    if (elementType == "ELECTRONIC" || elementType == "DIGITAL" || elementType == "SHORT")
                    {
                        // GAP: 6305-6322 OS_FileCenter_CreateAndMoveFX (file center ngoài) khi FlagIsFilePath="ADD"
                        if (string.IsNullOrEmpty(el.SignByCode))
                            return PortResult.Fail("Contract_TempContract_SaveX_SignByCodeNotNull", "Check.strElementType", el.ElementType);
                    }

                    // 1398/1426 gate: ElementJson = serialize nguyên object
                    elementRows.Add(new Contract_TempElement
                    {
                        TContractCode = strTContractCode,
                        SignByCode = StdParam(el.SignByCode),
                        ElementType = elementType,
                        ParamContractCode = StdParam(el.ParamContractCode),
                        ElementCode = StdParam(el.ElementCode),
                        PageIdx = el.PageIdx,
                        ElementX = el.ElementX,
                        ElementY = el.ElementY,
                        ElementWidth = el.ElementWidth,
                        ElementHeight = el.ElementHeight,
                        PageWidth = el.PageWidth,
                        PageHeight = el.PageHeight,
                        ElementTypeValue = el.ElementTypeValue,
                        ElementName = el.ElementName,
                        ElementDes = el.ElementDes,
                        ElementJson = System.Text.Json.JsonSerializer.Serialize(el),
                        FlagActive = "1",
                        LogLUDTimeUTC = DateTime.UtcNow,
                        LogLUBy = waUserCode,
                    });
                }
            }

            // 6385-6740 Delete All rồi Insert (nếu !delete) — transaction
            _db.ChangeTracker.Clear();
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 6387-6443 xoá mọi bảng con theo TContractCode
                await _db.Set<Contract_TempElement>().Where(x => x.TContractCode == strTContractCode).ExecuteDeleteAsync();
                await _db.Set<Contract_TempContractFiles>().Where(x => x.TContractCode == strTContractCode).ExecuteDeleteAsync();
                await _db.Set<Contract_TempContractUser>().Where(x => x.TContractCode == strTContractCode).ExecuteDeleteAsync();
                await _db.Set<Contract_TempContractParty>().Where(x => x.TContractCode == strTContractCode).ExecuteDeleteAsync();
                await _db.Set<Contract_TempChecker>().Where(x => x.TContractCode == strTContractCode).ExecuteDeleteAsync();
                await _db.Set<Contract_Attribute_Temp>().Where(x => x.TContractCode == strTContractCode).ExecuteDeleteAsync();
                await _db.Set<Contract_TempContract>().Where(x => x.TContractCode == strTContractCode).ExecuteDeleteAsync();

                if (!bIsDelete)
                {
                    // 6456-6501 INSERT Contract_TempContract (19 cột)
                    _db.Set<Contract_TempContract>().Add(new Contract_TempContract
                    {
                        TContractCode = strTContractCode,
                        NetworkID = strNetworkID,
                        OrgID = strOrgID,
                        ContractType = strContractType,
                        TContracName = strTContracName,
                        TContractBody = strTContractBody,
                        ContractName = strContractName,
                        Remark = strRemark,
                        TContractFilePath = strTContractFilePath,
                        TContractFileName = strTContractFileName,
                        TContractFileVersion = Guid.NewGuid().ToString(),
                        FlagCreatePartnerSignFirst = "0",   // StdFlag(null) — nguồn không có từ gate
                        FlagCheckOrderSign = strFlagCheckOrderSign,
                        FlagActive = strFlagActive,
                        FlagSysGenAccessKey = strFlagSysGenAccessKey,
                        CreateDTimeUTC = createDTimeUTC,
                        CreateBy = createBy,
                        LogLUDTimeUTC = DateTime.UtcNow,
                        LogLUBy = waUserCode,
                    });

                    if (attributeRows.Count > 0) _db.Set<Contract_Attribute_Temp>().AddRange(attributeRows);
                    if (partyRows.Count > 0) _db.Set<Contract_TempContractParty>().AddRange(partyRows);
                    if (userRows.Count > 0) _db.Set<Contract_TempContractUser>().AddRange(userRows);
                    if (elementRows.Count > 0) _db.Set<Contract_TempElement>().AddRange(elementRows);
                    // Contract_TempContractFiles: gate luôn gửi List rỗng (1404) -> không có dòng nào
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }

            // GAP: 6785-6796 Sys_User_CheckDB + Sys_User_UpdDTimeUsed (System.cs:13726-13748)
            //       -> cập nhật DTimeUsed hệ thống ngoài phạm vi Save, bỏ qua.

            // 1435-1450 gate: RedirectUrl theo act
            var url = "Index";
            if (!string.IsNullOrEmpty(req.Act) && req.Act.Equals("copy", StringComparison.OrdinalIgnoreCase))
                url = "Create?copy=" + strTContractCode;
            else if (!string.IsNullOrEmpty(req.Act) && req.Act.Equals("new", StringComparison.OrdinalIgnoreCase))
                url = "Create";

            return Ok(new { Success = true, RedirectUrl = url });
        }

        // TUtils.CUtils.StdParam — Trim().ToUpper(), rỗng -> null
        private static string? StdParam(string? x)
        {
            if (string.IsNullOrWhiteSpace(x)) return null;
            return x.Trim().ToUpperInvariant();
        }

        // TUtils.CUtils.StdFlag — "1" nếu đúng "1", mọi giá trị khác (kể cả null) -> "0"
        private static string StdFlag(string? x) => x == "1" ? "1" : "0";

        // string.Format("{0}", x) — nguồn không Trim, không ToUpper
        private static string StrValue(string? s) => s ?? string.Empty;
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

    // Request body của action Save (gate nguồn: TempContractModel model, string act)
    public class Contract_TempContractSaveReq
    {
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
        public string? WAUserCode { get; set; }
        public string? Act { get; set; }
        public string? FlagIsDelete { get; set; }
        public Contract_TempContractSaveModel? Model { get; set; }
    }

    public class Contract_TempContractSaveModel
    {
        public string? TContractCode { get; set; }
        public string? ContractName { get; set; }
        public string? ContractType { get; set; }
        public string? TContracName { get; set; }
        public string? TContractBody { get; set; }
        public string? FlagActive { get; set; }
        public string? Remark { get; set; }
        public string? FlagCheckOrderSign { get; set; }
        public string? FlagSysGenAccessKey { get; set; }
        public string? AttFileName { get; set; }
        public bool AttIsFilePath { get; set; }
        public bool IsAttachFileContract { get; set; }
        public string? AttFilePath { get; set; }
        public string? AttFileId { get; set; }
        public List<Contract_TempSaveAttReq>? AttList { get; set; }
        public List<Contract_TempSavePartyReq>? PartyList { get; set; }
        public List<Contract_TempSaveElementReq>? TempContractElementList { get; set; }
        public List<Contract_TempSaveUserReq>? TempContractUserList { get; set; }
    }

    public class Contract_TempSaveAttReq
    {
        public string? Code { get; set; }
        public string? Value { get; set; }
    }

    public class Contract_TempSavePartyReq
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? TContractCode { get; set; }
        public string? CustomerCodeSys { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? BankAccountNo { get; set; }
        public string? MST { get; set; }
        public string? OrgID { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhoneNo { get; set; }
        public string? CustomerWebsite { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? RepresentName { get; set; }
        public string? RepresentPosition { get; set; }
        public string? FlagActive { get; set; }
    }

    public class Contract_TempSaveElementReq
    {
        public string? ElementID { get; set; }
        public string? ElementCode { get; set; }
        public string? ParamContractCode { get; set; }
        public string? SignByCode { get; set; }
        public string? DataType { get; set; }
        public string? ElementType { get; set; }
        public string? PageIdx { get; set; }
        public string? ElementX { get; set; }
        public string? ElementY { get; set; }
        public string? ElementWidth { get; set; }
        public string? ElementHeight { get; set; }
        public string? PageWidth { get; set; }
        public string? PageHeight { get; set; }
        public string? ElementTypeValue { get; set; }
        public string? ElementImageSpec { get; set; }
        public string? ElementImageName { get; set; }
        public string? ElementName { get; set; }
        public string? ElementDes { get; set; }
        public string? FlagActive { get; set; }
        public string? FlagIsFilePath { get; set; }
    }

    public class Contract_TempSaveUserReq
    {
        public string? TContractCode { get; set; }
        public string? PartyCode { get; set; }
        public string? UserCodeSysSign { get; set; }
        public string? Idx { get; set; }
        public string? UserCodeSign { get; set; }
        public string? UserNameSign { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public string? UserZalo { get; set; }
        public string? AccessKey { get; set; }
        public string? FlagActive { get; set; }
    }
}
