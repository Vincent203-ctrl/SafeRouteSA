
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeRouteSA.Web.Data;
using SafeRouteSA.Web.Models;
using SafeRouteSA.Web.ViewModels;

namespace SafeRouteSA.Web.Controllers
{
    [Authorize]
    public class IncidentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IncidentsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ==========================================
        // MY REPORTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string? userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var incidents = await _context.Incidents
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.CreatedAtUtc)
                .ToListAsync();

            return View(incidents);
        }

        // ==========================================
        // CREATE INCIDENT - GET
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new IncidentCreateViewModel());
        }

        // ==========================================
        // CREATE INCIDENT - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            IncidentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            // Create incident report
            var incident = new Incident
            {
                ReferenceNumber =
                    $"SR-{Guid.NewGuid():N}".ToUpperInvariant(),

                UserId = userId,

                Title = model.Title.Trim(),

                Description = model.Description.Trim(),

                Category = model.Category!.Value,

                Latitude = model.Latitude!.Value,

                Longitude = model.Longitude!.Value,

                LocationDescription =
                    model.LocationDescription?.Trim(),

                Status = IncidentStatus.Pending,

                ApprovalStatus = IncidentApprovalStatus.Pending,

                ReviewedByUserId = null,
                ReviewedAtUtc = null,
                ReviewNotes = null,

                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Incidents.Add(incident);

            // Save incident first to generate its ID
            await _context.SaveChangesAsync();

            // ==========================================
            // NOTIFY ALL ADMINISTRATORS
            // ==========================================

            // Find Admin role
            var adminRole = await _roleManager
                .FindByNameAsync("Admin");

            if (adminRole != null)
            {
                // Find users assigned to Admin role
                var adminUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var adminId in adminUserIds)
                {
                    var notification = new Notification
                    {
                        UserId = adminId,

                        Title = "New Incident Report",

                        Message =
                            $"A new incident report " +
                            $"({incident.ReferenceNumber}) " +
                            $"has been submitted and is " +
                            $"awaiting your review.",

                        Type = NotificationType.IncidentSubmitted,

                        IncidentId = incident.Id,

                        IsRead = false,

                        CreatedAtUtc = DateTime.UtcNow,

                        ReadAtUtc = null
                    };

                    _context.Notifications.Add(notification);
                }

                // Save administrator notifications
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] =
                "Your incident report was submitted successfully " +
                "and is now awaiting administrator review.";

            return RedirectToAction(
                nameof(Details),
                new { id = incident.Id });
        }

        // ==========================================
        // VIEW INCIDENT DETAILS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            string? userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var incident = await _context.Incidents
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.UserId == userId);

            if (incident == null)
            {
                return NotFound();
            }

            return View(incident);
        }
    }
}
