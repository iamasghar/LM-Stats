using LM.Stats.Data;
using LM.Stats.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LM.Stats.Services;

public class DatabaseService
{
    private readonly AppDbContext _context;

    public DatabaseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(int Total, int stateId)> SaveStatsData(StatsInfo stats,
        List<Hunt> hunts,
        List<Kill> kills,
        List<OtherStat> otherStats)
    {
        int stateId = 0;
        await _context.Stats.AddAsync(stats);
        await _context.SaveChangesAsync();

        stateId = stats.Id;

        foreach (var hunt in hunts)
        {
            hunt.StatsId = stats.Id;
        }
        await _context.Hunts.AddRangeAsync(hunts);

        foreach (var kill in kills)
        {
            kill.StatsId = stats.Id;
        }
        await _context.Kills.AddRangeAsync(kills);

        foreach (var otherStat in otherStats)
        {
            otherStat.StatsId = stats.Id;
        }
        await _context.OtherStats.AddRangeAsync(otherStats);

        return (await _context.SaveChangesAsync(), stateId);
    }

    public async Task<bool> BackupDatabase(string backupPath)
    {
        try
        {
            var dbPath = _context.Database.GetDbConnection().DataSource;
            File.Copy(dbPath, backupPath, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestoreDatabase(string sourcePath)
    {
        try
        {
            var dbPath = _context.Database.GetDbConnection().DataSource;
            File.Copy(sourcePath, dbPath, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}