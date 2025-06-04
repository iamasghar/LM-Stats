namespace LM.Stats.Data.Models;

public class Hunt
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; }
    public int Total { get; set; }
    public int HuntCount { get; set; }
    public int Purchase { get; set; }
    public int L1Hunt { get; set; }
    public int L2Hunt { get; set; }
    public int L3Hunt { get; set; }
    public int L4Hunt { get; set; }
    public int L5Hunt { get; set; }
    public int L1Purchase { get; set; }
    public int L2Purchase { get; set; }
    public int L3Purchase { get; set; }
    public int L4Purchase { get; set; }
    public int L5Purchase { get; set; }
    public int PointsHunt { get; set; }
    public string GoalPercentageHunt { get; set; }
    public int PointsPurchase { get; set; }
    public string GoalPercentagePurchase { get; set; }
    public DateTime FirstHuntTime { get; set; }
    public DateTime LastHuntTime { get; set; }
    
    public int? StatsId { get; set; }
    public StatsInfo Stats { get; set; }
}