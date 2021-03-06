using System;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class UploadController : Controller
    {
        private readonly FilesDbContext _context;
        private readonly ILogger<UploadController> _logger;

        public UploadController(FilesDbContext context, ILogger<UploadController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult UploadFile()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> UploadFile(FormModel model)
        {

            List<IFormFile> files = model.files;
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var filePaths = new List<string>();
            foreach (IFormFile formFile in files)
            {
                if (formFile.Length > 0)
                {
                    string filePath = Path.Combine(Path.GetFullPath("StorageFiles"), userId + Path.GetFileName(formFile.FileName));
                    filePaths.Add(filePath);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }

                    _logger.LogTrace(message: $"Logging User Identity: {User.Identity?.ToString()}");
                    var f = new WebApp.Models.File()
                    {
                        Name = formFile.FileName,
                        Created = DateTime.Now,
                        Size = (int)formFile.Length,
                        Path = Path.GetFileName(filePath),
                        UserId = userId,
                    };
                    _context.Add(f);
                    _context.SaveChanges();
                }
            }
            return Redirect("/"); ;
        }

        [Authorize]
        public ActionResult Download(string path)
        {
            string fullName = Path.Combine(Path.GetFullPath("StorageFiles"), path);
            System.IO.FileStream fs = System.IO.File.OpenRead(fullName);
            byte[] data = new byte[fs.Length];
            int br = fs.Read(data, 0, data.Length);
            if (br != fs.Length)
                throw new System.IO.IOException(fullName);
            return File(
                data, System.Net.Mime.MediaTypeNames.Application.Octet, "Download" + "." + path.Split(".")[1]);
        }
    }
}
