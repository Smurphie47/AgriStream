using AgriStream.web.Models;
using AgriStream.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.web.Controllers
{
    [Authorize(Roles = "Farmer,Agronomist,Admin")]
    public class DroneController : Controller
    {
        private readonly DroneImageService _images;

        public DroneController(DroneImageService images)
        {
            _images = images;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View(new DroneUploadViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            DroneUploadViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid || model.Image == null)
            {
                return View(model);
            }

            try
            {
                var blobName = await _images.UploadAsync(
                    model.Image, cancellationToken);

                TempData["Success"] =
                    $"Image uploaded: {blobName}. " +
                    "The processing audit will be recorded asynchronously.";

                return RedirectToAction(nameof(Upload));
            }
            catch (ArgumentException exception)
            {
                ModelState.AddModelError(
                    nameof(model.Image), exception.Message);

                return View(model);
            }
        }
    }
}