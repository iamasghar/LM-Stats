using ClosedXML.Excel;
using LM.Stats.Data.Extensions;
using LM.Stats.Data.Models;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace LM.Stats.Services;

public class ExcelStatsService
{
    private readonly IConfiguration _config;

    public ExcelStatsService(IConfiguration config)
    {
        _config = config;
    }

    public class FileValidationResult
    {
        public bool IsValid { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public int ParsedRows { get; init; }
        public DateTime? SuggestedFromDate { get; init; }
        public DateTime? SuggestedToDate { get; init; }
    }

    private sealed class ParsedSheet
    {
        public HashSet<string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<Dictionary<string, string>> Rows { get; } = new();
    }

    public async Task<FileValidationResult> ValidateHuntFileAsync(IFormFile? file)
    {
        var fileCheck = ValidateFileInput(file, "Hunt");
        if (fileCheck is not null)
        {
            return fileCheck;
        }

        var sheet = await ReadRowsFromFormFileAsync(file!);
        var missingHeaders = FindMissingHeaderGroups(sheet.Headers, HuntRequiredHeaderGroups);
        if (missingHeaders.Any())
        {
            return Invalid($"Missing required Hunt headers: {string.Join(", ", missingHeaders)}");
        }

        var parsedRows = 0;
        DateTime? minDate = null;
        DateTime? maxDate = null;

        for (var i = 0; i < sheet.Rows.Count; i++)
        {
            var row = sheet.Rows[i];
            var rowNo = i + 2;
            var userIdValue = GetString(row, "userid", "user id", "iggid", "igg id");

            if (string.IsNullOrWhiteSpace(userIdValue))
            {
                continue;
            }

            if (!TryParseLong(userIdValue, out _))
            {
                return Invalid($"Invalid Hunt User ID at row {rowNo}.");
            }

            var firstHuntRaw = GetString(row, "firsthunttime", "first hunt time");
            var lastHuntRaw = GetString(row, "lasthunttime", "last hunt time");

            if (!TryParseOptionalDate(firstHuntRaw, out var firstHuntDate))
            {
                return Invalid($"Invalid First Hunt Time at row {rowNo}.");
            }

            if (!TryParseOptionalDate(lastHuntRaw, out var lastHuntDate))
            {
                return Invalid($"Invalid Last Hunt Time at row {rowNo}.");
            }

            if (firstHuntDate.HasValue)
            {
                minDate = !minDate.HasValue || firstHuntDate.Value < minDate.Value ? firstHuntDate : minDate;
                maxDate = !maxDate.HasValue || firstHuntDate.Value > maxDate.Value ? firstHuntDate : maxDate;
            }

            if (lastHuntDate.HasValue)
            {
                minDate = !minDate.HasValue || lastHuntDate.Value < minDate.Value ? lastHuntDate : minDate;
                maxDate = !maxDate.HasValue || lastHuntDate.Value > maxDate.Value ? lastHuntDate : maxDate;
            }

            parsedRows++;
        }

        if (parsedRows == 0)
        {
            return Invalid("Hunt file does not contain any valid player rows.");
        }

        return new FileValidationResult
        {
            IsValid = true,
            ParsedRows = parsedRows,
            SuggestedFromDate = minDate?.Date,
            SuggestedToDate = maxDate?.Date
        };
    }

    public async Task<FileValidationResult> ValidateKillsFileAsync(IFormFile? file)
    {
        var fileCheck = ValidateFileInput(file, "Kills");
        if (fileCheck is not null)
        {
            return fileCheck;
        }

        var sheet = await ReadRowsFromFormFileAsync(file!);
        var missingHeaders = FindMissingHeaderGroups(sheet.Headers, KillRequiredHeaderGroups);
        if (missingHeaders.Any())
        {
            return Invalid($"Missing required Kills headers: {string.Join(", ", missingHeaders)}");
        }

        var parsedRows = 0;
        for (var i = 0; i < sheet.Rows.Count; i++)
        {
            var row = sheet.Rows[i];
            var rowNo = i + 2;
            var iggIdValue = GetString(row, "iggid", "igg id", "userid", "user id");

            if (string.IsNullOrWhiteSpace(iggIdValue))
            {
                continue;
            }

            if (!TryParseLong(iggIdValue, out _))
            {
                return Invalid($"Invalid IGG ID at row {rowNo}.");
            }

            parsedRows++;
        }

        if (parsedRows == 0)
        {
            return Invalid("Kills file does not contain any valid player rows.");
        }

        return new FileValidationResult
        {
            IsValid = true,
            ParsedRows = parsedRows
        };
    }

    public async Task<List<Hunt>> ReadHuntsFromFileAsync(IFormFile file)
    {
        var sheet = await ReadRowsFromFormFileAsync(file);
        var missingHeaders = FindMissingHeaderGroups(sheet.Headers, HuntRequiredHeaderGroups);
        if (missingHeaders.Any())
        {
            throw new InvalidOperationException($"Missing required Hunt headers: {string.Join(", ", missingHeaders)}");
        }

        return sheet.Rows
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
    }

