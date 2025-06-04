namespace LM.Stats.Data.Models;

public class Kill
{
    public int Id { get; set; }
    public long IggId { get; set; }
    public string Name { get; set; }
    public string Rank { get; set; }
    public long Might { get; set; }
    public long OldMight { get; set; }
    public long MightDifference { get; set; }
    public long Kills { get; set; }
    public long OldKills { get; set; }
    public long KillsDifference { get; set; }
    public string OldName { get; set; }
    
    public int? StatsId { get; set; }
    public StatsInfo Stats { get; set; }
}