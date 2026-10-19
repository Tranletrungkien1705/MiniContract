using Microsoft.AspNetCore.Mvc;
using MiniContract.Data;

namespace MiniContract.Controllers.Port
{
    [ApiController]
    [Route("api/file")]
    public class FileControllerPort : ControllerBase
    {
        // Port 1:1 FileController.Index -> View() -> static HTML/Content or simple View equivalent for static page
        [HttpPost("index")]
        public IActionResult Index()
        {
            // No backend call, pure UI rendering index action
            return Ok(new { Success = true, Data = new { Message = "Index page loaded successfully" } });
        }
    }
}
