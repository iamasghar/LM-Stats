namespace LM.Stats.Data.Models;

public class OtherStat
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; }
    public long Might { get; set; }
    public long Kills { get; set; }
    public long TroopsKilled { get; set; }
    public long EnemiesDestroyedMight { get; set; }
    public long TroopsLost { get; set; }
    public string WinRate { get; set; }
    
    public int? StatsId { get; set; }
    public StatsInfo Stats { get; set; }
}