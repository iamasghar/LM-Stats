using LM.Stats.Services;
using LM.Stats.Data.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var configData = await _sheetsService.GetSheetData("configs");
            var huntData = await _sheetsService.GetSheetData("Hunt");
            var killData = await _sheetsService.GetSheetData("Kills");
            var otherStatsData = await _sheetsService.GetSheetData("Other StatsInfo");
            
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
            FirstHuntTime = DateTime.Parse(row[24].ToString()),
            LastHuntTime = DateTime.Parse(row[25].ToString())
        }).ToList();
    }
    
    private List<Kill> ProcessKillData(IList<IList<object>> data)
    {
        // Similar processing for Kill data
        // Implementation omitted for brevity
        return new List<Kill>();
    }
    
    private List<OtherStat> ProcessOtherStatsData(IList<IList<object>> data)
    {
        // Similar processing for OtherStats data
        // Implementation omitted for brevity
        return new List<OtherStat>();
    }
}