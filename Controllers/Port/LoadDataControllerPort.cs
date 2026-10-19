using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Models.Port;
using MiniContract.Data;

namespace MiniContract.Controllers.Port
{
    [ApiController]
    [Route("api/loaddata")]
    public class LoadDataControllerPort : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration? _config;

        public LoadDataControllerPort(AppDbContext db, IConfiguration? config = null)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("loadmst_district")]
        public async Task<IActionResult> LoadMst_District([FromBody] LoadMst_DistrictReq? req)
        {
            try
            {
                var provincecode = req?.ProvinceCode?.Trim();
                var listMst_District = new List<Mst_District>();

                if (!string.IsNullOrEmpty(provincecode))
                {
                    var query = _db.Set<Mst_District>().AsQueryable();
                    query = query.Where(x => EF.Functions.Like(x.ProvinceCode.ToLower(), PortQuery.Equal(provincecode), PortQuery.Esc));
                    query = query.Where(x => EF.Functions.Like(x.FlagActive.ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

                    listMst_District = await query.ToListAsync();
                }

                return Ok(listMst_District);
            }
            catch (Exception ex)
            {
                return PortResult.Fail("LoadData_LoadMst_District", "Exception", ex.Message);
            }
        }

        [HttpPost("loadmst_ward")]
        public async Task<IActionResult> LoadMst_Ward([FromBody] LoadMst_WardReq? req)
        {
            try
            {
                var provincecode = req?.ProvinceCode?.Trim();
                var districtcode = req?.DistrictCode?.Trim();
                var listMst_Ward = new List<Mst_Ward>();

                if (!string.IsNullOrEmpty(provincecode))
                {
                    var query = _db.Set<Mst_Ward>().AsQueryable();
                    query = query.Where(x => EF.Functions.Like(x.ProvinceCode.ToLower(), PortQuery.Equal(provincecode), PortQuery.Esc));
                    if (!string.IsNullOrEmpty(districtcode))
                    {
                        query = query.Where(x => EF.Functions.Like(x.DistrictCode.ToLower(), PortQuery.Equal(districtcode), PortQuery.Esc));
                    }
                    query = query.Where(x => EF.Functions.Like(x.FlagActive.ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

                    listMst_Ward = await query.ToListAsync();
                }

                return Ok(listMst_Ward);
            }
            catch (Exception ex)
            {
                return PortResult.Fail("LoadData_LoadMst_Ward", "Exception", ex.Message);
            }
        }

        [HttpPost("searchuser")]
        public async Task<IActionResult> SearchUser([FromBody] SearchUserReq? req)
        {
            try
            {
                var keyword = req?.Keyword?.Trim();
                var listSys_User = new List<Sys_User>();

                if (!string.IsNullOrEmpty(keyword))
                {
                    var query = _db.Set<Sys_User>().AsQueryable();
                    var pat = "%" + keyword.ToLower() + "%";
                    query = query.Where(x =>
                        (x.UserCode != null && EF.Functions.Like(x.UserCode.ToLower(), pat, PortQuery.Esc)) ||
                        (x.UserName != null && EF.Functions.Like(x.UserName.ToLower(), pat, PortQuery.Esc)) ||
                        (x.EMail != null && EF.Functions.Like(x.EMail.ToLower(), pat, PortQuery.Esc))
                    );
                    query = query.Where(x => x.FlagActive != null && EF.Functions.Like(x.FlagActive.ToLower(), PortQuery.Equal("1"), PortQuery.Esc));
                    query = query.Take(10);

                    listSys_User = await query.ToListAsync();
                }

                return Ok(new { Success = true, ListSys_User = listSys_User });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("LoadData_SearchUser", "Exception", ex.Message);
            }
        }

        /// <summary>
        /// Tạo mới / Lưu mẫu hợp đồng.
        /// Gate: LoadDataController.cs:236-267 Contract_TempContract_Create(string model)
        /// API:  ContractTempContractController.cs:20-75 WA_Contract_TempContract_Save
        /// BIZ:  QContract.cs:7005-7154 WAS_Contract_TempContract_Save -> 6822-7003 Contract_TempContract_Save -> 5423-6821 Contract_TempContract_SaveX_New20240312
        /// </summary>
        [HttpPost("contract_tempcontract_create")]
        public async Task<IActionResult> Contract_TempContract_Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_TempContract_CreateReq? req)
        {
            try
            {
                if (req == null)
                {
                    return PortResult.Fail("Contract_TempContract_Save", "Check.strJson", null);
                }

                if (!string.IsNullOrWhiteSpace(req.Model))
                {
                    try
                    {
                        var modelObj = JsonSerializer.Deserialize<Contract_TempContract_CreateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (modelObj != null)
                        {
                            req.TContractCode ??= modelObj.TContractCode;
                            req.NetworkID ??= modelObj.NetworkID;
                            req.OrgID ??= modelObj.OrgID;
                            req.ContractType ??= modelObj.ContractType;
                            req.TContracName ??= modelObj.TContracName;
                            req.TContractBody ??= modelObj.TContractBody;
                            req.ContractName ??= modelObj.ContractName;
                            req.Remark ??= modelObj.Remark;
                            req.FlagActive ??= modelObj.FlagActive;
                            req.FlagIsFilePath ??= modelObj.FlagIsFilePath;
                            req.TContractFileName ??= modelObj.TContractFileName;
                            req.TContractFilePath ??= modelObj.TContractFilePath;
                            req.TContractFileSpec ??= modelObj.TContractFileSpec;
                            req.FlagCreatePartnerSignFirst ??= modelObj.FlagCreatePartnerSignFirst;
                            req.FlagCheckOrderSign ??= modelObj.FlagCheckOrderSign;
                            req.FlagSysGenAccessKey ??= modelObj.FlagSysGenAccessKey;
                            req.Lst_Contract_Attribute_Temp ??= modelObj.Lst_Contract_Attribute_Temp;
                            req.Lst_Contract_TempContractParty ??= modelObj.Lst_Contract_TempContractParty;
                            req.Lst_Contract_TempContractUser ??= modelObj.Lst_Contract_TempContractUser;
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi deserialize model string
                    }
                }

                if (string.IsNullOrWhiteSpace(req.TContractCode) && string.IsNullOrWhiteSpace(req.TContracName) && string.IsNullOrWhiteSpace(req.Model))
                {
                    return PortResult.Fail("Contract_TempContract_Save", "Check.strJson", null);
                }

                // 1. Refine & Check TContractCode (QContract.cs:5481, 5515-5526)
                var strTContractCode = StdParam(req.TContractCode);
                if (string.IsNullOrEmpty(strTContractCode))
                {
                    return PortResult.Fail("Contract_TempContract_SaveX_InvalidTContractCode", "Check.strTContractCode", req.TContractCode);
                }

                var strNetworkID = StdParam(req.NetworkID) ?? (_config?["Biz_NetworkID"] ?? "1001");
                var strOrgID = StdParam(req.OrgID) ?? (_config?["Biz_OrgID"] ?? "7200000001");
                var strContractType = StdParam(req.ContractType);
                var strTContracName = req.TContracName?.Trim() ?? "";
                var strTContractBody = req.TContractBody ?? "";
                var strContractName = req.ContractName?.Trim() ?? "";
                var strRemark = req.Remark?.Trim() ?? "";
                var strTContractFilePath = req.TContractFilePath?.Trim() ?? "";
                var strTContractFileName = req.TContractFileName?.Trim() ?? "";
                var strFlagActive = StdFlag(req.FlagActive);
                var strFlagIsFilePath = StdFlag(req.FlagIsFilePath);
                var strFlagCreatePartnerSignFirst = StdFlag(req.FlagCreatePartnerSignFirst);
                var strFlagCheckOrderSign = StdFlag(req.FlagCheckOrderSign);
                var strFlagSysGenAccessKey = "1"; // QContract.cs:5506: TConst.Flag.Active
                var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

                // 2. Check TContracName (QContract.cs:5555-5566)
                if (string.IsNullOrEmpty(strTContracName))
                {
                    return PortResult.Fail("Contract_TempContract_SaveX_InvalidTContracName", "Check.strTContracName", req.TContracName);
                }

                // 3. Check Mst_ContractType_CheckDB (QContract.cs:5605-5612)
                var contractTypeDb = await _db.Set<Mst_ContractType>()
                    .FirstOrDefaultAsync(x =>
                        EF.Functions.Like(x.NetworkID.ToLower(), PortQuery.Equal(strNetworkID), PortQuery.Esc) &&
                        EF.Functions.Like(x.ContractType.ToLower(), PortQuery.Equal(strContractType ?? ""), PortQuery.Esc));

                if (contractTypeDb == null)
                {
                    return PortResult.Fail("Mst_ContractType_CheckDB_OrganNotFound",
                        "Check.NetworkID", strNetworkID,
                        "Check.ContractType", strContractType);
                }
                if (contractTypeDb.FlagActive != "1")
                {
                    return PortResult.Fail("Mst_ContractType_CheckDB_FlagActiveNotMatched",
                        "Check.NetworkID", strNetworkID,
                        "Check.ContractType", strContractType,
                        "Check.strFlagActiveListToCheck", "1",
                        "DB.FlagActive", contractTypeDb.FlagActive);
                }

                // 4. Check trùng tên TContracName trong cùng OrgID (QContract.cs:5616-5655)
                var dup = await _db.Set<Contract_TempContract>()
                    .FirstOrDefaultAsync(x =>
                        EF.Functions.Like((x.TContracName ?? "").ToLower(), PortQuery.Equal(strTContracName), PortQuery.Esc) &&
                        EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc));

                if (dup != null && !string.Equals(strTContractCode, dup.TContractCode, StringComparison.OrdinalIgnoreCase))
                {
                    return PortResult.Fail("Contract_TempContract_SaveX_TContracNameExist",
                        "Check.strTContracName", strTContracName);
                }

                // 5. Check TContractFileName (QContract.cs:5659-5671)
                if (string.IsNullOrEmpty(strTContractFileName))
                {
                    return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName",
                        "Check.ContractCode", strTContractCode,
                        "Check.ContractFileName", strTContractFileName);
                }

                // 6. Check Contract_Attribute_Temp nếu có (QContract.cs:5759-5851)
                if (req.Lst_Contract_Attribute_Temp != null)
                {
                    foreach (var att in req.Lst_Contract_Attribute_Temp)
                    {
                        var attCode = StdParam(att.AttributeContractCode);
                        if (string.IsNullOrEmpty(attCode))
                        {
                            return PortResult.Fail("Contract_TempContract_SaveX_InvalidAttributeContractCode",
                                "Check.strTContractCode", strTContractCode,
                                "Check.strAttributeContractCode", att.AttributeContractCode);
                        }
                        if (string.IsNullOrWhiteSpace(att.AttributeValue))
                        {
                            return PortResult.Fail("Contract_TempContract_SaveX_InvalidAttributeValue",
                                "Check.strTContractCode", strTContractCode,
                                "Check.strAttributeContractCode", attCode,
                                "Check.strAttributeValue", att.AttributeValue);
                        }
                        var attDb = await _db.Set<Mst_Attribute_Contract>()
                            .FirstOrDefaultAsync(x => EF.Functions.Like(x.AttributeContractCode.ToLower(), PortQuery.Equal(attCode), PortQuery.Esc));
                        if (attDb == null)
                        {
                            return PortResult.Fail("Mst_Attribute_Contract_CheckDB_OrganNotFound",
                                "Check.AttributeContractCode", attCode);
                        }
                        if (attDb.FlagActive != "1")
                        {
                            return PortResult.Fail("Mst_Attribute_Contract_CheckDB_FlagActiveNotMatched",
                                "Check.AttributeContractCode", attCode,
                                "Check.strFlagActiveListToCheck", "1",
                                "DB.FlagActive", attDb.FlagActive);
                        }
                    }
                }

                // 7. Check Contract_TempContractUser nếu có (QContract.cs:6036-6069)
                if (req.Lst_Contract_TempContractUser != null)
                {
                    var lstAccessKey = new List<string>();
                    foreach (var u in req.Lst_Contract_TempContractUser)
                    {
                        var accessKey = StdParam(u.AccessKey);
                        if (!string.IsNullOrEmpty(accessKey))
                        {
                            if (accessKey.Length < 6 || accessKey.Length > 50)
                            {
                                return PortResult.Fail("Contract_TempContract_Save_Contract_TempContractUserInvalidAccessKeyLength",
                                    "Check.Idx", u.Idx,
                                    "Check.UserCodeSign", u.UserCodeSign,
                                    "Check.AccessKey", accessKey);
                            }
                            if (lstAccessKey.Contains(accessKey))
                            {
                                return PortResult.Fail("Contract_TempContract_Save_Contract_TempContractUserDuplicateAccessKey",
                                    "Check.AccessKey", accessKey);
                            }
                            lstAccessKey.Add(accessKey);
                        }
                    }
                }

                // 8. Lưu DB (QContract.cs:6383-6741)
                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    var existing = await _db.Set<Contract_TempContract>()
                        .FirstOrDefaultAsync(x => EF.Functions.Like(x.TContractCode.ToLower(), PortQuery.Equal(strTContractCode), PortQuery.Esc));

                    if (existing != null)
                    {
                        existing.NetworkID = strNetworkID;
                        existing.OrgID = strOrgID;
                        existing.ContractType = strContractType;
                        existing.TContracName = strTContracName;
                        existing.TContractBody = strTContractBody;
                        existing.ContractName = strContractName;
                        existing.Remark = strRemark;
                        existing.TContractFilePath = strTContractFilePath;
                        existing.TContractFileName = strTContractFileName;
                        existing.TContractFileVersion = Guid.NewGuid().ToString();
                        existing.FlagCreatePartnerSignFirst = strFlagCreatePartnerSignFirst;
                        existing.FlagCheckOrderSign = strFlagCheckOrderSign;
                        existing.FlagActive = strFlagActive;
                        existing.FlagSysGenAccessKey = strFlagSysGenAccessKey;
                        existing.LogLUDTimeUTC = DateTime.UtcNow;
                        existing.LogLUBy = userCode;
                    }
                    else
                    {
                        existing = new Contract_TempContract
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
                            FlagCreatePartnerSignFirst = strFlagCreatePartnerSignFirst,
                            FlagCheckOrderSign = strFlagCheckOrderSign,
                            FlagActive = strFlagActive,
                            FlagSysGenAccessKey = strFlagSysGenAccessKey,
                            CreateDTimeUTC = DateTime.UtcNow,
                            CreateBy = userCode,
                            CreateLUBy = userCode,
                            LogLUDTimeUTC = DateTime.UtcNow,
                            LogLUBy = userCode
                        };
                        _db.Set<Contract_TempContract>().Add(existing);
                    }

                    // Lưu Contract_Attribute_Temp nếu có
                    if (req.Lst_Contract_Attribute_Temp != null)
                    {
                        var oldAtts = await _db.Set<Contract_Attribute_Temp>()
                            .Where(x => x.TContractCode == strTContractCode)
                            .ToListAsync();
                        _db.Set<Contract_Attribute_Temp>().RemoveRange(oldAtts);

                        foreach (var att in req.Lst_Contract_Attribute_Temp)
                        {
                            _db.Set<Contract_Attribute_Temp>().Add(new Contract_Attribute_Temp
                            {
                                AttributeContractCode = StdParam(att.AttributeContractCode)!,
                                TContractCode = strTContractCode,
                                AttributeValue = att.AttributeValue?.Trim(),
                                NetworkID = strNetworkID,
                                OrgID = strOrgID,
                                FlagActive = "1",
                                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                                LogLUBy = userCode
                            });
                        }
                    }

                    // Lưu Contract_TempContractParty nếu có
                    if (req.Lst_Contract_TempContractParty != null)
                    {
                        var oldParties = await _db.Set<Contract_TempContractParty>()
                            .Where(x => x.TContractCode == strTContractCode)
                            .ToListAsync();
                        _db.Set<Contract_TempContractParty>().RemoveRange(oldParties);

                        foreach (var p in req.Lst_Contract_TempContractParty)
                        {
                            _db.Set<Contract_TempContractParty>().Add(new Contract_TempContractParty
                            {
                                TContractCode = strTContractCode,
                                PartyCode = StdParam(p.PartyCode) ?? "PARTY_A",
                                OrgID = strOrgID,
                                Description = p.Description?.Trim(),
                                CustomerCodeSys = p.CustomerCodeSys?.Trim(),
                                MST = StdParam(p.MST),
                                CustomerCode = StdParam(p.CustomerCode),
                                CustomerName = p.CustomerName?.Trim(),
                                CustomerAddress = p.CustomerAddress?.Trim(),
                                CustomerEmail = p.CustomerEmail?.Trim(),
                                CustomerPhoneNo = p.CustomerPhoneNo?.Trim(),
                                FlagActive = "1",
                                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                                LogLUBy = userCode
                            });
                        }
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    return Ok(new { Success = true, Data = existing });
                }
                catch (Exception)
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return PortResult.Fail("Contract_TempContract_Save", "Exception", ex.Message);
            }
        }

        private static string? StdParam(object? value)
        {
            var s = value?.ToString()?.Trim();
            return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
        }

        private static string StdFlag(object? value)
        {
            return value?.ToString()?.Trim() == "1" ? "1" : "0";
        }
    }

    public class LoadMst_DistrictReq
    {
        public string? ProvinceCode { get; set; }
    }

    public class LoadMst_WardReq
    {
        public string? ProvinceCode { get; set; }
        public string? DistrictCode { get; set; }
    }

    public class SearchUserReq
    {
        public string? Keyword { get; set; }
    }

    public class Contract_TempContract_CreateReq
    {
        public string? Model { get; set; }
        public string? TContractCode { get; set; }
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
        public string? ContractType { get; set; }
        public string? TContracName { get; set; }
        public string? TContractBody { get; set; }
        public string? ContractName { get; set; }
        public string? Remark { get; set; }
        public string? FlagActive { get; set; }
        public string? FlagIsFilePath { get; set; }
        public string? TContractFileSpec { get; set; }
        public string? TContractFileName { get; set; }
        public string? TContractFilePath { get; set; }
        public string? FlagCreatePartnerSignFirst { get; set; }
        public string? FlagCheckOrderSign { get; set; }
        public string? FlagSysGenAccessKey { get; set; }

        public List<Contract_Attribute_Temp>? Lst_Contract_Attribute_Temp { get; set; }
        public List<Contract_TempContractParty>? Lst_Contract_TempContractParty { get; set; }
        public List<Contract_TempContractUserReq>? Lst_Contract_TempContractUser { get; set; }
    }

    public class Contract_TempContractUserReq
    {
        public string? Idx { get; set; }
        public string? UserCodeSign { get; set; }
        public string? AccessKey { get; set; }
    }
}
