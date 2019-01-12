using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace Benchmarks.UI.Server.Controllers
{
    [Route("api/download")]
    public class DownloadController : ControllerBase
    {
        static string _driverPath = @"C:\Users\sebros\Documents\Projects\benchmarks\src\BenchmarksDriver\bin\Debug\netcoreapp2.1";

        [HttpGet]
        [Route("{filename}")]
        [RequestSizeLimit(100_000_000)]
        public IActionResult Download(string filename)
        {
            return File(System.IO.File.OpenRead(Path.Combine(_driverPath, filename)), "application/object");
        }
    }
}
