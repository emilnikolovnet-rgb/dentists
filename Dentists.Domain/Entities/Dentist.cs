using Dentists.Domain.Enums;
using Newtonsoft.Json;

namespace Dentists.Domain.Entities;

public class Dentist
{
    /// <summary>
    /// The dentist's identity, and the identifier other services use to reference one.
    /// Doubles as the document id and partition key in Cosmos, so it is assigned by the
    /// application at construction and never by the store.
    /// </summary>
    public Guid Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public DateTime LastUpdatedDate { get; private set; }

    /// <summary>
    /// When the dentist was deleted, or null while they are active. Deleting is soft: the
    /// document stays, along with the appointments embedded in it, so bookings the Appointments
    /// service still holds are not silently destroyed.
    /// </summary>
    public DateTime? DeletedDate { get; private set; }

    /// <summary>
    /// Convenience for in-memory checks. Not mapped, and so not usable in a query — filter on
    /// <see cref="DeletedDate"/> there instead, which Cosmos can translate.
    /// </summary>
    public bool IsDeleted => DeletedDate.HasValue;

    /// <summary>
    /// Embedded in the dentist document rather than stored separately, so availability is
    /// answerable from one document and the aggregate is written atomically.
    /// </summary>
    public ICollection<DentistAppointment> Appointments { get; private set; } = new List<DentistAppointment>();

    /// <summary>
    /// Integration messages raised by changes to this dentist and not yet published. Embedded
    /// for the same reason as the appointments: they share the document's partition key, so a
    /// change and the announcement of it are one atomic write.
    /// </summary>
    public ICollection<OutboxMessage> Outbox { get; private set; } = new List<OutboxMessage>();

    /// <summary>
    /// Messages already applied to this dentist, so an at-least-once redelivery can be ignored.
    /// </summary>
    public ICollection<InboxEntry> Inbox { get; private set; } = new List<InboxEntry>();

    // Constructor
    public Dentist() { }

