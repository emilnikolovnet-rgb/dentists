namespace Dentists.Domain.Enums
{
    /// <summary>
    /// Mirrors Appointments.Domain.Enums.Statuses. The values are kept in the same order so an
    /// appointment status arriving from the Appointments service maps across without translation.
    /// </summary>
    public enum Statuses
    {
        Pending,
        Confirmed,
        Cancelled
    }
}
