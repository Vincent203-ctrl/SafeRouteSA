
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SafeRouteSA.Web.Models;

namespace SafeRouteSA.Web.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ==========================================
        // INCIDENT REPORTS TABLE
        // ==========================================
        public DbSet<Incident> Incidents => Set<Incident>();

        // ==========================================
        // USER NOTIFICATIONS TABLE
        // ==========================================
        public DbSet<Notification> Notifications
            => Set<Notification>();

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ==========================================
            // INCIDENT CONFIGURATION
            // ==========================================
            builder.Entity<Incident>(entity =>
            {
                // Unique incident reference number
                entity.HasIndex(i => i.ReferenceNumber)
                      .IsUnique();

                // Connect reports to registered users
                entity.HasOne<IdentityUser>()
                      .WithMany()
                      .HasForeignKey(i => i.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Admin review information
                entity.Property(i => i.ReviewedByUserId)
                      .HasMaxLength(450);

                entity.Property(i => i.ReviewNotes)
                      .HasMaxLength(1000);
            });

            // ==========================================
            // NOTIFICATION CONFIGURATION
            // ==========================================
            builder.Entity<Notification>(entity =>
            {
                // Primary key
                entity.HasKey(n => n.Id);

                // Connect notification to registered user
                entity.HasOne<IdentityUser>()
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Connect notification to incident report
                entity.HasOne<Incident>()
                      .WithMany()
                      .HasForeignKey(n => n.IncidentId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Notification title
                entity.Property(n => n.Title)
                      .IsRequired()
                      .HasMaxLength(150);

                // Notification message
                entity.Property(n => n.Message)
                      .IsRequired()
                      .HasMaxLength(1000);

                // Faster retrieval of user notifications
                entity.HasIndex(n => new
                {
                    n.UserId,
                    n.IsRead,
                    n.CreatedAtUtc
                });

                // Faster retrieval by related incident
                entity.HasIndex(n => n.IncidentId);
            });
        }
    }
}