    public Dentist(string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void Update(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        LastUpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the dentist deleted. They stop being readable through the repository from here
    /// on, but the document and its appointments remain.
    /// </summary>
    public void MarkDeleted()
    {
        if (IsDeleted)
        {
            return;
        }

        DeletedDate = DateTime.UtcNow;
        LastUpdatedDate = DeletedDate.Value;
    }

    /// <summary>
    /// Records a booking against this dentist. One entry per booking: an event redelivered by
    /// the Appointments service must not create a second copy. Cosmos has no unique index to
    /// enforce that, so the aggregate does it — which is safe because the whole collection
    /// lives in this document and is written under a single etag check.
    /// </summary>
    public DentistAppointment AddAppointment(Guid appointmentCorrelationId, DateTime scheduledDate)
    {
        var existing = FindAppointment(appointmentCorrelationId);
        if (existing is not null)
        {
            return existing;
        }

        var appointment = new DentistAppointment(appointmentCorrelationId, scheduledDate);
        Appointments.Add(appointment);
        LastUpdatedDate = DateTime.UtcNow;

        return appointment;
    }

    /// <summary>
    /// Moves a booking to a new time and applies the status the Appointments service reports.
    /// Returns null when this dentist has no such booking.
    /// </summary>
    /// <exception cref="InvalidOperationException">The booking is cancelled.</exception>
    public DentistAppointment? UpdateAppointment(
        Guid appointmentCorrelationId,
        DateTime scheduledDate,
        Statuses status)
    {
        var appointment = FindAppointment(appointmentCorrelationId);
        if (appointment is null)
        {
            return null;
        }

        // Cancelled is terminal, and rescheduling a cancelled booking is as meaningless as
        // moving it to another status, so the whole update is refused rather than half of it.
        if (appointment.Status == Statuses.Cancelled)
        {
            throw new InvalidOperationException(
                $"Appointment {appointmentCorrelationId} is cancelled and cannot be updated.");
        }

        appointment.Reschedule(scheduledDate);

        // SetStatus rejects a no-op move off Cancelled, and re-applying the status a booking
        // already holds would only churn its LastUpdatedDate.
        if (appointment.Status != status)
        {
            appointment.SetStatus(status);
        }

        LastUpdatedDate = DateTime.UtcNow;

        return appointment;
    }

    /// <summary>
    /// Moves a booking to a new time without disturbing its status.
    /// Returns null when this dentist has no such booking.
    /// </summary>
    /// <exception cref="InvalidOperationException">The booking is cancelled.</exception>
    public DentistAppointment? RescheduleAppointment(Guid appointmentCorrelationId, DateTime scheduledDate)
    {
        var appointment = FindAppointment(appointmentCorrelationId);
        if (appointment is null)
        {
            return null;
        }

        if (appointment.Status == Statuses.Cancelled)
        {
            throw new InvalidOperationException(
                $"Appointment {appointmentCorrelationId} is cancelled and cannot be rescheduled.");
        }

        if (appointment.ScheduledDate == scheduledDate)
        {
            return appointment;
        }

        appointment.Reschedule(scheduledDate);
        LastUpdatedDate = DateTime.UtcNow;

        return appointment;
    }

    /// <summary>
    /// Whether this dentist has a live booking overlapping [<paramref name="from"/>,
    /// <paramref name="to"/>), ignoring <paramref name="exceptAppointment"/>. The in-memory
    /// counterpart of the availability query, for checking one dentist already loaded.
    /// </summary>
    public bool HasConflict(DateTime from, DateTime to, Guid? exceptAppointment = null)
    {
        return Appointments.Any(a =>
            a.Status != Statuses.Cancelled
            && a.AppointmentCorrelationId != exceptAppointment
            && a.ScheduledDate >= from
            && a.ScheduledDate < to);
    }

    /// <summary>
    /// Applies a status to a booking without disturbing when it is scheduled.
    /// Returns null when this dentist has no such booking.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The booking is cancelled and <paramref name="status"/> would move it off Cancelled.
    /// </exception>
    public DentistAppointment? SetAppointmentStatus(Guid appointmentCorrelationId, Statuses status)
    {
        var appointment = FindAppointment(appointmentCorrelationId);
        if (appointment is null)
        {
            return null;
        }

        // Re-asserting the status a booking already holds is what a redelivered event looks
        // like, so it is accepted and ignored. Taking this branch first is also what lets a
        // repeated cancellation through, which SetStatus would otherwise refuse as terminal.
        if (appointment.Status == status)
        {
            return appointment;
        }

        appointment.SetStatus(status);
        LastUpdatedDate = DateTime.UtcNow;

        return appointment;
    }

    /// <summary>
    /// Drops a booking from this dentist entirely, for when the Appointments service reports
    /// one that never should have reached us. Returns false when there is no such booking.
    /// </summary>
    public bool RemoveAppointment(Guid appointmentCorrelationId)
    {
        var appointment = FindAppointment(appointmentCorrelationId);
        if (appointment is null)
        {
            return false;
        }

        Appointments.Remove(appointment);
        LastUpdatedDate = DateTime.UtcNow;

        return true;
    }

    public DentistAppointment? FindAppointment(Guid appointmentCorrelationId)
    {
        return Appointments.FirstOrDefault(a => a.AppointmentCorrelationId == appointmentCorrelationId);
    }

    // ---- Outbox ----------------------------------------------------------------------

    /// <summary>
    /// Queues an integration message to be published once this dentist is saved. Call it in the
    /// same unit of work as the change it announces — that is the whole point.
    /// </summary>
    public OutboxMessage EnqueueOutbox(Guid messageId, string messageType, string payload)
    {
        var existing = Outbox.FirstOrDefault(m => m.MessageId == messageId);
        if (existing is not null)
        {
            return existing;
        }

        var message = new OutboxMessage(messageId, messageType, payload);
        Outbox.Add(message);
        LastUpdatedDate = DateTime.UtcNow;

        return message;
    }

    public IEnumerable<OutboxMessage> PendingOutbox()
    {
        return Outbox.Where(m => !m.IsDispatched).OrderBy(m => m.EnqueuedAt);
    }

    /// <summary>
    /// Records that a queued message reached the transport. Returns false if there is no such
    /// pending message, which is what a duplicate dispatch looks like.
    /// </summary>
    public bool MarkDispatched(Guid messageId)
    {
        var message = Outbox.FirstOrDefault(m => m.MessageId == messageId && !m.IsDispatched);
        if (message is null)
        {
            return false;
        }

        message.MarkDispatched();
        LastUpdatedDate = DateTime.UtcNow;

        return true;
    }

    // ---- Inbox -----------------------------------------------------------------------

    public bool HasConsumed(Guid messageId)
    {
        return Inbox.Any(e => e.MessageId == messageId);
    }

    /// <summary>
    /// Notes that a message has been applied. Returns false if it already had been, which is
    /// the caller's signal to do nothing rather than apply it twice.
    /// </summary>
    public bool RecordConsumed(Guid messageId)
    {
        if (HasConsumed(messageId))
        {
            return false;
        }

        Inbox.Add(new InboxEntry(messageId));
        LastUpdatedDate = DateTime.UtcNow;

        return true;
    }

    // ---- Housekeeping ----------------------------------------------------------------

    /// <summary>
    /// Drops dispatched outbox messages and inbox entries older than <paramref name="cutoff"/>.
    /// <para>
    /// Not optional. Both collections live inside this document, and a Cosmos document cannot
    /// exceed 2 MB — left alone they would eventually make the dentist unwritable. The cutoff
    /// is the duplicate-detection window: past it, a redelivery is no longer expected, so the
    /// record of having handled it is no longer worth its space.
    /// </para>
    /// </summary>
    /// <returns>How many entries were removed.</returns>
    public int PruneMessages(DateTime cutoff)
    {
        var staleOutbox = Outbox.Where(m => m.IsDispatched && m.DispatchedAt < cutoff).ToList();
        var staleInbox = Inbox.Where(e => e.ConsumedAt < cutoff).ToList();

        foreach (var message in staleOutbox)
        {
            Outbox.Remove(message);
        }

        foreach (var entry in staleInbox)
        {
            Inbox.Remove(entry);
        }

        var removed = staleOutbox.Count + staleInbox.Count;
        if (removed > 0)
        {
            LastUpdatedDate = DateTime.UtcNow;
        }

        return removed;
    }
}
