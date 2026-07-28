namespace Dentists.Api.Controllers;

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
}
