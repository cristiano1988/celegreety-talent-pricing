using MediatR;
using Microsoft.AspNetCore.Mvc;
using Features.TalentPricings.Commands;
using Features.TalentPricings.Queries;
using Microsoft.AspNetCore.Authorization;

namespace Controllers;

/// <summary>
/// Manages talent pricing configuration and retrieval.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class TalentPricingController : ControllerBase
{
    private readonly IMediator _mediator;

    public TalentPricingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new pricing configuration for a talent, creating the necessary Stripe products and prices.
    /// </summary>
    /// <param name="command">The pricing creation command.</param>
    /// <returns>The created pricing result.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePricing([FromBody] CreateTalentPricingCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the current pricing and history for a specific talent.
    /// </summary>
    /// <param name="talentId">The unique identifier of the talent.</param>
    /// <returns>Pricing details with history.</returns>
    [HttpGet("{talentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPricing(int talentId)
    {
        var result = await _mediator.Send(new GetTalentPricingQuery(talentId));

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Updates an existing pricing configuration, archiving old Stripe prices and creating new ones.
    /// </summary>
    /// <param name="command">The pricing update command.</param>
    /// <returns>No content.</returns>
    [HttpPut]
    [Authorize(Policy = "CanEditTalent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePricing([FromBody] UpdateTalentPricingCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}