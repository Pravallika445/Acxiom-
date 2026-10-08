using System.Security.Claims;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface ICrmAuthorizationService
{
    bool IsAdmin(ClaimsPrincipal user);
    bool IsManager(ClaimsPrincipal user);
    bool IsSales(ClaimsPrincipal user);
    string? GetUserId(ClaimsPrincipal user);

    IQueryable<Opportunity> ScopeOpportunities(IQueryable<Opportunity> query, ClaimsPrincipal user);
    IQueryable<FollowUp> ScopeFollowUps(IQueryable<FollowUp> query, ClaimsPrincipal user);

    bool CanAccessOpportunity(Opportunity opportunity, ClaimsPrincipal user);
    bool CanAccessFollowUp(FollowUp followUp, ClaimsPrincipal user);
}
