namespace Dentists.Infrastructure.Persistence;

using Dentists.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class DentistsDbContext : DbContext
{
    public const string DentistsContainer = "dentists";

    public DentistsDbContext(DbContextOptions<DentistsDbContext> options) : base(options)
    {
    }

    public DbSet<Dentist> Dentists { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Every property is given its stored name explicitly. EF would otherwise use the CLR
        // name, and the document shape is not something to change later without a migration.
        modelBuilder.Entity<Dentist>(entity =>
        {
            entity.ToContainer(DentistsContainer);

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ToJsonProperty("id");

            // Partitioning on the id spreads dentists evenly and keeps each aggregate — the
            // dentist and its embedded appointments — inside one logical partition, which is
            // the only scope Cosmos writes atomically.
            entity.HasPartitionKey(e => e.Id);

            entity.Property(e => e.FirstName)
                .ToJsonProperty("firstName")
                .IsRequired();

            entity.Property(e => e.LastName)
                .ToJsonProperty("lastName")
                .IsRequired();

            entity.Property(e => e.LastUpdatedDate)
                .ToJsonProperty("lastUpdatedDate")
                .IsRequired();

            // Cosmos has no concurrency token of our own choosing; the server-maintained _etag
            // is what makes a read-modify-write fail rather than silently overwrite.
            entity.UseETagConcurrency();

            // Owned, so the appointments serialize as an array inside the dentist document
            // instead of becoming documents in their own right.
            entity.OwnsMany(e => e.Appointments, appointment =>
            {
                appointment.ToJsonProperty("appointments");

                // Without this EF keys embedded entries by their position in the array, so
                // removing one renumbers the rest. The booking's own id is stable instead.
                appointment.HasKey(a => a.AppointmentCorrelationId);

                // Named rather than left to convention only so its stored name can be set;
                // the convention picks a PascalCase one that the rest of the document isn't.
                appointment.WithOwner().HasForeignKey("DentistId");
                appointment.Property<Guid>("DentistId")
                    .ToJsonProperty("dentistId");

                appointment.Property(a => a.AppointmentCorrelationId)
                    .ToJsonProperty("appointmentCorrelationId")
                    .IsRequired();

                appointment.Property(a => a.ScheduledDate)
                    .ToJsonProperty("scheduledDate")
                    .IsRequired();

                appointment.Property(a => a.LastUpdatedDate)
                    .ToJsonProperty("lastUpdatedDate")
                    .IsRequired();

                // Stored by name so a value added to the enum cannot reinterpret existing
                // documents the way a shifted ordinal would.
                appointment.Property(a => a.Status)
                    .ToJsonProperty("status")
                    .IsRequired()
                    .HasConversion<string>();
            });
        });
    }
}
