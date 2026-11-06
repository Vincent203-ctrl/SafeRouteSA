
using SafeRouteSA.Web.Models;

namespace SafeRouteSA.Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int ApprovedReports { get; set; }
        public int RejectedReports { get; set; }

        // Search functionality
        public string SearchTerm { get; set; } = string.Empty;

        public int SearchResultCount { get; set; }

        // Filter functionality
        public string StatusFilter { get; set; } = "All";

        public List<Incident> Incidents { get; set; } = new();
    }
}
