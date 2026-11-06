
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeRouteSA.Web.Data;

namespace SafeRouteSA.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // VIEW MY NOTIFICATIONS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAtUtc)
                .ToListAsync();

            return View(notifications);
        }

        // ==========================================
        // MARK ONE NOTIFICATION AS READ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            await _context.Notifications
                .Where(n => n.Id == id &&
                            n.UserId == userId &&
                            !n.IsRead)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, DateTime.UtcNow));

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // MARK ALL NOTIFICATIONS AS READ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, DateTime.UtcNow));

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // OPEN RELATED INCIDENT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> OpenIncident(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var notification = await _context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n =>
                    n.Id == id && n.UserId == userId);

            if (notification == null)
                return NotFound();

            if (notification.IncidentId == null)
                return RedirectToAction(nameof(Index));

            // Mark the notification as read
            await _context.Notifications
                .Where(n => n.Id == id &&
                            n.UserId == userId &&
                            !n.IsRead)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, DateTime.UtcNow));

            if (User.IsInRole("Admin"))
            {
                return RedirectToAction(
                    "Details",
                    "Admin",
                    new { id = notification.IncidentId.Value });
            }

            return RedirectToAction(
                "Details",
                "Incidents",
                new { id = notification.IncidentId.Value });
        }
    }
}
