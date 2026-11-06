using AgriStream.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.web.Controllers
{
    [Authorize(Roles = "Farmer,Agronomist,Admin")]
    public class DashboardController : Controller
    {
        private readonly FarmFunctionsService _functions;

        public DashboardController(FarmFunctionsService functions)
        {
            _functions = functions;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var readings = await _functions.GetLatestAsync(
                cancellationToken);

            return View(readings);
        }

        [HttpPost]
        [Authorize(Roles = "Farmer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Irrigate(
            string deviceId,
            CancellationToken cancellationToken)
        {
            var allowedDevices = new[]
            {
                "SENSOR-001", "SENSOR-002", "SENSOR-003"
            };

            if (!allowedDevices.Contains(deviceId))
            {
                return BadRequest("Invalid device.");
            }

            await _functions.TriggerIrrigationAsync(
                deviceId, cancellationToken);

            TempData["Success"] =
                $"Irrigation command queued for {deviceId}.";

            return RedirectToAction(nameof(Index));
        }
    }
}