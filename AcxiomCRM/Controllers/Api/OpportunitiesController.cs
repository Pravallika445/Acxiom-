using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Dtos;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
public class OpportunitiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICrmAuthorizationService _authService;

    public OpportunitiesController(ApplicationDbContext context, ICrmAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    // GET: /api/opportunities
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OpportunityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        // Enforce role-based data scope: Sales users only receive their own opportunities
        var scopedQuery = _authService.ScopeOpportunities(_context.Opportunities, User);

        var opportunities = await scopedQuery
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .OrderByDescending(o => o.CreatedDate)
            .Select(o => MapToDto(o))
            .ToListAsync();

        return Ok(opportunities);
    }

    // GET: /api/opportunities/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OpportunityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var opportunity = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
        {
            return NotFound(new { message = $"Opportunity with ID {id} was not found." });
        }

        // Enforce role-based authorization: Sales user cannot access another user's opportunity
        if (!_authService.CanAccessOpportunity(opportunity, User))
        {
            return Forbid();
        }

        return Ok(MapToDto(opportunity));
    }

    private static OpportunityDto MapToDto(Opportunity o)
    {
        return new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName,
            Amount = o.Amount,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Stage = o.Stage,
            Status = o.Status,
            AssignedToUserId = o.AssignedToUserId,
            AssignedToUserName = o.AssignedToUser?.FullName,
            CreatedDate = o.CreatedDate
        };
    }
}
