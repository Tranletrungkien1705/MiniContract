using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] Contract_TempContract_DeleteReq? req)
        {
            if (req == null)
            {
                return Ok(new { Success = true, Messages = new[] { "Mã mẫu hợp đồng không hợp lệ" } });
            }

            List<Contract_TempContractItem>? items = null;
            if (!string.IsNullOrEmpty(req.Model))
            {
                try
                {
                    var parsedInput = JsonSerializer.Deserialize<Contract_TempContractInputModel>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    items = parsedInput?.Lst_Contract_TempContract;
                }
                catch
                {
                    try
                    {
                        items = JsonSerializer.Deserialize<List<Contract_TempContractItem>>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch { }
                }
            }

            if (items == null && req.Lst_Contract_TempContract != null)
            {
                items = req.Lst_Contract_TempContract;
            }

            if (items == null || items.Count == 0)
            {
                return Ok(new { Success = true, Messages = new[] { "Mã mẫu hợp đồng không hợp lệ" } });
            }

            var listTContractCode = items.Where(x => !string.IsNullOrEmpty(x.TContractCode)).Select(x => x.TContractCode!).Distinct().ToList();
            if (listTContractCode.Count == 0)
            {
                return Ok(new { Success = true, Messages = new[] { "Mã mẫu hợp đồng không hợp lệ" } });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var code in listTContractCode)
                {
                    var entity = await _db.Set<Contract_TempContract>().FirstOrDefaultAsync(x => x.TContractCode == code);
                    if (entity != null)
                    {
                        _db.Set<Contract_TempContract>().Remove(entity);
                    }
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var message = listTContractCode.Count == 1 
                    ? $"Xóa mẫu hợp đồng '{listTContractCode[0]}' thành công!" 
                    : "Xóa mẫu hợp đồng thành công!";

                return Ok(new { Success = true, Messages = new[] { message } });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_TempContract_Delete", "Exception", ex.Message);
            }
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

    public class Contract_TempContract_DeleteReq
    {
        public string? Model { get; set; }
        public List<Contract_TempContractItem>? Lst_Contract_TempContract { get; set; }
    }

    public class Contract_TempContractInputModel
    {
        public List<Contract_TempContractItem>? Lst_Contract_TempContract { get; set; }
    }

    public class Contract_TempContractItem
    {
        public string? TContractCode { get; set; }
    }
}