    public async Task<List<Kill>> ReadKillsFromFileAsync(IFormFile file)
    {
        var sheet = await ReadRowsFromFormFileAsync(file);
        var missingHeaders = FindMissingHeaderGroups(sheet.Headers, KillRequiredHeaderGroups);
        if (missingHeaders.Any())
        {
            throw new InvalidOperationException($"Missing required Kills headers: {string.Join(", ", missingHeaders)}");
        }

        return sheet.Rows
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

    private async Task<ParsedSheet> ReadRowsFromFormFileAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        return ext switch
        {
            ".csv" => await ReadCsvRowsAsync(file),
            ".xlsx" => await ReadExcelRowsAsync(file),
            _ => throw new InvalidOperationException("Only CSV and XLSX files are supported.")
        };
    }

    private static async Task<ParsedSheet> ReadExcelRowsAsync(IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var range = worksheet.RangeUsed();
        var parsed = new ParsedSheet();

        if (range is null)
        {
            return parsed;
        }

        var headerRow = range.FirstRowUsed();
        var headers = headerRow.Cells()
            .Select((c, i) => new { Index = i + 1, Name = NormalizeHeader(c.GetString()) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToList();

        foreach (var h in headers)
        {
            parsed.Headers.Add(h.Name);
        }

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
                parsed.Rows.Add(rowData);
            }
        }

        return parsed;
    }

    private static async Task<ParsedSheet> ReadCsvRowsAsync(IFormFile file)
    {
        var parsed = new ParsedSheet();

        using var source = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);
        buffer.Position = 0;

        string? firstLine;
        using (var probeReader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
        {
            firstLine = await probeReader.ReadLineAsync();
        }

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return parsed;
        }

        buffer.Position = 0;
        using var parser = new TextFieldParser(buffer, Encoding.UTF8)
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited
        };

        parser.SetDelimiters(DetectCsvDelimiter(firstLine));

        if (parser.EndOfData)
        {
            return parsed;
        }

        var headersRaw = parser.ReadFields() ?? Array.Empty<string>();
        var normalizedHeaders = headersRaw.Select(h => NormalizeHeader(h ?? string.Empty)).ToList();
        foreach (var h in normalizedHeaders.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            parsed.Headers.Add(h);
        }

        while (!parser.EndOfData)
        {
            string[]? fields;
            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException)
            {
                continue;
            }

            if (fields is null)
            {
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hasData = false;

            for (var i = 0; i < normalizedHeaders.Count; i++)
            {
                var key = normalizedHeaders[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var value = i < fields.Length ? (fields[i] ?? string.Empty).Trim() : string.Empty;
                row[key] = value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    hasData = true;
                }
            }

            if (hasData)
            {
                parsed.Rows.Add(row);
            }
        }

        return parsed;
    }

    private static string[] DetectCsvDelimiter(string line)
    {
        var commaCount = line.Count(c => c == ',');
        var semicolonCount = line.Count(c => c == ';');
        var tabCount = line.Count(c => c == '\t');

        if (tabCount >= semicolonCount && tabCount >= commaCount && tabCount > 0)
        {
            return ["\t"];
        }

        if (semicolonCount > commaCount)
        {
            return [";"];
        }

        return [","];
    }

    private static FileValidationResult? ValidateFileInput(IFormFile? file, string label)
    {
        if (file is null || file.Length == 0)
        {
            return Invalid($"{label} file is required.");
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext is not (".csv" or ".xlsx"))
        {
            return Invalid($"{label} file must be CSV or XLSX.");
        }

        return null;
    }

    private static FileValidationResult Invalid(string message)
    {
        return new FileValidationResult
        {
            IsValid = false,
            ErrorMessage = message
        };
    }

    private static List<string> FindMissingHeaderGroups(HashSet<string> headers, IEnumerable<string[]> requiredHeaderGroups)
    {
        var missing = new List<string>();
        foreach (var group in requiredHeaderGroups)
        {
            var found = group.Select(NormalizeHeader).Any(headers.Contains);
            if (!found)
            {
                missing.Add(group[0]);
            }
        }

        return missing;
    }

    private static bool TryParseOptionalDate(string value, out DateTime? parsedDate)
    {
        parsedDate = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var parsed = value.ToSafeDateTime();
        if (parsed == default || parsed.Year <= 1)
        {
            return true;
        }

        parsedDate = parsed;
        return true;
    }

    private static bool TryParseLong(string value, out long parsed)
    {
        parsed = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var clean = value.Replace(",", string.Empty).Trim();
        if (long.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
        {
            return true;
        }

        if (double.TryParse(clean, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var dbl))
        {
            parsed = (long)Math.Round(dbl, MidpointRounding.AwayFromZero);
            return true;
        }

        return false;
    }

    private static readonly string[][] HuntRequiredHeaderGroups =
    [
        ["User ID", "IGG ID"],
        ["Name", "Player Name"],
        ["Total"],
        ["Hunt", "Hunt Count"],
        ["Purchase"],
        ["Points (Hunt)", "Hunt Points"],
        ["Goal Percentage (Hunt)", "Goal Percentage Hunt"],
        ["Points (Purchase)", "Purchase Points"],
        ["Goal Percentage (Purchase)", "Goal Percentage Purchase"],
        ["First Hunt Time"],
        ["Last Hunt Time"]
    ];

    private static readonly string[][] KillRequiredHeaderGroups =
    [
        ["IGG ID", "User ID"],
        ["Name", "Player Name"],
        ["Rank"],
        ["Might"],
        ["Old Might", "Previous Might"],
        ["Might Difference", "Might Diff"],
        ["Kills"],
        ["Old Kills", "Previous Kills"],
        ["Kills Difference", "Kills Diff"]
    ];

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
