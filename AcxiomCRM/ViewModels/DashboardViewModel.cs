namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    // 8 Required KPI Metrics
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public decimal OpenPipelineValue { get; set; }
    public int WonOpportunities { get; set; }
    public int PendingFollowUps { get; set; }

    // Chart 1: Opportunity pipeline/stage distribution
    public List<string> PipelineStageLabels { get; set; } = new();
    public List<int> PipelineStageCounts { get; set; } = new();

    // Chart 2: Opportunity value by stage
    public List<string> StageValueLabels { get; set; } = new();
    public List<decimal> StageValues { get; set; } = new();

    // Chart 3: Follow-Up / Activity Trend
    public List<string> ActivityLabels { get; set; } = new();
    public List<int> ActivityCounts { get; set; } = new();
}
