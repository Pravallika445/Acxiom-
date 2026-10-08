using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class FollowUpViewModel
{
    public int FollowUpId { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "Description / Notes")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Follow-up date/time is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Follow-Up Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.Now.AddDays(1);

    [Required]
    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Pending";

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Opportunity")]
    public int? OpportunityId { get; set; }

    [Display(Name = "Assigned User")]
    public string? AssignedToUserId { get; set; }

    // Display helpers
    public string? CustomerName { get; set; }
    public string? OpportunityName { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}
