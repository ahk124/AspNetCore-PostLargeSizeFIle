using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace PostLargeSizeFIle.Controllers
{
    public class FilesController : Controller
    {
        public FilesController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        [HttpPost("/file")]
        [DisableRequestSizeLimit]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> OnPostAsync()
        {
            var connectionString = Configuration.GetConnectionString("FileStore");
            var containerClient = new BlobContainerClient(connectionString, "filestore");
            await containerClient.CreateIfNotExistsAsync();

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
                    await containerClient.UploadBlobAsync(fileName, section.Body);
                }

                section = await reader.ReadNextSectionAsync();
            }

            return Redirect("complete.html");
        }
    }
}
