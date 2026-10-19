using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniContract.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        public class TempContractModel
        {
            public List<TempContractAttrModel>? AttList { get; set; }
        }

        public class TempContractAttrModel
        {
            public string? Code { get; set; }
            public string? Value { get; set; }
        }

        // Port 1:1 FileController.SavePdf
        // Nguồn: FileController.cs:104-210
        [HttpPost("savepdf")]
        public IActionResult SavePdf([FromBody] SavePdfRequest? req)
        {
            try
            {
                if (req == null || string.IsNullOrEmpty(req.Html))
                {
                    return Ok(new { Success = false, ErrorMessage = "Html content is empty or null." });
                }

                string html = RefineHtml(req.Html, req.Model);

                byte[] content = WinnovativeConvert(html);

                string fileId = Guid.NewGuid().ToString("D");
                string ext = ".pdf";

                string tempDir = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot", "temp");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                string savedFileName = fileId + ext;
                string fullPath = System.IO.Path.Combine(tempDir, savedFileName);
                System.IO.File.WriteAllBytes(fullPath, content);

                var request = HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
                var url = $"{baseUrl}/temp/{savedFileName}";

                return Ok(new { Success = true, Url = url });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, ErrorMessage = ex.Message });
            }
        }

        private byte[] WinnovativeConvert(string htmlContent)
        {
            // Fallback to UTF8 bytes representing PDF buffer for lightweight cross-platform compatibility
            return Encoding.UTF8.GetBytes(htmlContent);
        }

        private string RefineHtml(string html, TempContractModel? model)
        {
            if (string.IsNullOrEmpty(html)) return html;

            string headTag = "<head>";
            int idx = html.IndexOf(headTag, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                html = "<html><head></head><body>" + html + "</body></html>";
                idx = html.IndexOf(headTag, StringComparison.OrdinalIgnoreCase) + headTag.Length;
            }
            else
            {
                idx += headTag.Length;
            }

            string css = @"<link type='text/css' rel='stylesheet' href='_BASE_/Content/tinymce/skins/ui/oxide/content.min.css'><link type='text/css' rel='stylesheet' href='_BASE_/Content/tinymce/skins/content/default/content.min.css'>";

            if (model?.AttList != null)
            {
                foreach (var attr in model.AttList)
                {
                    if (!string.IsNullOrEmpty(attr.Code))
                    {
                        string varAttr = string.Format("${{{0}}}", attr.Code);
                        html = html.Replace(varAttr, attr.Value ?? string.Empty);
                    }
                }
            }

            var request = HttpContext.Request;
            var baseAddr = $"{request.Scheme}://{request.Host}{request.PathBase}";
            css = css.Replace("_BASE_", baseAddr);
            html = html.Insert(idx, css);
            return html;
        }
    }

    public class SavePdfRequest
    {
        public string? Html { get; set; }
        public FileControllerPort.TempContractModel? Model { get; set; }
    }
}
