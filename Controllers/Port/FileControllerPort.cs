using Microsoft.AspNetCore.Mvc;
using MiniContract.Data;

namespace MiniContract.Controllers.Port
{
    [ApiController]
    [Route("api/file")]
    public class FileControllerPort : ControllerBase
    {
        [HttpPost("index")]
        public IActionResult Index()
        {
            return Ok(new { Success = true, Data = new { Message = "Index page loaded successfully" } });
        }

        // Port 1:1 FileController.UploadImage
        // Nguồn: FileController.cs:22-55
        [HttpPost("uploadimage")]
        public IActionResult UploadImage(IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Ok(new
                    {
                        Success = false,
                        Url = "File is empty or null."
                    });
                }

                string fileId = Guid.NewGuid().ToString("D");
                string oldFileName = file.FileName;
                var ext = System.IO.Path.GetExtension(oldFileName).ToLower();

                string tempDir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                string savedFileName = fileId + ext;
                string fullPath = System.IO.Path.Combine(tempDir, savedFileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                var request = HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
                var url = $"{baseUrl}/temp/{savedFileName}";

                return Ok(new
                {
                    Success = true,
                    FileName = oldFileName,
                    Url = url
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    Success = false,
                    Url = e.Message
                });
            }
        }
    }
}
