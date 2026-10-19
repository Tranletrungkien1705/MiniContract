using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniContract.Models.Port;
using MiniContract.Data;

namespace MiniContract.Controllers.Port
{
    [ApiController]
    [Route("api/loaddata/loadmst_district")]
    public class LoadDataControllerPort : ControllerBase
    {
        private readonly AppDbContext _db;

        public LoadDataControllerPort(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
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
    }

    public class LoadMst_DistrictReq
    {
        public string? ProvinceCode { get; set; }
    }
}
