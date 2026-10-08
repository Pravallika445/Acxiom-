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
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICrmAuthorizationService _authService;

    public FollowUpsController(ApplicationDbContext context, ICrmAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? filter = "Pending")
    {
        // Apply role-based data scope: Sales sees only own records; Admin & Manager see all
        var query = _authService.ScopeFollowUps(_context.FollowUps, User)
            .Include(f => f.Customer)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedToUser)
            .AsQueryable();

        if (filter == "Pending")
        {
            query = query.Where(f => f.Status == "Pending");
        }
        else if (filter == "Completed")
        {
            query = query.Where(f => f.Status == "Completed");
        }

        ViewBag.Filter = filter;

        var list = await query
            .OrderBy(f => f.FollowUpDate)
            .ToListAsync();

        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var followUp = await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedToUser)
            .FirstOrDefaultAsync(f => f.FollowUpId == id);

        if (followUp == null)
        {
            return NotFound();
        }

        // Server-side authorization check: Sales can only view own assigned follow-up
        if (!_authService.CanAccessFollowUp(followUp, User))
        {
            return Forbid();
        }

        return View(followUp);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? opportunityId = null)
    {
        var model = new FollowUpViewModel
        {
            FollowUpDate = DateTime.Now.AddDays(1).Date.AddHours(10), // Tomorrow at 10 AM
            CustomerId = customerId,
            OpportunityId = opportunityId
        };

        if (_authService.IsSales(User))
        {
            model.AssignedToUserId = _authService.GetUserId(User);
        }

        await PopulateDropDownsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpViewModel model)
    {
        // Enforce server-side ownership assignment for Sales: never trust browser input
        if (_authService.IsSales(User))
        {
            model.AssignedToUserId = _authService.GetUserId(User);
        }

        // Mandatory server-side validation: Follow-up date/time cannot be in the past
        if (model.FollowUpDate < DateTime.Now)
        {
            ModelState.AddModelError("FollowUpDate", "Follow-up date/time cannot be in the past.");
        }

        // Verify opportunity access if linked
        if (model.OpportunityId.HasValue)
        {
            var opp = await _context.Opportunities.FindAsync(model.OpportunityId.Value);
            if (opp != null && !_authService.CanAccessOpportunity(opp, User))
            {
                ModelState.AddModelError("OpportunityId", "You do not have access to link this opportunity.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync();
            return View(model);
        }

        var assignedUserId = _authService.IsSales(User)
            ? _authService.GetUserId(User)
            : (string.IsNullOrWhiteSpace(model.AssignedToUserId) ? null : model.AssignedToUserId);

        var followUp = new FollowUp
        {
            Subject = model.Subject.Trim(),
            Description = model.Description?.Trim(),
            FollowUpDate = model.FollowUpDate,
            Status = "Pending",
            CustomerId = model.CustomerId,
            OpportunityId = model.OpportunityId,
            AssignedToUserId = assignedUserId,
            CreatedDate = DateTime.Now,
            CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null)
        {
            return NotFound();
        }

        // Server-side authorization check: verify user can complete this follow-up
        if (!_authService.CanAccessFollowUp(followUp, User))
        {
            return Forbid();
        }

        followUp.Status = "Completed";
        followUp.CompletedDate = DateTime.Now;

        _context.Update(followUp);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Follow-up marked as completed.";
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

        var oppQuery = _authService.ScopeOpportunities(_context.Opportunities, User);
        var opportunities = await oppQuery
            .OrderBy(o => o.OpportunityName)
            .Select(o => new SelectListItem
            {
                Value = o.OpportunityId.ToString(),
                Text = o.OpportunityName
            })
            .ToListAsync();

        var usersQuery = _context.Users.AsQueryable();
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
        ViewBag.Opportunities = opportunities;
        ViewBag.Users = users;
    }
}
