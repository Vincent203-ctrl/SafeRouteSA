using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeRouteSA.Web.Models
{
    // INCIDENT CATEGORIES
    public enum IncidentCategory
    {
        Accident = 1,
        Fire = 2,
        Theft = 3,
        MedicalEmergency = 4,
        SuspiciousActivity = 5,
        Other = 6
    }

    // INCIDENT PROGRESS STATUS
    public enum IncidentStatus
    {
        Pending = 1,
        UnderReview = 2,
        InProgress = 3,
        Resolved = 4,
        Closed = 5
    }

    // ADMIN APPROVAL STATUS
    public enum IncidentApprovalStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    public class Incident
    {
        public int Id { get; set; }

        // UNIQUE INCIDENT REFERENCE
        [Required]
        [MaxLength(50)]
        public string ReferenceNumber { get; set; } = string.Empty;

        // USER WHO SUBMITTED THE REPORT
        [Required]
        public string UserId { get; set; } = string.Empty;

        // INCIDENT INFORMATION
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public IncidentCategory Category { get; set; }

        // INCIDENT PROGRESS
        public IncidentStatus Status { get; set; }
            = IncidentStatus.Pending;

        // ADMIN APPROVAL DECISION
        public IncidentApprovalStatus ApprovalStatus { get; set; }
            = IncidentApprovalStatus.Pending;

        // GPS LOCATION
        [Column(TypeName = "decimal(9,6)")]
        public decimal Latitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal Longitude { get; set; }

        [MaxLength(300)]
        public string? LocationDescription { get; set; }

        // DATE INCIDENT WAS SUBMITTED
        public DateTime CreatedAtUtc { get; set; }
            = DateTime.UtcNow;

        // ADMIN WHO REVIEWED THE REPORT
        [MaxLength(450)]
        public string? ReviewedByUserId { get; set; }

        // DATE OF ADMIN DECISION
        public DateTime? ReviewedAtUtc { get; set; }

        // ADMIN REVIEW NOTES / REJECTION REASON
        [MaxLength(1000)]
        public string? ReviewNotes { get; set; }
    }
}
