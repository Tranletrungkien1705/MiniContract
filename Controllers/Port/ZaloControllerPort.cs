using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MiniContract.Data;

namespace MiniContract.Controllers.Port;

// port 1:1 ZaloController.cs — Index
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\ZaloController.cs:12-16 (Index)
// API: không có (ZaloController chỉ là Gate MVC controller nhận callback OAuth)
// BIZ: không có
[ApiController]
[Route("api/zalo")]
public class ZaloControllerPort : ControllerBase
{
    private readonly AppDbContext? _db;

    public ZaloControllerPort(AppDbContext? db = null)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate ZaloController.cs:12-16.
    /// Chuỗi nghiệp vụ:
    /// 1. Nhận tham số oa_id và code từ body hoặc query string (callback Zalo OAuth).
    /// 2. Gán ViewBag.Code = code; (ZaloController.cs:14).
    /// 3. Trả về View() (ZaloController.cs:15).
    /// Ở tầng API port: trả về { Success = true, Code = code, Data = new { oa_id = oaId, Code = code } }.
    /// </summary>
    [HttpPost("index")]
    [HttpGet("index")]
    public IActionResult Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Zalo_IndexReq? req = null)
    {
        try
        {
            var oaId = req?.oa_id ?? Request?.Query["oa_id"].FirstOrDefault();
            if (string.IsNullOrEmpty(oaId) && Request?.Query != null && Request.Query.TryGetValue("OA_ID", out var qOaId))
            {
                oaId = qOaId.FirstOrDefault();
            }

            var code = req?.code ?? Request?.Query["code"].FirstOrDefault();
            if (string.IsNullOrEmpty(code) && Request?.Query != null && Request.Query.TryGetValue("Code", out var qCode))
            {
                code = qCode.FirstOrDefault();
            }

            return Ok(new
            {
                Success = true,
                Code = code,
                Data = new
                {
                    oa_id = oaId,
                    Code = code
                }
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Zalo_Index", "Exception", ex.Message);
        }
    }
}

public class Zalo_IndexReq
{
    public string? oa_id { get; set; }
    public string? code { get; set; }
}
