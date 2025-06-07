using LM.Stats.Services;
using LM.Stats.Data.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LM.Stats.Data.Extensions;
using System.Globalization;

namespace LM.Stats.Controllers;

public class HomeController : Controller
{
    private readonly GoogleSheetsService _sheetsService;
    private readonly DatabaseService _dbService;
    private readonly GoogleDriveService _driveService;
    private readonly IConfiguration _config;
    
    public HomeController(
        GoogleSheetsService sheetsService,
        DatabaseService dbService,
        GoogleDriveService driveService,
        IConfiguration config)
    {
        _sheetsService = sheetsService;
        _dbService = dbService;
        _driveService = driveService;
        _config = config;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> ImportFromSheets(DateTime fromDate, DateTime toDate, string uniqueId)
    {
        try
        {
            // Get data from sheets
            var configData = await _sheetsService.GetSheetData("Configs");
            var huntData = await _sheetsService.GetSheetData("Hunt");
            var killData = await _sheetsService.GetSheetData("Kills");
            var otherStatsData = await _sheetsService.GetSheetData("OtherStats");
            
            // Process data
            var stats = new StatsInfo
            {
                FromDate = fromDate,
                ToDate = toDate,
                UniqueIdentifier = uniqueId
            };
            
            var hunts = ProcessHuntData(huntData);
            var kills = ProcessKillData(killData);
            var otherStats = ProcessOtherStatsData(otherStatsData);
            
            // Save to database
            await _dbService.SaveStatsData(stats, hunts, kills, otherStats);
            
            return Json(new { success = true, message = "Data imported successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> BackupToDrive()
    {
        try
        {
            var dbPath = _config.GetConnectionString("DefaultConnection").Replace("Data Source=", "");
            var backupPath = Path.Combine(Path.GetDirectoryName(dbPath), "data_backup.db");
            
            // Create backup
            await _dbService.BackupDatabase(backupPath);
            
            // Upload to Drive
            var success = await _driveService.UploadFile(backupPath, $"data_{DateTime.UtcNow:yyyyMMddHHmmss}.db");
            
            if (success)
            {
                return Json(new { success = true, message = "Backup created and uploaded successfully!" });
            }
            return Json(new { success = false, message = "Failed to upload backup to Drive" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> SyncFromDrive()
    {
        try
        {
            var dbPath = _config.GetConnectionString("DefaultConnection").Replace("Data Source=", "");
            var tempPath = Path.Combine(Path.GetDirectoryName(dbPath), "data_temp.db");
            
            // Get latest backup from Drive
            var fileId = await _driveService.GetLatestBackupFileId();
            if (string.IsNullOrEmpty(fileId))
            {
                return Json(new { success = false, message = "No backup found on Drive" });
            }
            
            // Download and restore
            var success = await _driveService.DownloadFile(fileId, tempPath);
            if (success)
            {
                success = await _dbService.RestoreDatabase(tempPath);
                if (success)
                {
                    return Json(new { success = true, message = "Database restored from Drive successfully!" });
                }
            }
            
            return Json(new { success = false, message = "Failed to restore database" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
    
    private List<Hunt> ProcessHuntData(IList<IList<object>> data)
    {
        // Skip header row
        return data.Skip(1).Select(row => new Hunt
        {
            UserId = long.Parse(row[0].ToString()),
            Name = row[1].ToString(),
            Total = int.Parse(row[2].ToString()),
            HuntCount = int.Parse(row[3].ToString()),
            Purchase = int.Parse(row[4].ToString()),
            L1Hunt = int.Parse(row[6].ToString()),
            L2Hunt = int.Parse(row[7].ToString()),
            L3Hunt = int.Parse(row[8].ToString()),
            L4Hunt = int.Parse(row[9].ToString()),
            L5Hunt = int.Parse(row[10].ToString()),
            L1Purchase = int.Parse(row[12].ToString()),
            L2Purchase = int.Parse(row[13].ToString()),
            L3Purchase = int.Parse(row[14].ToString()),
            L4Purchase = int.Parse(row[15].ToString()),
            L5Purchase = int.Parse(row[16].ToString()),
            PointsHunt = int.Parse(row[18].ToString()),
            GoalPercentageHunt = row[19].ToString(),
            PointsPurchase = int.Parse(row[21].ToString()),
            GoalPercentagePurchase = row[22].ToString(),
            FirstHuntTime = row[24].ToString().ToSafeDateTime(),
            LastHuntTime = row[25].ToString().ToSafeDateTime()
        }).ToList();
    }

    private List<Kill> ProcessKillData(IList<IList<object>> data)
    {
        return data.Skip(1).Select(row => new Kill
        {
            IggId = row.ElementAtOrDefault(0)?.ToString().ToSafeLong() ?? 0,
            Name = row.ElementAtOrDefault(1)?.ToString().Trim() ?? string.Empty,
            Rank = row.ElementAtOrDefault(2)?.ToString().Trim() ?? string.Empty,
            Might = row.ElementAtOrDefault(3)?.ToString().Replace(",", "").ToSafeLong() ?? 0,
            OldMight = row.ElementAtOrDefault(4)?.ToString().Replace(",", "").ToSafeLong() ?? 0,
            MightDifference = row.ElementAtOrDefault(5)?.ToString().Replace(",", "").ToSafeLong() ?? 0,
            Kills = row.ElementAtOrDefault(6)?.ToString().Replace(",", "").ToSafeLong() ?? 0,
            OldKills = row.ElementAtOrDefault(7)?.ToString().Replace(",", "").ToSafeLong() ?? 0,
            KillsDifference = row.ElementAtOrDefault(8)?.ToString().Replace(",", "").ToSafeLong() ?? 0,
            OldName = row.Count > 9 ? row[9]?.ToString().Trim() : string.Empty // Safe access
        }).Where(k => k.IggId != 0)
          .ToList();
    }

    private List<OtherStat> ProcessOtherStatsData(IList<IList<object>> data)
    {
        return data.Skip(1).Where(row => row.Count >= 7).Select(row => new OtherStat
        {
            UserId = row[0].ToString().ToSafeLong() ?? 0,
            Name = row[1].ToString().Trim(),
            Might = row[2].ToString()
                         .Replace(",", "")
                         .ToSafeLong() ?? 0,
            Kills = row[3].ToString()
                         .Replace(",", "")
                         .ToSafeLong() ?? 0,
            TroopsKilled = row[4].ToString()
                               .Replace(",", "")
                               .ToSafeLong() ?? 0,
            EnemiesDestroyedMight = row[5].ToString()
                                        .Replace(",", "")
                                        .ToSafeLong() ?? 0,
            TroopsLost = row[6].ToString()
                              .Replace(",", "")
                              .ToSafeLong() ?? 0,
            WinRate = ParseWinRate(row.Count > 7 ? row[7].ToString() : null)
        }).Where(o => o.UserId != 0) // Filter out invalid rows
          .ToList();
    }
    private static string ParseWinRate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "0%";

        // Handle both "76.09%" and "76.09" formats
        var cleanInput = input.Trim()
                             .Replace("%", "")
                             .Replace(",", ".");

        if (decimal.TryParse(cleanInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
        {
            return rate <= 1 ? $"{rate:P2}" : $"{rate / 100m:P2}"; // Handle both 0.76 and 76 formats
        }

        return "0%";
    }
}