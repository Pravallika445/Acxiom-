using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class OpportunityViewModel
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    [Display(Name = "Amount ($)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; }

    [Required(ErrorMessage = "Expected close date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

    [Required(ErrorMessage = "Stage is required.")]
    [StringLength(50)]
    [Display(Name = "Stage")]
    public string Stage { get; set; } = "Prospecting";

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Open";

    [Display(Name = "Assigned User")]
    public string? AssignedToUserId { get; set; }

    // Display helpers
    public string? CustomerName { get; set; }
    public string? AssignedToUserName { get; set; }
}
