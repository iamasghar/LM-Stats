// Controllers/ReportController.cs
using LM.Stats.Data;
using LM.Stats.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LM.Stats.Controllers;

public class ReportController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportController> _logger;

    public ReportController(AppDbContext context, ILogger<ReportController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult PlayerExplorer()
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
    public async Task<IActionResult> GetUploadedReports()
    {
        var reports = await _context.Stats
            .AsNoTracking()
            .OrderByDescending(s => s.FromDate)
            .Select(s => new
            {
                id = s.Id,
                uniqueIdentifier = s.UniqueIdentifier,
                fromDate = s.FromDate.ToString("yyyy-MM-dd"),
                toDate = s.ToDate.ToString("yyyy-MM-dd"),
                displayName = $"{s.FromDate:dd MMM yyyy} - {s.ToDate:dd MMM yyyy}"
            })
            .ToListAsync();

        return Json(reports);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUploadedReport(int statsId)
    {
        var stats = await _context.Stats
            .FirstOrDefaultAsync(s => s.Id == statsId);

        if (stats == null)
        {
            return Json(new { success = false, message = "Selected report was not found." });
        }

        await using var trx = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Stats.Remove(stats);
            await _context.SaveChangesAsync();
            await trx.CommitAsync();

            _logger.LogInformation("Deleted uploaded report {StatsId} ({FromDate} - {ToDate}).", stats.Id, stats.FromDate, stats.ToDate);

            return Json(new
            {
                success = true,
                message = $"Deleted report for {stats.FromDate:dd MMM yyyy} - {stats.ToDate:dd MMM yyyy}."
            });
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            _logger.LogError(ex, "Failed deleting uploaded report {StatsId}.", statsId);
            return Json(new { success = false, message = "Failed to delete report." });
        }
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

    [HttpGet]
    public async Task<IActionResult> SearchPlayers(string term = "", int take = 20)
    {
        term = (term ?? string.Empty).Trim();
        take = take <= 0 ? 20 : Math.Min(take, 50);

        var query = _context.StatsSummaries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(s => s.Name.Contains(term));
        }

        var names = await query
            .Select(s => s.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .OrderBy(n => n)
            .Take(take)
            .ToListAsync();

        return Json(names);
    }

    [HttpGet]
    public async Task<IActionResult> GetPlayerDetails(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Player name is required");
        }

        var normalizedName = name.Trim().ToLower();

        var targetUserId = await _context.StatsSummaries
            .AsNoTracking()
            .Include(s => s.Stats)
            .Where(s => !string.IsNullOrWhiteSpace(s.Name) && s.Name.Trim().ToLower() == normalizedName)
            .OrderByDescending(s => s.Stats.FromDate)
            .Select(s => (long?)s.UserId)
            .FirstOrDefaultAsync();

        if (!targetUserId.HasValue)
        {
            return NotFound();
        }

        var rows = await _context.StatsSummaries
            .AsNoTracking()
            .Include(s => s.Stats)
            .Where(s => s.UserId == targetUserId.Value)
            .OrderBy(s => s.Stats.FromDate)
            .Select(s => new
            {
                week = s.Stats.UniqueIdentifier,
                fromDate = s.Stats.FromDate.ToString("yyyy-MM-dd"),
                toDate = s.Stats.ToDate.ToString("yyyy-MM-dd"),
                name = s.Name,
                rank = s.Rank,
                zone = s.Zone,
                might = s.Might,
                mightDiff = s.MightDifference,
                kills = s.Kills,
                killsDiff = s.KillsDifference,
                killsPercentage = Math.Round(s.KillsPercentage),
                edm = s.EDM,
                edmDiff = s.EDMDifference,
                troopsLost = s.TroopsLost,
                troopsLostDiff = s.TroopsLostDifference,
                huntPoints = s.HuntPoints,
                huntPercentage = Math.Round(s.HuntPercentage),
                purchasePoints = s.PurchasePoints,
                purchasePercentage = Math.Round(s.PurchasePercentage),
                firstHuntTime = s.FirstHuntTime,
                lastHuntTime = s.LastHuntTime
            })
            .ToListAsync();

        if (!rows.Any())
        {
            return NotFound();
        }

        var latest = rows.Last();

        return Json(new
        {
            player = new
            {
                latest.name,
                latest.rank,
                latest.zone,
                latest.might,
                latest.mightDiff,
                latest.kills,
                latest.killsDiff,
                latest.killsPercentage,
                latest.edm,
                latest.edmDiff,
                latest.troopsLost,
                latest.troopsLostDiff,
                latest.huntPoints,
                latest.huntPercentage,
                latest.purchasePoints,
                latest.purchasePercentage,
                latest.firstHuntTime,
                latest.lastHuntTime
            },
            history = rows
        });
    }
}

