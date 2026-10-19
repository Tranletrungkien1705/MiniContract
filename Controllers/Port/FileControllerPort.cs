using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniContract.Data;

namespace MiniContract.Controllers.Port
{
    [ApiController]
    [Route("api/file")]
    public class FileControllerPort : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public FileControllerPort(IWebHostEnvironment env)
        {
            _env = env;
        }

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

                string tempDir = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot", "temp");
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

        // Port 1:1 FileController.UploadFile
        // Nguồn: FileController.cs:59-97
        [HttpPost("uploadfile")]
        public IActionResult UploadFile(IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Ok(new
                    {
                        success = false,
                        url = "File is empty or null."
                    });
                }

                string fileId = Guid.NewGuid().ToString("D");
                string fileName = file.FileName;
                var ext = System.IO.Path.GetExtension(fileName).ToLower();

                // Dùng logic SaveTempFile tương đương nguồn
                string tempDir = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot", "temp");
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

                double size = file.Length;
                string filesize = size.ToString();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        url = url,
                        fileId = fileId + ext,
                        fileName = fileName,
                        fileSize = filesize
                    }
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    success = false,
                    url = e.Message
                });
            }
        }
    }
}
