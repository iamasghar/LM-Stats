using ClosedXML.Excel;
using LM.Stats.Data.Extensions;
using LM.Stats.Data.Models;
using System.Globalization;

namespace LM.Stats.Services;

public class ExcelStatsService
{
    private readonly IConfiguration _config;

    public ExcelStatsService(IConfiguration config)
    {
        _config = config;
    }

    public Task<List<Hunt>> ReadHuntsAsync()
    {
        var filePath = GetFilePath("Hunt", "Monsters", "Gift", "GiftStats");
        var rows = ReadWorksheetRows(filePath);

        var hunts = rows
            .Select(row => new Hunt
            {
                UserId = GetLong(row, "userid", "user id", "iggid", "igg id"),
                Name = GetString(row, "name", "playername", "player name"),
                Total = GetInt(row, "total"),
                HuntCount = GetInt(row, "hunt", "huntcount", "hunt count"),
                Purchase = GetInt(row, "purchase"),
                L1Hunt = GetInt(row, "l1hunt", "l1(hunt)", "l1 hunt"),
                L2Hunt = GetInt(row, "l2hunt", "l2(hunt)", "l2 hunt"),
                L3Hunt = GetInt(row, "l3hunt", "l3(hunt)", "l3 hunt"),
                L4Hunt = GetInt(row, "l4hunt", "l4(hunt)", "l4 hunt"),
                L5Hunt = GetInt(row, "l5hunt", "l5(hunt)", "l5 hunt"),
                L1Purchase = GetInt(row, "l1purchase", "l1(purchase)", "l1 purchase"),
                L2Purchase = GetInt(row, "l2purchase", "l2(purchase)", "l2 purchase"),
                L3Purchase = GetInt(row, "l3purchase", "l3(purchase)", "l3 purchase"),
                L4Purchase = GetInt(row, "l4purchase", "l4(purchase)", "l4 purchase"),
                L5Purchase = GetInt(row, "l5purchase", "l5(purchase)", "l5 purchase"),
                PointsHunt = GetInt(row, "pointshunt", "points(hunt)", "huntpoints", "hunt points"),
                GoalPercentageHunt = GetString(row, "goalpercentagehunt", "goal percentage(hunt)", "goal percentage hunt"),
                PointsPurchase = GetInt(row, "pointspurchase", "points(purchase)", "purchasepoints", "purchase points"),
                GoalPercentagePurchase = GetString(row, "goalpercentagepurchase", "goal percentage(purchase)", "goal percentage purchase"),
                FirstHuntTime = GetDateTime(row, "firsthunttime", "first hunt time"),
                LastHuntTime = GetDateTime(row, "lasthunttime", "last hunt time")
            })
            .Where(h => h.UserId != 0)
            .ToList();

        return Task.FromResult(hunts);
    }

    public Task<List<Kill>> ReadKillsAsync()
    {
        var filePath = GetFilePath("Kills", "Guild", "GuildList");
        var rows = ReadWorksheetRows(filePath);

        var kills = rows
            .Select(row => new Kill
            {
                IggId = GetLong(row, "iggid", "igg id", "userid", "user id"),
                Name = GetString(row, "name", "playername", "player name"),
                Rank = GetString(row, "rank"),
                Might = GetLong(row, "might"),
                OldMight = GetLong(row, "oldmight", "old might", "previousmight", "previous might"),
                MightDifference = GetLong(row, "mightdifference", "might difference", "mightdiff", "might diff"),
                Kills = GetLong(row, "kills"),
                OldKills = GetLong(row, "oldkills", "old kills", "previouskills", "previous kills"),
                KillsDifference = GetLong(row, "killsdifference", "kills difference", "killsdiff", "kills diff"),
                OldName = GetString(row, "oldname", "old name", "previousname", "previous name")
            })
            .Where(k => k.IggId != 0)
            .ToList();

        return Task.FromResult(kills);
    }

    private string GetFilePath(string type, params string[] fallbackSearchTerms)
    {
        var directory = _config["ExcelSettings:Directory"] ?? @"D:\\LM";
        var configuredName = _config[$"ExcelSettings:{type}FileName"];

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Excel directory not found: {directory}");
        }

        var excelFiles = Directory.GetFiles(directory, "*.xlsx");

        if (!string.IsNullOrWhiteSpace(configuredName))
        {
            var configuredPath = Path.Combine(directory, configuredName);
            if (File.Exists(configuredPath))
            {
                return configuredPath;
            }

            var containsConfigured = excelFiles.FirstOrDefault(f =>
                Path.GetFileName(f).Contains(configuredName, StringComparison.OrdinalIgnoreCase));

            if (containsConfigured is not null)
            {
                return containsConfigured;
            }
        }

        var terms = fallbackSearchTerms.Prepend(type).ToArray();
        foreach (var term in terms)
        {
            var match = excelFiles.FirstOrDefault(f =>
                Path.GetFileNameWithoutExtension(f).Contains(term, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }
        }

        throw new FileNotFoundException($"No Excel file found for type '{type}' in '{directory}'");
    }

    private static List<Dictionary<string, string>> ReadWorksheetRows(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();

        var range = worksheet.RangeUsed();
        if (range is null)
        {
            return new List<Dictionary<string, string>>();
        }

        var headerRow = range.FirstRowUsed();
        var headers = headerRow.Cells()
            .Select((c, i) => new { Index = i + 1, Name = NormalizeHeader(c.GetString()) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToList();

        var rows = new List<Dictionary<string, string>>();

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hasData = false;

            foreach (var header in headers)
            {
                var value = row.Cell(header.Index).GetFormattedString().Trim();
                rowData[header.Name] = value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    hasData = true;
                }
            }

            if (hasData)
            {
                rows.Add(rowData);
            }
        }

        return rows;
    }

    private static string NormalizeHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return string.Empty;
        }

        return new string(header
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static string GetString(Dictionary<string, string> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            var normalized = NormalizeHeader(key);
            if (row.TryGetValue(normalized, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static int GetInt(Dictionary<string, string> row, params string[] keys)
    {
        var value = GetString(row, keys);
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var clean = value.Replace(",", string.Empty).Trim();

        if (int.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue;
        }

        if (double.TryParse(clean, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return (int)Math.Round(doubleValue, MidpointRounding.AwayFromZero);
        }

        return 0;
    }

    private static long GetLong(Dictionary<string, string> row, params string[] keys)
    {
        var value = GetString(row, keys);
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var clean = value.Replace(",", string.Empty).Trim();

        if (long.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue;
        }

        if (double.TryParse(clean, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return (long)Math.Round(doubleValue, MidpointRounding.AwayFromZero);
        }

        return clean.ToSafeLong() ?? 0;
    }

    private static DateTime GetDateTime(Dictionary<string, string> row, params string[] keys)
    {
        var value = GetString(row, keys);
        return value.ToSafeDateTime();
    }
}
