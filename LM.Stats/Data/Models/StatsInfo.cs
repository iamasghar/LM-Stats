namespace LM.Stats.Data.Models;

public class StatsInfo
{
    public int Id { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string UniqueIdentifier { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<Hunt> Hunts { get; set; } = new List<Hunt>();
    public ICollection<Kill> Kills { get; set; } = new List<Kill>();
    public ICollection<OtherStat> OtherStats { get; set; } = new List<OtherStat>();
}