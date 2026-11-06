using AgriStream.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.web.Controllers
{
    [Authorize(Roles = "Admin,Agronomist")]
    public class TelemetryController : Controller
    {
        private readonly TelemetryService _telemetry;

        public TelemetryController(TelemetryService telemetry)
        {
            _telemetry = telemetry;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate()
        {
            var count = await _telemetry.GenerateSampleReadingsAsync();

            TempData["Success"] =
                $"{count} simulated sensor readings saved to Azure Table Storage.";

            return RedirectToAction(nameof(Index));
        }
    }
}