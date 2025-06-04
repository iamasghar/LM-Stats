using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace LM.Stats.Services;

public class GoogleDriveService
{
    private readonly IConfiguration _config;
    private readonly string[] _scopes = { DriveService.Scope.DriveFile };
    
    public GoogleDriveService(IConfiguration config)
    {
        _config = config;
    }
    
    public async Task<bool> UploadFile(string filePath, string fileName)
    {
        try
        {
            var credential = await GetServiceAccountCredential();
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "LM StatsInfo"
            });
            
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName,
                Parents = new List<string> { _config["GoogleSettings:DriveFolderId"] }
            };
            
            using var stream = new FileStream(filePath, FileMode.Open);
            var request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
            request.Fields = "id";
            
            var result = await request.UploadAsync();
            return result.Status == UploadStatus.Completed;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> DownloadFile(string fileId, string savePath)
    {
        try
        {
            var credential = await GetServiceAccountCredential();
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "LM StatsInfo"
            });
            
            var request = service.Files.Get(fileId);
            using var stream = new FileStream(savePath, FileMode.Create);
            await request.DownloadAsync(stream);
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<string> GetLatestBackupFileId()
    {
        try
        {
            var credential = await GetServiceAccountCredential();
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "LM StatsInfo"
            });
            
            var request = service.Files.List();
            request.Q = $"'{_config["GoogleSettings:DriveFolderId"]}' in parents and name contains 'data.db'";
            request.Fields = "files(id, name, createdTime)";
            request.OrderBy = "createdTime desc";
            request.PageSize = 1;
            
            var result = await request.ExecuteAsync();
            return result.Files?.FirstOrDefault()?.Id;
        }
        catch
        {
            return null;
        }
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