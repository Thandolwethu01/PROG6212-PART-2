
using Microsoft.EntityFrameworkCore;
using RaceDay.Models.API.Models;

namespace RaceDay.Data.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventOrganiser> EventOrganisers => Set<EventOrganiser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organiser>()
            .HasIndex(x => x.ContactInfo)
            .IsUnique();

        modelBuilder.Entity<Participant>()
            .HasIndex(x => x.ContactInfo)
            .IsUnique();

        modelBuilder.Entity<EventOrganiser>()
            .HasOne(x => x.Event)
            .WithMany(x => x.EventOrganisers)
            .HasForeignKey(x => x.EventID);

        modelBuilder.Entity<EventOrganiser>()
            .HasOne(x => x.Organiser)
            .WithMany(x => x.EventOrganisers)
            .HasForeignKey(x => x.OrganizerID);

        modelBuilder.Entity<EventOrganiser>()
            .HasIndex(x => new { x.EventID, x.OrganizerID })
            .IsUnique();

        modelBuilder.Entity<Category>()
            .Property(x => x.Distance)
            .HasPrecision(8, 2);

        modelBuilder.Entity<Registration>()
            .HasIndex(x => new { x.ParticipantID, x.CategoryID })
            .IsUnique();

        modelBuilder.Entity<Registration>()
            .HasOne(x => x.Result)
            .WithOne(x => x.Registration)
            .HasForeignKey<Result>(x => x.RegistrationID);

        modelBuilder.Entity<Result>()
            .HasIndex(x => x.RegistrationID)
            .IsUnique();
    }
}
