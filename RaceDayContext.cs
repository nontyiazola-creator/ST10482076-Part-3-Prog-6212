using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Unique constraint matching your Part 1 SQL script:
        // UQ_Enrolment_Unique UNIQUE (ParticipantID, EventID, CategoryID)
        mb.Entity<Enrolment>()
          .HasIndex(e => new { e.ParticipantID, e.EventID, e.CategoryID })
          .IsUnique();

        // Result.EnrolmentID is UNIQUE in your SQL script (1-to-1)
        mb.Entity<Result>()
          .HasIndex(r => r.EnrolmentID)
          .IsUnique();

        // Event -> Organiser (User). Restrict = don't cascade-delete users' events
        mb.Entity<Event>()
          .HasOne(e => e.Organiser)
          .WithMany(u => u.Events)
          .HasForeignKey(e => e.OrganiserID)
          .OnDelete(DeleteBehavior.Restrict);

        // FIX: Enrolment's 3 foreign keys must be Restrict (SQL Server error 1785)
        mb.Entity<Enrolment>()
          .HasOne(e => e.Event)
          .WithMany(ev => ev.Enrolments)
          .HasForeignKey(e => e.EventID)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Enrolment>()
          .HasOne(e => e.Category)
          .WithMany(c => c.Enrolments)
          .HasForeignKey(e => e.CategoryID)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Enrolment>()
          .HasOne(e => e.Participant)
          .WithMany(u => u.Enrolments)
          .HasForeignKey(e => e.ParticipantID)
          .OnDelete(DeleteBehavior.Restrict);

        // Seed the two roles from your SQL script
        mb.Entity<Role>().HasData(
            new Role { RoleID = 1, RoleName = "Organiser" },
            new Role { RoleID = 2, RoleName = "Participant" });

        // DECIMAL(6,2) to match your SQL script
        mb.Entity<Event>()
          .Property(e => e.Distance)
          .HasPrecision(6, 2);
    }
}