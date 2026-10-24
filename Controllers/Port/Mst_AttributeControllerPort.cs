using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ProductController.Mst_Attribute_Create
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ProductController.cs:5318-5352
[ApiController]
[Route("api/mst_attribute")]
public class Mst_AttributeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_AttributeControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Attribute_CreateReq? req)
    {
        string strErrorCodeDefault = "Mst_Attribute_Create";
        if (req == null)
            return PortResult.Fail(strErrorCodeDefault, "Check.strJson", null);

        req.AttributeName = StdStr(req.AttributeName);
        if (string.IsNullOrEmpty(req.AttributeName))
            return PortResult.Fail(strErrorCodeDefault, "Check.AttributeName", null);

        try
        {
            var seqCode = await PortSeq.NextAsync<Seq_AttributeCode>(_db);
            var strAttributeCode = $"ATTR_{seqCode:D6}";

            var item = new Mst_Attribute
            {
                AttributeCode = strAttributeCode,
                AttributeName = req.AttributeName
            };

            return Ok(new
            {
                Success = true,
                Data = item
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail(strErrorCodeDefault, "Exception", ex.Message);
        }
    }

    private static string? StdStr(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();
    }
}

public class Mst_Attribute_CreateReq
{
    public string? AttributeCode { get; set; }
    public string? AttributeName { get; set; }
}
