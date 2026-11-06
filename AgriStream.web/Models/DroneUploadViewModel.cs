using System.ComponentModel.DataAnnotations;

namespace AgriStream.web.Models
{
    public class DroneUploadViewModel
    {
        [Required(ErrorMessage = "Please select an image.")]
        [Display(Name = "Drone image")]
        public IFormFile? Image { get; set; }
    }
}