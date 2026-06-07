// Controllers/ReportController.cs
using LM.Stats.Data;
using LM.Stats.Data.Extensions;
using LM.Stats.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LM.Stats.Controllers;

public class ReportController : Controller
{
    public sealed class UpdateConfigSettingsRequest
    {
        public decimal HuntGoal { get; set; }
        public decimal PurchaseGoal { get; set; }
        public decimal KillsGoal { get; set; }
        public List<string>? SkipInReport { get; set; }
    }

    public sealed class UpdateReportValueRequest
    {
        public string Week { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string Field { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

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
    public async Task<IActionResult> GetConfigSettings()
    {
        var keys = new[] { "HuntGoal", "PurchaseGoal", "KillsGoal", "SkipInReport" };
        var configs = await _context.Configs
            .AsNoTracking()
            .Where(c => keys.Contains(c.Key))
            .ToDictionaryAsync(c => c.Key, c => c.Value ?? string.Empty);

        var huntGoal = configs.TryGetValue("HuntGoal", out var huntValue) ? (huntValue.ToSafeDecimal() ?? 0m) : 0m;
        var purchaseGoal = configs.TryGetValue("PurchaseGoal", out var purchaseValue) ? (purchaseValue.ToSafeDecimal() ?? 0m) : 0m;
        var killsGoal = configs.TryGetValue("KillsGoal", out var killsValue) ? (killsValue.ToSafeDecimal() ?? 0m) : 0m;
        var skipInReport = configs.TryGetValue("SkipInReport", out var skipValue)
            ? skipValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()
            : new List<string>();

        return Json(new
        {
            huntGoal,
            purchaseGoal,
            killsGoal,
            skipInReport
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateConfigSettings([FromBody] UpdateConfigSettingsRequest request)
    {
        if (request is null)
        {
            return Json(new { success = false, message = "Settings request is invalid." });
        }

        if (request.HuntGoal < 0 || request.PurchaseGoal < 0 || request.KillsGoal < 0)
        {
            return Json(new { success = false, message = "Goal values cannot be negative." });
        }

        var cleanedSkip = (request.SkipInReport ?? new List<string>())
            .Select(x => (x ?? string.Empty).Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var skipCsv = string.Join(',', cleanedSkip);

        await UpsertConfigValueAsync("HuntGoal", request.HuntGoal.ToString("0"));
        await UpsertConfigValueAsync("PurchaseGoal", request.PurchaseGoal.ToString("0"));
        await UpsertConfigValueAsync("KillsGoal", request.KillsGoal.ToString("0"));
        await UpsertConfigValueAsync("SkipInReport", skipCsv);

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = "Configuration updated successfully.",
            data = new
            {
                huntGoal = request.HuntGoal,
                purchaseGoal = request.PurchaseGoal,
                killsGoal = request.KillsGoal,
                skipInReport = cleanedSkip
            }
        });
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

    private async Task UpsertConfigValueAsync(string key, string value)
    {
        var row = await _context.Configs.FirstOrDefaultAsync(c => c.Key == key);
        if (row == null)
        {
            _context.Configs.Add(new Config
            {
                Key = key,
                Value = value
            });
            return;
        }

        row.Value = value;
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
        var killsGoal = _context.Configs.FirstOrDefault(c => c.Key == "KillsGoal")?.Value.ToSafeDecimal() ?? 0m;
        var huntGoal = _context.Configs.FirstOrDefault(c => c.Key == "HuntGoal")?.Value.ToSafeDecimal() ?? 0m;

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
                    userId = summary.UserId,
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
            goals = new
            {
                kills = killsGoal,
                hunt = huntGoal
            },
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

    [HttpPost]
    public async Task<IActionResult> UpdateReportValue([FromBody] UpdateReportValueRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Week) || request.UserId <= 0 || string.IsNullOrWhiteSpace(request.Field))
        {
            return Json(new { success = false, message = "Update request is invalid." });
        }

        var normalizedField = request.Field.Trim();
        var stats = await _context.Stats
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UniqueIdentifier == request.Week);

        if (stats == null)
        {
            return Json(new { success = false, message = "Selected report was not found." });
        }

        var summary = await _context.StatsSummaries
            .FirstOrDefaultAsync(s => s.StatsId == stats.Id && s.UserId == request.UserId);

        if (summary == null)
        {
            return Json(new { success = false, message = "Selected player row was not found." });
        }

        switch (normalizedField)
        {
            case "killsDiff":
            {
                var parsed = request.Value.ToSafeLong();
                if (!parsed.HasValue)
                {
                    return Json(new { success = false, message = "Kills value is invalid." });
                }

                summary.KillsDifference = parsed.Value;
                break;
            }
            case "edmDiff":
            {
                var parsed = request.Value.ToSafeLong();
                if (!parsed.HasValue)
                {
                    return Json(new { success = false, message = "EDM value is invalid." });
                }

                summary.EDMDifference = parsed.Value;
                break;
            }
            case "troopsLostDiff":
            {
                var parsed = request.Value.ToSafeLong();
                if (!parsed.HasValue)
                {
                    return Json(new { success = false, message = "Troops Lost value is invalid." });
                }

                summary.TroopsLostDifference = parsed.Value;
                break;
            }
            case "huntPoints":
            {
                var parsed = request.Value.ToSafeInt();
                if (!parsed.HasValue)
                {
                    return Json(new { success = false, message = "Hunt value is invalid." });
                }

                summary.HuntPoints = parsed.Value;
                break;
            }
            case "purchasePoints":
            {
                var parsed = request.Value.ToSafeInt();
                if (!parsed.HasValue)
                {
                    return Json(new { success = false, message = "Purchase value is invalid." });
                }

                summary.PurchasePoints = parsed.Value;
                break;
            }
            default:
                return Json(new { success = false, message = "This field cannot be edited." });
        }

        var huntGoal = _context.Configs.FirstOrDefault(c => c.Key == "HuntGoal")?.Value.ToSafeDecimal() ?? 1m;
        var purchaseGoal = _context.Configs.FirstOrDefault(c => c.Key == "PurchaseGoal")?.Value.ToSafeDecimal() ?? 1m;
        var killsGoal = _context.Configs.FirstOrDefault(c => c.Key == "KillsGoal")?.Value.ToSafeDecimal() ?? 1m;

        summary.KillsPercentage = killsGoal > 0 ? (summary.KillsDifference / killsGoal) * 100m : 0m;
        summary.HuntPercentage = huntGoal > 0 ? (summary.HuntPoints / huntGoal) * 100m : 0m;
        summary.PurchasePercentage = purchaseGoal > 0 ? (summary.PurchasePoints / purchaseGoal) * 100m : 0m;

        if (!string.Equals(summary.Zone, "New", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(summary.Zone, "Left", StringComparison.OrdinalIgnoreCase))
        {
            var goalsMet = 0;
            if (summary.HuntPercentage >= 95m)
            {
                goalsMet++;
            }

            if (summary.KillsPercentage >= 95m)
            {
                goalsMet++;
            }

            summary.Zone = goalsMet switch
            {
                2 => "Green",
                1 => "Yellow",
                _ => "Red"
            };
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated report value {Field} for user {UserId} in week {Week}.", normalizedField, request.UserId, request.Week);

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> SearchPlayers(string term = "", int take = 20)
    {
        term = (term ?? string.Empty).Trim();
        take = take <= 0 ? 20 : Math.Min(take, 5000);

        var query = _context.StatsSummaries.AsNoTracking().Select(s => s.Name)
            .Concat(_context.Hunts.AsNoTracking().Select(h => h.Name))
            .Concat(_context.Kills.AsNoTracking().Select(k => k.Name))
            .Concat(_context.Kills.AsNoTracking().Select(k => k.OldName));

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(n => n.Contains(term));
        }

        var names = await query
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

