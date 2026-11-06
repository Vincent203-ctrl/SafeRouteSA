
using SafeRouteSA.Web.Models;

namespace SafeRouteSA.Web.ViewModels
{
    public class AdminIncidentDetailsViewModel
    {
        public Incident Incident { get; set; } = null!;

        public string ReporterEmail { get; set; } =
            "Unknown reporter";

        public string ReviewerEmail { get; set; } =
            "Not reviewed";
    }
}
