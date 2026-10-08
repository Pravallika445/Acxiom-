using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICrmAuthorizationService _authService;

    public OpportunitiesController(ApplicationDbContext context, ICrmAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? stage, string? status)
    {
        // Apply role-based data scope: Sales sees only own records; Admin & Manager see all
        var query = _authService.ScopeOpportunities(_context.Opportunities, User)
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o => o.OpportunityName.Contains(search) 
                                  || (o.Customer != null && o.Customer.CustomerName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        ViewBag.Search = search;
        ViewBag.Stage = stage;
        ViewBag.Status = status;

        var list = await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var opportunity = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .Include(o => o.FollowUps.OrderByDescending(f => f.FollowUpDate))
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
        {
            return NotFound();
        }

        // Server-side authorization check: deny access if Sales user accesses another's record
        if (!_authService.CanAccessOpportunity(opportunity, User))
        {
            return Forbid();
        }

        return View(opportunity);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null)
    {
        var model = new OpportunityViewModel
        {
            ExpectedCloseDate = DateTime.Today.AddDays(30)
        };

        if (customerId.HasValue)
        {
            model.CustomerId = customerId.Value;
        }

        if (_authService.IsSales(User))
        {
            model.AssignedToUserId = _authService.GetUserId(User);
        }

        await PopulateDropDownsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityViewModel model)
    {
        // Enforce server-side ownership assignment for Sales: never trust browser input
        if (_authService.IsSales(User))
        {
            model.AssignedToUserId = _authService.GetUserId(User);
        }

        // Mandatory server-side validations
        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (model.ExpectedCloseDate.Date < DateTime.Today)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected close date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync();
            return View(model);
        }

        var customer = await _context.Customers.FindAsync(model.CustomerId);
        if (customer == null)
        {
            ModelState.AddModelError("CustomerId", "Selected customer does not exist.");
            await PopulateDropDownsAsync();
            return View(model);
        }

        var assignedUserId = _authService.IsSales(User)
            ? _authService.GetUserId(User)
            : (string.IsNullOrWhiteSpace(model.AssignedToUserId) ? null : model.AssignedToUserId);

        var opportunity = new Opportunity
        {
            OpportunityName = model.OpportunityName.Trim(),
            CustomerId = model.CustomerId,
            Amount = model.Amount,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate.Date,
            Stage = model.Stage,
            Status = model.Status,
            AssignedToUserId = assignedUserId,
            CreatedDate = DateTime.Now,
            CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Opportunity created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        // Server-side authorization check: Sales can only edit own assigned records
        if (!_authService.CanAccessOpportunity(opportunity, User))
        {
            return Forbid();
        }

        var model = new OpportunityViewModel
        {
            OpportunityId = opportunity.OpportunityId,
            OpportunityName = opportunity.OpportunityName,
            CustomerId = opportunity.CustomerId,
            Amount = opportunity.Amount,
            Probability = opportunity.Probability,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            Stage = opportunity.Stage,
            Status = opportunity.Status,
            AssignedToUserId = opportunity.AssignedToUserId
        };

        await PopulateDropDownsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityViewModel model)
    {
        if (id != model.OpportunityId)
        {
            return BadRequest();
        }

        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        // Server-side authorization check: verify user is allowed to edit this record
        if (!_authService.CanAccessOpportunity(opportunity, User))
        {
            return Forbid();
        }

        // Enforce ownership: Sales cannot reassign record to someone else
        if (_authService.IsSales(User))
        {
            model.AssignedToUserId = _authService.GetUserId(User);
        }

        // Mandatory server-side validations
        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (model.ExpectedCloseDate.Date < DateTime.Today)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected close date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync();
            return View(model);
        }

        var assignedUserId = _authService.IsSales(User)
            ? _authService.GetUserId(User)
            : (string.IsNullOrWhiteSpace(model.AssignedToUserId) ? null : model.AssignedToUserId);

        opportunity.OpportunityName = model.OpportunityName.Trim();
        opportunity.CustomerId = model.CustomerId;
        opportunity.Amount = model.Amount;
        opportunity.Probability = model.Probability;
        opportunity.ExpectedCloseDate = model.ExpectedCloseDate.Date;
        opportunity.Stage = model.Stage;
        opportunity.Status = model.Status;
        opportunity.AssignedToUserId = assignedUserId;

        _context.Update(opportunity);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Opportunity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        // Server-side authorization check: verify user is allowed to delete/deactivate this record
        if (!_authService.CanAccessOpportunity(opportunity, User))
        {
            return Forbid();
        }

        opportunity.Status = "Inactive";
        _context.Update(opportunity);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Opportunity deactivated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropDownsAsync()
    {
        var customers = await _context.Customers
            .OrderBy(c => c.CustomerName)
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = $"{c.CustomerName} ({c.CustomerCode})"
            })
            .ToListAsync();

        var usersQuery = _context.Users.AsQueryable();
        // If Sales, only list self to prevent confusion in the UI
        if (_authService.IsSales(User))
        {
            var userId = _authService.GetUserId(User);
            usersQuery = usersQuery.Where(u => u.Id == userId);
        }

        var users = await usersQuery
            .OrderBy(u => u.FullName)
            .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})"
            })
            .ToListAsync();

        ViewBag.Customers = customers;
        ViewBag.Users = users;
    }
}
