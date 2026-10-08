using System.Security.Claims;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public class CrmAuthorizationService : ICrmAuthorizationService
{
    public bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
    public bool IsManager(ClaimsPrincipal user) => user.IsInRole(Roles.Manager);
    public bool IsSales(ClaimsPrincipal user) => user.IsInRole(Roles.Sales);

    public string? GetUserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public IQueryable<Opportunity> ScopeOpportunities(IQueryable<Opportunity> query, ClaimsPrincipal user)
    {
        // Admin and Manager can see all team/CRM level opportunities
        if (IsAdmin(user) || IsManager(user))
        {
            return query;
        }

        // Sales users can see only records assigned to themselves
        var userId = GetUserId(user);
        return query.Where(o => o.AssignedToUserId == userId);
    }

    public IQueryable<FollowUp> ScopeFollowUps(IQueryable<FollowUp> query, ClaimsPrincipal user)
    {
        // Admin and Manager can see all team/CRM level follow-ups
        if (IsAdmin(user) || IsManager(user))
        {
            return query;
        }

        // Sales users can see only records assigned to themselves
        var userId = GetUserId(user);
        return query.Where(f => f.AssignedToUserId == userId);
    }

    public bool CanAccessOpportunity(Opportunity opportunity, ClaimsPrincipal user)
    {
        if (IsAdmin(user) || IsManager(user))
        {
            return true;
        }

        var userId = GetUserId(user);
        return !string.IsNullOrEmpty(userId) && opportunity.AssignedToUserId == userId;
    }

    public bool CanAccessFollowUp(FollowUp followUp, ClaimsPrincipal user)
    {
        if (IsAdmin(user) || IsManager(user))
        {
            return true;
        }

        var userId = GetUserId(user);
        return !string.IsNullOrEmpty(userId) && followUp.AssignedToUserId == userId;
    }
}
