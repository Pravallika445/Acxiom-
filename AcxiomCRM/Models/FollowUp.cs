using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class FollowUp
{
    public int FollowUpId { get; set; }

    [Required]
    [StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // "Pending", "Completed"

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }

    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? CompletedDate { get; set; }
}
