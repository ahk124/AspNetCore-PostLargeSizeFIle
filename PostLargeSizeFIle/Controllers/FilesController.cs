using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PostLargeSizeFIle.Controllers
{
    public class FilesController : Controller
    {
        public FilesController(IWebHostEnvironment hostEnvironment)
        {
            HostEnvironment = hostEnvironment;
        }

        private IWebHostEnvironment HostEnvironment { get; }

        [HttpPost("/file")]
        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            var saveDir = Path.Combine(HostEnvironment.ContentRootPath, "_FileStore");
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

            var savePath = Path.Combine(saveDir, file.FileName);
            using var fileStream = System.IO.File.Create(savePath);

            await file.CopyToAsync(fileStream);

            return Redirect("complete.html");
        }
    }
}
