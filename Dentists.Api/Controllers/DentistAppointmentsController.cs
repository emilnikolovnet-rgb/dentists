namespace Dentists.Api.Controllers;

using Dentists.Api.Contracts;
using Dentists.Application.Commands;
using Dentists.Application.DTOs;
using Dentists.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Appointments are embedded in the dentist they belong to rather than existing on their own,
/// so they are addressed underneath one.
/// </summary>
[ApiController]
[Route("api/dentists/{dentistId:guid}/appointments")]
[Produces("application/json")]
public class DentistAppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DentistAppointmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Record a booking against a dentist
    /// </summary>
    /// <remarks>
    /// Idempotent on the appointment's correlation id: re-sending a booking already recorded
    /// returns it unchanged rather than duplicating it.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(DentistAppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DentistAppointmentDto>> Add(
        Guid dentistId,
        [FromBody] AddAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddAppointmentCommand
        {
            DentistId = dentistId,
            AppointmentCorrelationId = request.AppointmentCorrelationId,
            ScheduledDate = request.ScheduledDate
        };

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { dentistId, appointmentCorrelationId = result.AppointmentCorrelationId },
            result);
    }

    /// <summary>
    /// Get one of a dentist's appointments
    /// </summary>
    [HttpGet("{appointmentCorrelationId:guid}")]
    [ProducesResponseType(typeof(DentistAppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DentistAppointmentDto>> GetById(
        Guid dentistId,
        Guid appointmentCorrelationId,
        CancellationToken cancellationToken)
    {
        var query = new GetAppointmentByIdQuery
        {
            DentistId = dentistId,
            AppointmentCorrelationId = appointmentCorrelationId
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Update a dentist's appointment
    /// </summary>
    /// <remarks>
    /// Cancelling is done here, by sending a status of Cancelled. That is terminal: a further
    /// update to a cancelled booking is refused with a 409.
    /// </remarks>
    [HttpPut("{appointmentCorrelationId:guid}")]
    [ProducesResponseType(typeof(DentistAppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DentistAppointmentDto>> Update(
        Guid dentistId,
        Guid appointmentCorrelationId,
        [FromBody] UpdateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAppointmentCommand
        {
            DentistId = dentistId,
            AppointmentCorrelationId = appointmentCorrelationId,
            ScheduledDate = request.ScheduledDate,
            Status = request.Status
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Set only the status of a dentist's appointment
    /// </summary>
    /// <remarks>
    /// Leaves the scheduled date as it is. Re-sending the status a booking already holds
    /// succeeds unchanged, so a redelivered event is harmless — including a repeated
    /// Cancelled. Moving a cancelled booking to any other status is refused with a 409.
    /// </remarks>
    [HttpPut("{appointmentCorrelationId:guid}/status")]
    [ProducesResponseType(typeof(DentistAppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DentistAppointmentDto>> SetStatus(
        Guid dentistId,
        Guid appointmentCorrelationId,
        [FromBody] SetAppointmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetAppointmentStatusCommand
        {
            DentistId = dentistId,
            AppointmentCorrelationId = appointmentCorrelationId,
            Status = request.Status
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Remove a dentist's appointment
    /// </summary>
    /// <remarks>
    /// Deletes the record outright. To mark a booking cancelled while keeping it, send a
    /// status of Cancelled to the update endpoint instead.
    /// </remarks>
    [HttpDelete("{appointmentCorrelationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid dentistId,
        Guid appointmentCorrelationId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteAppointmentCommand
        {
            DentistId = dentistId,
            AppointmentCorrelationId = appointmentCorrelationId
        };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}
