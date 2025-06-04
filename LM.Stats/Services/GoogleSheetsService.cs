using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace LM.Stats.Services;

public class GoogleSheetsService
{
    private readonly IConfiguration _config;
    private readonly string[] _scopes = { SheetsService.Scope.SpreadsheetsReadonly };

    public GoogleSheetsService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IList<IList<object>>> GetSheetData(string sheetName)
    {
        var credential = await GetServiceAccountCredential();
        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "LM StatsInfo"
        });

        var range = $"{sheetName}!A:Z";
        var request = service.Spreadsheets.Values.Get(_config["GoogleSettings:SheetId"], range);

        var response = await request.ExecuteAsync();
        return response.Values;
    }

    private async Task<ServiceAccountCredential> GetServiceAccountCredential()
    {
        using var stream = new FileStream(_config["GoogleSettings:ServiceAccountKeyPath"], FileMode.Open, FileAccess.Read);
        var credential = GoogleCredential.FromStream(stream)
            .CreateScoped(_scopes)
            .UnderlyingCredential as ServiceAccountCredential;

        return credential ?? throw new InvalidOperationException("Failed to create service account credential");
    }
}