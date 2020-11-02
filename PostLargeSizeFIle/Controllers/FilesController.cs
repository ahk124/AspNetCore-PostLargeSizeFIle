using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

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
        [DisableRequestSizeLimit]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> OnPostAsync()
        {
            var saveDir = Path.Combine(HostEnvironment.ContentRootPath, "_FileStore");
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

            // https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads#upload-large-files-with-streaming-1
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType)) return BadRequest($"Expected a multipart request, but got {Request.ContentType}");

            var defaultFormOptions = new FormOptions();
            var boundary = Request.GetBoundary(defaultFormOptions);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (hasContentDispositionHeader && MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                {
                    var fileName = Path.GetFileName(contentDisposition.FileName.Value);
                    var savePath = Path.Combine(saveDir, fileName);

                    using var stream = System.IO.File.Create(savePath);
                    await section.Body.CopyToAsync(stream);
                }

                section = await reader.ReadNextSectionAsync();
            }

            return Redirect("complete.html");
        }
    }
}
