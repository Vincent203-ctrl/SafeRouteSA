using System;
using System.ComponentModel.DataAnnotations;

namespace SafeRouteSA.Web.Models
{
    public enum NotificationType
    {
        IncidentSubmitted = 1,
        IncidentApproved = 2,
        IncidentRejected = 3
    }

    public class Notification
    {
        public int Id { get; set; }

        // USER RECEIVING THE NOTIFICATION
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        // NOTIFICATION TITLE
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        // NOTIFICATION MESSAGE
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        // TYPE OF NOTIFICATION
        public NotificationType Type { get; set; }

        // RELATED INCIDENT
        public int? IncidentId { get; set; }

        // READ / UNREAD STATUS
        public bool IsRead { get; set; } = false;

        // DATE CREATED
        public DateTime CreatedAtUtc { get; set; }
            = DateTime.UtcNow;

        // DATE READ
        public DateTime? ReadAtUtc { get; set; }
    }
}
