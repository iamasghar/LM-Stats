using LM.Stats.Data;
using LM.Stats.Data.Extensions;
using LM.Stats.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LM.Stats.Services;
public class StatsProcessorService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public StatsProcessorService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task ProcessStatsAsync(int statsId)
    {
        // Get goals from config
        var huntGoal = _context.Configs.FirstOrDefault(c => c.Key == "HuntGoal")?.Value.ToSafeDecimal() ?? 1m;
        var purchaseGoal = _context.Configs.FirstOrDefault(c => c.Key == "PurchaseGoal")?.Value.ToSafeDecimal() ?? 1m;
        var killsGoal = _context.Configs.FirstOrDefault(c => c.Key == "KillsGoal")?.Value.ToSafeDecimal() ?? 1m;

        // Get current data
        var currentStats = await _context.Stats
            .Include(s => s.Hunts)
            .Include(s => s.Kills)
            .Include(s => s.OtherStats)
            .FirstOrDefaultAsync(s => s.Id == statsId);

        if (currentStats == null) return;

        // Get previous data for diffs
        var previousStats = await _context.Stats
            .Include(s => s.Hunts)
            .Include(s => s.Kills)
            .Include(s => s.OtherStats)
            .Where(s => s.FromDate < currentStats.FromDate)
            .OrderByDescending(s => s.FromDate)
            .FirstOrDefaultAsync();

        // Process each user
        var summaries = new List<StatsSummary>();

        // Get all unique users across all three tables
        var allUserIds = currentStats.Hunts.Select(h => h.UserId)
            .Union(currentStats.Kills.Select(k => k.IggId))
            .Union(currentStats.OtherStats.Select(o => o.UserId))
            .Distinct();

        foreach (var userId in allUserIds)
        {
            var summary = new StatsSummary
            {
                StatsId = statsId,
                UserId = userId,
                Zone = "New" // Default, will be updated
            };

            // Process Hunts data
            var hunt = currentStats.Hunts.FirstOrDefault(h => h.UserId == userId);
            if (hunt != null)
            {
                summary.Name = hunt.Name;
                summary.HuntPoints = hunt.PointsHunt;
                summary.HuntPercentage = hunt.GoalPercentageHunt.ToSafeDecimal() ?? 0m;
                summary.PurchasePoints = hunt.PointsPurchase;
                summary.PurchasePercentage = hunt.GoalPercentagePurchase.ToSafeDecimal() ?? 0m;
                summary.FirstHuntTime = hunt.FirstHuntTime;
                summary.LastHuntTime = hunt.LastHuntTime;
            }

            // Process Kills data
            var kill = currentStats.Kills.FirstOrDefault(k => k.IggId == userId);
            if (kill != null)
            {
                summary.Name ??= kill.Name;
                summary.Rank = kill.Rank;
            }

            // Process OtherStats data
            var otherStat = currentStats.OtherStats.FirstOrDefault(o => o.UserId == userId);
            if (otherStat != null)
            {
                summary.Name ??= otherStat.Name;
                summary.Might = otherStat.Might;
                summary.Kills = otherStat.Kills;
                summary.EDM = otherStat.EnemiesDestroyedMight;
                summary.TroopsLost = otherStat.TroopsLost;

                // Calculate differences if previous data exists
                if (previousStats != null)
                {
                    var prevOtherStat = previousStats.OtherStats.FirstOrDefault(o => o.UserId == userId);
                    if (prevOtherStat != null)
                    {
                        summary.MightDifference = otherStat.Might - prevOtherStat.Might;
                        summary.KillsDifference = otherStat.Kills - prevOtherStat.Kills;
                        summary.EDMDifference = otherStat.EnemiesDestroyedMight - prevOtherStat.EnemiesDestroyedMight;
                        summary.TroopsLostDifference = otherStat.TroopsLost - prevOtherStat.TroopsLost;
                        summary.Zone = "Left"; // Will be updated if current data exists
                    }
                }
            }
            else if(kill != null)
            {
                summary.Might = kill.Might;
                summary.MightDifference = kill.MightDifference;
                summary.Kills = kill.Kills;
                summary.KillsDifference = kill.KillsDifference;
                summary.EDM = 0;
                summary.EDMDifference = 0;
            }

            summary.KillsPercentage = (summary.KillsDifference / killsGoal) * 100;

            // Determine Zone
            if (kill != null && kill.OldMight == 0)
                summary.Zone = "New";
            else if (kill is null && otherStat is null)
                summary.Zone = "Left";
            else
                summary.Zone = CalculateZone(
                    summary.HuntPercentage >= 100,//95,
                    summary.PurchasePercentage >= 100,
                    summary.KillsPercentage >= 100,
                    (otherStat != null || kill != null),
                    previousStats?.Kills.Any(o => o.IggId == userId) ?? false);


            summary.Rank ??= "RANK0";
            
            summaries.Add(summary);
        }

        // Save to database
        _context.StatsSummaries.AddRange(summaries);
        await _context.SaveChangesAsync();
    }

    private string CalculateZone(bool huntGoalMet, bool purchaseGoalMet, bool killsGoalMet, bool hasCurrentData, bool hadPreviousData)
    {
        if (!hasCurrentData) return hadPreviousData ? "Left" : "New";

        var goalsMet = (huntGoalMet || purchaseGoalMet) ? 1 : 0;
        goalsMet += killsGoalMet ? 1 : 0;

        return goalsMet switch
        {
            2 => "Green",  // Both goals met
            1 => "Yellow", // One goal met
            _ => "Red"     // No goals met
        };
    }
}