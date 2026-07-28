namespace Dentists.Api.Controllers;

using Dentists.Api.Contracts;
using Dentists.Application.Commands;
using Dentists.Application.DTOs;
using Dentists.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DentistsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DentistsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all dentists
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DentistDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DentistDto>>> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetAllDentistsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all dentists with no booking in the given period
    /// </summary>
    /// <remarks>
    /// Declared before the "{id}" route so that /api/dentists/available is not captured by it.
    /// </remarks>
    [HttpGet("available")]
    [ProducesResponseType(typeof(IEnumerable<DentistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<DentistDto>>> GetAvailable(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var query = new GetAvailableDentistsQuery { From = from, To = to };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get dentist by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DentistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DentistDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetDentistByIdQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Add a dentist
    /// </summary>
    /// <remarks>
    /// The dentist's identifier is assigned here, not supplied. It is returned on the response
    /// and in the Location header.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(DentistDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DentistDto>> Create(
        [FromBody] CreateDentistRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDentistCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update a dentist
    /// </summary>
    /// <remarks>
    /// Changes the dentist's name only. Their appointments are managed through
    /// /api/dentists/{dentistId}/appointments and are left as they are.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DentistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DentistDto>> Update(
        Guid id,
        [FromBody] UpdateDentistRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDentistCommand
        {
            Id = id,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Delete a dentist
    /// </summary>
    /// <remarks>
    /// A soft delete. The dentist stops appearing in every read — including the availability
    /// search — but the record and its appointments are kept, so bookings the Appointments
    /// service still holds are not destroyed. Deleting an already-deleted dentist returns 404.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteDentistCommand { Id = id };

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}
