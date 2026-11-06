
using System.ComponentModel.DataAnnotations;
using SafeRouteSA.Web.Models;

namespace SafeRouteSA.Web.ViewModels
{
    public class IncidentCreateViewModel
    {
        [Required]
        [StringLength(150, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(IncidentCategory))]
        public IncidentCategory? Category { get; set; }

        [Required(ErrorMessage = "Please select a location.")]
        [Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Required(ErrorMessage = "Please select a location.")]
        [Range(-180, 180)]
        public decimal? Longitude { get; set; }

        [StringLength(300)]
        public string? LocationDescription { get; set; }
    }
}
