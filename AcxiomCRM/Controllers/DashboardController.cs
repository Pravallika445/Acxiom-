using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICrmAuthorizationService _authService;

    public DashboardController(ApplicationDbContext context, ICrmAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();

        // 1. Total Customers
        model.TotalCustomers = await _context.Customers.CountAsync();

        // 2. Active Customers
        model.ActiveCustomers = await _context.Customers.CountAsync(c => c.Status == "Active");

        // 3 & 4. Leads: entity not yet implemented in Phase 1/2/3, set to closest supported metric (0)
        model.TotalLeads = 0;
        model.OpenLeads = 0;

        // Scoped Opportunities and Follow-Ups
        var oppQuery = _authService.ScopeOpportunities(_context.Opportunities, User);
        var fuQuery = _authService.ScopeFollowUps(_context.FollowUps, User);

        // 5. Total Opportunities
        model.TotalOpportunities = await oppQuery.CountAsync();

        // 6. Open Pipeline Value
        model.OpenPipelineValue = await oppQuery
            .Where(o => o.Status == "Open")
            .SumAsync(o => (decimal?)o.Amount) ?? 0m;

        // 7. Won Opportunities
        model.WonOpportunities = await oppQuery
            .CountAsync(o => o.Status == "Won");

        // 8. Pending Follow-Ups
        model.PendingFollowUps = await fuQuery
            .CountAsync(f => f.Status == "Pending");

        // Predefined stages for consistent and graceful zero-state rendering
        var standardStages = new[] { "Prospecting", "Qualification", "Proposal", "Negotiation", "Closed Won", "Closed Lost" };

        // Chart 1: Opportunity Pipeline / Stage Distribution
        var stageCounts = await oppQuery
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToListAsync();
        var stageCountDict = stageCounts.ToDictionary(s => s.Stage, s => s.Count);

        foreach (var stage in standardStages)
        {
            model.PipelineStageLabels.Add(stage);
            model.PipelineStageCounts.Add(stageCountDict.TryGetValue(stage, out var count) ? count : 0);
        }

        // Chart 2: Opportunity Value by Stage
        var stageValues = await oppQuery
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, TotalValue = g.Sum(o => o.Amount) })
            .ToListAsync();
        var stageValueDict = stageValues.ToDictionary(s => s.Stage, s => s.TotalValue);

        foreach (var stage in standardStages)
        {
            model.StageValueLabels.Add(stage);
            model.StageValues.Add(stageValueDict.TryGetValue(stage, out var val) ? val : 0m);
        }

        // Chart 3: Follow-Up / Activity Distribution
        var followUpStatuses = new[] { "Pending", "Completed" };
        var followUpCounts = await fuQuery
            .GroupBy(f => f.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var fuDict = followUpCounts.ToDictionary(f => f.Status, f => f.Count);

        foreach (var status in followUpStatuses)
        {
            model.ActivityLabels.Add(status);
            model.ActivityCounts.Add(fuDict.TryGetValue(status, out var c) ? c : 0);
        }

        return View(model);
    }
}
