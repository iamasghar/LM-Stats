using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace LM.Stats.Services;

public class GoogleSheetsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleSheetsService> _logger;
    private readonly string[] _scopes = { SheetsService.Scope.SpreadsheetsReadonly };

    public GoogleSheetsService(IConfiguration config, ILogger<GoogleSheetsService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<IList<IList<object>>> GetSheetData(string sheetName)
    {
        using (_logger.BeginScope("GetSheetData for {SheetName}", sheetName))
        {
            try
            {
                _logger.LogInformation("Starting to fetch sheet data for {SheetName}", sheetName);

                var credential = await GetServiceAccountCredential();
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "LM StatsInfo"
                });

                var range = $"{sheetName}!A:Z";
                _logger.LogDebug("Preparing request for range {Range}", range);

                var request = service.Spreadsheets.Values.Get(_config["GoogleSettings:SheetId"], range);

                _logger.LogInformation("Executing Google Sheets API request");
                var response = await request.ExecuteAsync();

                _logger.LogInformation("Successfully retrieved {RowCount} rows from sheet {SheetName}",
                    response.Values?.Count ?? 0, sheetName);

                return response.Values ?? new List<IList<object>>();
            }
            catch (GoogleApiException ex)
            {
                _logger.LogError($"Google API Error: {ex.Error.Code} - {ex.Error.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sheet data for {SheetName}", sheetName);
                throw;
            }
        }
    }

    private async Task<ServiceAccountCredential> GetServiceAccountCredential()
    {
        try
        {
            var keyPath = _config["GoogleSettings:ServiceAccountKeyPath"];
            _logger.LogDebug("Attempting to read service account key from {KeyPath}", keyPath);

            if (string.IsNullOrEmpty(keyPath))
            {
                _logger.LogError("Service account key path is not configured");
                throw new InvalidOperationException("Service account key path is not configured");
            }

            if (!File.Exists(keyPath))
            {
                _logger.LogError("Service account key file not found at {KeyPath}", keyPath);
                throw new FileNotFoundException("Service account key file not found", keyPath);
            }

            await using var stream = new FileStream(keyPath, FileMode.Open, FileAccess.Read);
            var credential = GoogleCredential.FromStream(stream)
                .CreateScoped(_scopes)
                .UnderlyingCredential as ServiceAccountCredential;

            if (credential == null)
            {
                _logger.LogError("Failed to create service account credential");
                throw new InvalidOperationException("Failed to create service account credential");
            }

            _logger.LogInformation("Successfully created service account credential");
            return credential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetServiceAccountCredential");
            throw;
        }
    }
}