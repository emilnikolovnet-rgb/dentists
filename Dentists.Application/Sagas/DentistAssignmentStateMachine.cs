namespace Dentists.Application.Sagas;

using Appointments.Application.Events;
using Appointments.Application.Messages;
using Dentists.Contracts.Events;
using Dentists.Contracts.Messages;
using Dentists.Domain.Enums;
using MassTransit;

/// <summary>
/// Assigns a real dentist to an appointment booked in the Appointments service, and keeps that
/// dentist's copy of the booking in step for the rest of its life.
///
/// AppointmentCreated starts the saga in Reserving and asks for a dentist. A successful
/// reservation asks the Appointments service to confirm; a failed one compensates by asking it
/// to cancel. From then on the saga forwards the states that service reports — confirmed,
/// cancelled, rescheduled — to whichever dentist it reserved.
///
/// The saga does no data access of its own. Every change to a dentist goes through a command
/// to a consumer, because that is where the business change, the inbox entry and the outbox
/// message can be committed together.
/// </summary>
public class DentistAssignmentStateMachine : MassTransitStateMachine<DentistAssignmentState>
{
    public State Reserving { get; private set; } = null!;
    public State AwaitingConfirmation { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<AppointmentCreated> Created { get; private set; } = null!;
    public Event<AppointmentUpdated> Updated { get; private set; } = null!;
    public Event<AppointmentConfirmed> AppointmentWasConfirmed { get; private set; } = null!;
    public Event<AppointmentCancelled> AppointmentWasCancelled { get; private set; } = null!;

    public Event<DentistReserved> Reserved { get; private set; } = null!;
    public Event<DentistReservationFailed> ReservationFailed { get; private set; } = null!;

    public DentistAssignmentStateMachine()
    {
        // SendAsync, not Send, throughout. context.Init<T>() returns a Task, and the
        // synchronous Send overload binds against that Task as though it were the message —
        // which fails at run time looking for an endpoint for Task<SendTuple<T>>, not at
        // compile time.
        InstanceState(x => x.CurrentState);

        // Every message in this workflow carries the appointment's CorrelationId, so the whole
        // saga correlates on one value and no lookup by any other key is ever needed.
        Event(() => Created, e => e.CorrelateById(context => context.Message.CorrelationId));
        Event(() => Updated, e => e.CorrelateById(context => context.Message.CorrelationId));
        Event(() => AppointmentWasConfirmed, e => e.CorrelateById(context => context.Message.CorrelationId));
        Event(() => AppointmentWasCancelled, e => e.CorrelateById(context => context.Message.CorrelationId));
        Event(() => Reserved, e => e.CorrelateById(context => context.Message.CorrelationId));
        Event(() => ReservationFailed, e => e.CorrelateById(context => context.Message.CorrelationId));

        Initially(
            When(Created)
                .Then(context =>
                {
                    context.Saga.AppointmentId = context.Message.AppointmentId;
                    context.Saga.ScheduledDate = context.Message.ScheduledDate;
                })
                .SendAsync(context => context.Init<ReserveDentist>(new
                {
                    context.Saga.CorrelationId,
                    context.Saga.AppointmentId,
                    context.Saga.ScheduledDate
                }))
                .TransitionTo(Reserving));

        During(Reserving,
            When(Reserved)
                .Then(context =>
                {
                    context.Saga.DentistId = context.Message.DentistId;
                    context.Saga.ReservedAt = context.Message.ReservedAt;
                })
                // The Appointments service's public inbound contract. Reaching it means a real
                // dentist is now holding the slot, so the appointment is safe to confirm.
                .SendAsync(context => context.Init<ConfirmAppointmentRequested>(new
                {
                    context.Saga.AppointmentId,
                    Source = "Dentists.DentistAssignmentSaga"
                }))
                .TransitionTo(AwaitingConfirmation),

            // Compensation: nobody is free, so the appointment cannot stand.
            When(ReservationFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .SendAsync(context => context.Init<CancelAppointment>(new
                {
                    context.Saga.CorrelationId,
                    context.Saga.AppointmentId,
                    Reason = context.Saga.FailureReason!
                }))
                .TransitionTo(Failed),

            // The appointment can be cancelled while we are still looking for a dentist. There
            // is nothing reserved yet to release, so this just stops the workflow.
            When(AppointmentWasCancelled)
                .TransitionTo(Cancelled));

        During(AwaitingConfirmation,
            When(AppointmentWasConfirmed)
                .SendAsync(context => context.Init<SetDentistAppointmentStatus>(new
                {
                    context.Saga.CorrelationId,
                    DentistId = context.Saga.DentistId!.Value,
                    Status = nameof(Statuses.Confirmed)
                }))
                .TransitionTo(Confirmed),

            When(AppointmentWasCancelled)
                .SendAsync(context => context.Init<SetDentistAppointmentStatus>(new
                {
                    context.Saga.CorrelationId,
                    DentistId = context.Saga.DentistId!.Value,
                    Status = nameof(Statuses.Cancelled)
                }))
                .TransitionTo(Cancelled),

            When(Updated)
                .Then(context => context.Saga.ScheduledDate = context.Message.ScheduledDate)
                .SendAsync(context => context.Init<RescheduleDentistAppointment>(new
                {
                    context.Saga.CorrelationId,
                    DentistId = context.Saga.DentistId!.Value,
                    context.Saga.ScheduledDate
                })));

        // A confirmed appointment can still be rescheduled or cancelled afterwards.
        During(Confirmed,
            When(AppointmentWasCancelled)
                .SendAsync(context => context.Init<SetDentistAppointmentStatus>(new
                {
                    context.Saga.CorrelationId,
                    DentistId = context.Saga.DentistId!.Value,
                    Status = nameof(Statuses.Cancelled)
                }))
                .TransitionTo(Cancelled),

            When(Updated)
                .Then(context => context.Saga.ScheduledDate = context.Message.ScheduledDate)
                .SendAsync(context => context.Init<RescheduleDentistAppointment>(new
                {
                    context.Saga.CorrelationId,
                    DentistId = context.Saga.DentistId!.Value,
                    context.Saga.ScheduledDate
                })),

            Ignore(Created),
            Ignore(AppointmentWasConfirmed),
            Ignore(Reserved),
            Ignore(ReservationFailed));

        // Terminal. Late redeliveries are accepted and dropped rather than faulted, so a
        // duplicate arriving after the workflow finished is harmless.
        During(Cancelled,
            Ignore(Created),
            Ignore(Updated),
            Ignore(AppointmentWasConfirmed),
            Ignore(AppointmentWasCancelled),
            Ignore(Reserved),
            Ignore(ReservationFailed));

        During(Failed,
            Ignore(Created),
            Ignore(Updated),
            Ignore(AppointmentWasConfirmed),
            Ignore(AppointmentWasCancelled),
            Ignore(Reserved),
            Ignore(ReservationFailed));
    }
}
