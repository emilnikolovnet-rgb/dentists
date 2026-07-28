namespace Dentists.Infrastructure.Persistence;

using Dentists.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class DentistsDbContext : DbContext
{
    public DentistsDbContext(DbContextOptions<DentistsDbContext> options) : base(options)
    {
    }

    public DbSet<Dentist> Dentists { get; set; } = null!;

    public DbSet<DentistAppointment> DentistAppointments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Dentist>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CorrelationId)
                .IsRequired();

            entity.HasIndex(e => e.CorrelationId)
                .IsUnique();

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastUpdatedDate)
                .IsRequired()
                .IsConcurrencyToken()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasMany(e => e.Appointments)
                .WithOne(a => a.Dentist)
                .HasForeignKey(a => a.DentistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable("dentists");
        });

        modelBuilder.Entity<DentistAppointment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AppointmentCorrelationId)
                .IsRequired();

            // One row per booking: an event redelivered by the Appointments service must not
            // create a second copy.
            entity.HasIndex(e => e.AppointmentCorrelationId)
                .IsUnique();

            entity.Property(e => e.ScheduledDate)
                .IsRequired();

            entity.Property(e => e.LastUpdatedDate)
                .IsRequired()
                .IsConcurrencyToken()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion<string>();

            // Availability filters on dentist, then window, then status.
            entity.HasIndex(e => new { e.DentistId, e.ScheduledDate, e.Status });

            entity.ToTable("dentist_appointments");
        });
    }
}
