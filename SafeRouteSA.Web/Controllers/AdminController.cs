
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeRouteSA.Web.Data;
using SafeRouteSA.Web.Models;
using SafeRouteSA.Web.ViewModels;

namespace SafeRouteSA.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // ADMIN DASHBOARD - SEARCH AND FILTER
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] string? searchTerm,
            [FromQuery] string? statusFilter)
        {
            searchTerm = searchTerm?.Trim();

            if (searchTerm?.Length > 150)
            {
                searchTerm = searchTerm[..150];
            }

            var allowedFilters = new[]
            {
                "All",
                "Pending",
                "Approved",
                "Rejected"
            };

            statusFilter = allowedFilters.FirstOrDefault(
                f => string.Equals(
                    f,
                    statusFilter,
                    StringComparison.OrdinalIgnoreCase)) ?? "All";

            var incidentsQuery = _context.Incidents
                .AsNoTracking()
                .AsQueryable();

            // SEARCH REPORTS
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                incidentsQuery = incidentsQuery.Where(i =>
                    i.Title.Contains(searchTerm) ||
                    i.ReferenceNumber.Contains(searchTerm) ||
                    (i.LocationDescription != null &&
                     i.LocationDescription.Contains(searchTerm)));
            }

            // FILTER REPORTS
            switch (statusFilter)
            {
                case "Pending":
                    incidentsQuery = incidentsQuery.Where(i =>
                        i.ApprovalStatus ==
                        IncidentApprovalStatus.Pending);
                    break;

                case "Approved":
                    incidentsQuery = incidentsQuery.Where(i =>
                        i.ApprovalStatus ==
                        IncidentApprovalStatus.Approved);
                    break;

                case "Rejected":
                    incidentsQuery = incidentsQuery.Where(i =>
                        i.ApprovalStatus ==
                        IncidentApprovalStatus.Rejected);
                    break;
            }

            var model = new AdminDashboardViewModel
            {
                TotalReports = await _context.Incidents
                    .CountAsync(),

                PendingReports = await _context.Incidents
                    .CountAsync(i =>
                        i.ApprovalStatus ==
                        IncidentApprovalStatus.Pending),

                ApprovedReports = await _context.Incidents
                    .CountAsync(i =>
                        i.ApprovalStatus ==
                        IncidentApprovalStatus.Approved),

                RejectedReports = await _context.Incidents
                    .CountAsync(i =>
                        i.ApprovalStatus ==
                        IncidentApprovalStatus.Rejected),

                SearchTerm = searchTerm ?? string.Empty,

                StatusFilter = statusFilter,

                SearchResultCount =
                    await incidentsQuery.CountAsync(),

                Incidents = await incidentsQuery
                    .OrderByDescending(i => i.CreatedAtUtc)
                    .ToListAsync()
            };

            return View(model);
        }

        // ==========================================
        // VIEW INCIDENT DETAILS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var incident = await _context.Incidents
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (incident == null)
            {
                return NotFound();
            }

            var reporter = await _userManager
                .FindByIdAsync(incident.UserId);

            IdentityUser? reviewer = null;

            if (!string.IsNullOrWhiteSpace(
                incident.ReviewedByUserId))
            {
                reviewer = await _userManager
                    .FindByIdAsync(incident.ReviewedByUserId);
            }

            var model = new AdminIncidentDetailsViewModel
            {
                Incident = incident,

                ReporterEmail = reporter?.Email
                    ?? "Unknown reporter",

                ReviewerEmail = reviewer?.Email
                    ?? "Not recorded"
            };

            return View(model);
        }

        // ==========================================
        // APPROVE INCIDENT + NOTIFY USER
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int id,
            string? reviewNotes)
        {
            if (reviewNotes?.Length > 1000)
            {
                TempData["ErrorMessage"] =
                    "Review notes cannot exceed 1000 characters.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            var reviewerId = _userManager.GetUserId(User);

            if (reviewerId == null)
            {
                return Challenge();
            }

            var notes = string.IsNullOrWhiteSpace(reviewNotes)
                ? "Approved by SafeRoute SA administrator."
                : reviewNotes.Trim();

            var reviewedAtUtc = DateTime.UtcNow;

            // Use a transaction so the decision and notification
            // are saved together.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Only Pending reports can be approved
            var affectedRows = await _context.Incidents
                .Where(i =>
                    i.Id == id &&
                    i.ApprovalStatus ==
                    IncidentApprovalStatus.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        i => i.ApprovalStatus,
                        IncidentApprovalStatus.Approved)
                    .SetProperty(
                        i => i.Status,
                        IncidentStatus.UnderReview)
                    .SetProperty(
                        i => i.ReviewedByUserId,
                        reviewerId)
                    .SetProperty(
                        i => i.ReviewedAtUtc,
                        reviewedAtUtc)
                    .SetProperty(
                        i => i.ReviewNotes,
                        notes));

            if (affectedRows == 0)
            {
                var incidentExists = await _context.Incidents
                    .AnyAsync(i => i.Id == id);

                TempData["ErrorMessage"] = incidentExists
                    ? "This report has already been reviewed."
                    : "Incident not found.";

                return incidentExists
                    ? RedirectToAction(nameof(Details), new { id })
                    : NotFound();
            }

            // Retrieve the approved incident
            var incident = await _context.Incidents
                .AsNoTracking()
                .FirstAsync(i => i.Id == id);

            // Create notification for the reporter
            var notification = new Notification
            {
                UserId = incident.UserId,

                Title = "Incident Approved",

                Message =
                    $"Your incident report " +
                    $"({incident.ReferenceNumber}) " +
                    $"has been approved by the administrator. " +
                    $"Open your report to view the review notes.",

                Type = NotificationType.IncidentApproved,

                IncidentId = incident.Id,

                IsRead = false,

                CreatedAtUtc = reviewedAtUtc,

                ReadAtUtc = null
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "Incident approved and user notified successfully.";

            return RedirectToAction(
                nameof(Details), new { id });
        }

        // ==========================================
        // REJECT INCIDENT + NOTIFY USER
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? reviewNotes)
        {
            if (string.IsNullOrWhiteSpace(reviewNotes) ||
                reviewNotes.Trim().Length > 1000)
            {
                TempData["ErrorMessage"] =
                    "Please provide a rejection reason " +
                    "of up to 1000 characters.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            var reviewerId = _userManager.GetUserId(User);

            if (reviewerId == null)
            {
                return Challenge();
            }

            var notes = reviewNotes.Trim();
            var reviewedAtUtc = DateTime.UtcNow;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Only Pending reports can be rejected
            var affectedRows = await _context.Incidents
                .Where(i =>
                    i.Id == id &&
                    i.ApprovalStatus ==
                    IncidentApprovalStatus.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        i => i.ApprovalStatus,
                        IncidentApprovalStatus.Rejected)
                    .SetProperty(
                        i => i.Status,
                        IncidentStatus.Closed)
                    .SetProperty(
                        i => i.ReviewedByUserId,
                        reviewerId)
                    .SetProperty(
                        i => i.ReviewedAtUtc,
                        reviewedAtUtc)
                    .SetProperty(
                        i => i.ReviewNotes,
                        notes));

            if (affectedRows == 0)
            {
                var incidentExists = await _context.Incidents
                    .AnyAsync(i => i.Id == id);

                TempData["ErrorMessage"] = incidentExists
                    ? "This report has already been reviewed."
                    : "Incident not found.";

                return incidentExists
                    ? RedirectToAction(nameof(Details), new { id })
                    : NotFound();
            }

            // Retrieve the rejected incident
            var incident = await _context.Incidents
                .AsNoTracking()
                .FirstAsync(i => i.Id == id);

            // Create notification for the reporter
            var notification = new Notification
            {
                UserId = incident.UserId,

                Title = "Incident Rejected",

                Message =
                    $"Your incident report " +
                    $"({incident.ReferenceNumber}) " +
                    $"has been rejected by the administrator. " +
                    $"Open your report to read the rejection reason.",

                Type = NotificationType.IncidentRejected,

                IncidentId = incident.Id,

                IsRead = false,

                CreatedAtUtc = reviewedAtUtc,

                ReadAtUtc = null
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "Incident rejected and user notified successfully.";

            return RedirectToAction(
                nameof(Details), new { id });
        }
    }
}
