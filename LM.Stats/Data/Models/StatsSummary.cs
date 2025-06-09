namespace LM.Stats.Data.Models;
public class StatsSummary
{
    public int Id { get; set; }
    public int StatsId { get; set; }
    public StatsInfo Stats { get; set; }

    public long UserId { get; set; }
    public string Name { get; set; }
    public string Rank { get; set; }

    // Might Data
    public long Might { get; set; }
    public long MightDifference { get; set; }

    // Kills Data
    public long Kills { get; set; }
    public long KillsDifference { get; set; }
    public decimal KillsPercentage { get; set; }

    // EDM (Enemies Destroyed Might)
    public long EDM { get; set; }
    public long EDMDifference { get; set; }

    // Troops
    public long TroopsLost { get; set; }
    public long TroopsLostDifference { get; set; }

    // Hunt Data
    public int HuntPoints { get; set; }
    public decimal HuntPercentage { get; set; }
    public int PurchasePoints { get; set; }
    public decimal PurchasePercentage { get; set; }
    public DateTime? FirstHuntTime { get; set; }
    public DateTime? LastHuntTime { get; set; }

    // Zone Classification
    public string Zone { get; set; } // Green, Yellow, Red, New, Left
}