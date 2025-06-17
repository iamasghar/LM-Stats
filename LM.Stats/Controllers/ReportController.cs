// Controllers/ReportController.cs
using LM.Stats.Data;
using LM.Stats.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LM.Stats.Controllers;

public class ReportController : Controller
{
    private readonly AppDbContext _context;

    public ReportController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableWeeks()
    {
        var weeks = await _context.Stats
            .OrderByDescending(s => s.FromDate)
            .Select(s => new {
                UniqueIdentifier = s.UniqueIdentifier,
                FromDate = s.FromDate.ToString("yyyy-MM-dd"),
                ToDate = s.ToDate.ToString("yyyy-MM-dd")
            })
            .ToListAsync();

        return Json(weeks);
    }

    [HttpGet]
    public async Task<IActionResult> GenerateReport(string week)
    {
        var stats = await _context.Stats
            .FirstOrDefaultAsync(s => s.UniqueIdentifier == week);

        var usersToSkip = _context.Configs.FirstOrDefault(c => c.Key == "SkipInReport")?.Value?.Split(',');

        if (stats == null)
        {
            return NotFound();
        }

        // Get previous 5 weeks for history
        var weeksToShow = await _context.Stats
            .Where(s => s.FromDate <= stats.FromDate)
            .OrderByDescending(s => s.FromDate)
            .Take(6)
            .Select(s => new {
                UniqueIdentifier = s.UniqueIdentifier,
                FromDate = s.FromDate.ToString("yyyy-MM-dd"),
                ToDate = s.ToDate.ToString("yyyy-MM-dd")
            })
            .ToListAsync();

        // Get all summaries for these weeks
        var allSummaries = await _context.StatsSummaries
            .Include(s => s.Stats)
            .Where(s => weeksToShow.Select(w => w.UniqueIdentifier).Contains(s.Stats.UniqueIdentifier))
            .ToListAsync();

        // Process current week data
        var currentSummaries = allSummaries
            .Where(s => s.StatsId == stats.Id)
            .Where(s => usersToSkip is null || !usersToSkip.Contains(s.Name))
            .Select(summary =>
            {
                var history = allSummaries
                    .Where(s => s.UserId == summary.UserId)
                    .OrderBy(s => s.Stats.FromDate)
                    .Select(s => new
                    {
                        week = s.Stats.UniqueIdentifier,
                        fromDate = s.Stats.FromDate.ToString("yyyy-MM-dd"),
                        toDate = s.Stats.ToDate.ToString("yyyy-MM-dd"),
                        zone = s.Zone
                    })
                    .ToList();

                return new
                {
                    name = summary.Name,
                    rank = summary.Rank,
                    might = summary.Might,
                    mightDiff = summary.MightDifference,
                    kills = summary.Kills,
                    killsDiff = summary.KillsDifference,
                    killsPercentage = Math.Round(summary.KillsPercentage),
                    edm = summary.EDM,
                    edmDiff = summary.EDMDifference,
                    troopsLost = summary.TroopsLost,
                    troopsLostDiff = summary.TroopsLostDifference,
                    huntPoints = summary.HuntPoints,
                    huntPercentage = Math.Round(summary.HuntPercentage),
                    purchasePoints = summary.PurchasePoints,
                    purchasePercentage = Math.Round(summary.PurchasePercentage),
                    firstHuntTime = summary.FirstHuntTime?.ToString("yyyy/MM/dd HH:mm:ss"),
                    lastHuntTime = summary.LastHuntTime?.ToString("yyyy/MM/dd HH:mm:ss"),
                    zone = summary.Zone,
                    completedBoth = summary.Zone == "Green",
                    completedOne = summary.Zone == "Yellow",
                    failedBoth = summary.Zone == "Red",
                    isNewPlayer = summary.Zone == "New",
                    hasLeft = summary.Zone == "Left",
                    history
                };
            }).ToList();



        // Get top 3 performers in each category
        var validPlayers = currentSummaries.Where(p => !p.isNewPlayer && !p.hasLeft).ToList();

        var topPerformers = validPlayers.Any() ? new
        {
            topKills = validPlayers.OrderByDescending(p => p.killsDiff).Take(3).ToList(),
            topHunt = validPlayers.OrderByDescending(p => p.huntPoints).Take(3).ToList(),
            topEDM = validPlayers.OrderByDescending(p => p.edmDiff).Take(3).ToList(),
            topPurchase = validPlayers.OrderByDescending(p => p.purchasePoints).Take(3).ToList()
        } : null;

        // Categorize players
        var completedBoth = currentSummaries
            .Where(row => row.completedBoth && !row.isNewPlayer && !row.hasLeft)
            .OrderByDescending(r => r.killsDiff)
            .ToList();
        var completedOne = currentSummaries
            .Where(row => row.completedOne && !row.completedBoth && !row.isNewPlayer && !row.hasLeft)
            .OrderByDescending(r => r.killsDiff)
            .ToList();
        var failedBoth = currentSummaries
            .Where(row => row.failedBoth && !row.isNewPlayer && !row.hasLeft)
            .OrderByDescending(r => r.killsDiff)
            .ToList();
        var recentlyJoined = currentSummaries
            .Where(row => row.isNewPlayer)
            .OrderByDescending(r => r.killsDiff)
            .ToList();
        var leftPlayers = currentSummaries
            .Where(row => row.hasLeft)
            .OrderByDescending(r => r.killsDiff)
            .ToList();

        return Json(new
        {
            week,
            weeksToShow,
            completedBoth,
            completedOne,
            failedBoth,
            recentlyJoined,
            leftPlayers,
            topPerformers
        });
    }

    [HttpGet]
    public async Task<IActionResult> GuildTrends()
    {
        var trends = await _context.StatsSummaries
            .Include(s => s.Stats)
            .GroupBy(s => s.Stats.UniqueIdentifier)
            .Select(g => new {
                Week = g.Key,
                DateRange = $"{g.First().Stats.FromDate:dd-MMM} - {g.First().Stats.ToDate:dd-MMM}",
                TotalKills = g.Sum(s => s.KillsDifference),
                TotalHuntPoints = g.Sum(s => s.HuntPoints),
                TotalEDM = g.Sum(s => s.EDMDifference),
                GreenZoneCount = g.Count(s => s.Zone == "Green"),
                YellowZoneCount = g.Count(s => s.Zone == "Yellow"),
                RedZoneCount = g.Count(s => s.Zone == "Red")
            })
            .OrderBy(t => t.Week)
            .ToListAsync();

        return Json(trends);
    }
}