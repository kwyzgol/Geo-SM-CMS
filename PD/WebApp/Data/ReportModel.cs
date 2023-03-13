using Org.BouncyCastle.Crypto.Tls;

namespace WebApp.Data;

public class ReportModel : DatabaseConnected
{
    public ulong ReportId { get; set; }
    public string? Status { get; set; }
    public ReportType Type { get; set; }
    public string? Content { get; set; }
    public string? ReportCreator { get; set; }
    public ulong? ReportCreatorId { get; set; }
    public object? ReportedContent { get; set; }

    public static Task<OperationResult> Create(ReportType type, long contentId, Content contentType, string accessToken, string reason, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return databases.CreateReport(type, contentId, contentType, accessToken, reason);
    }

    public static async Task<OperationResult> Get(string? accessToken, ReportType? type, 
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && type != null ? 
            await databases.GetReport(accessToken, (ReportType)type) :
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> Delete(string? accessToken, ulong? reportId, 
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && reportId != null ? 
            await databases.DeleteReport(accessToken, (ulong)reportId) : 
            new OperationResult(false, "Error");
    }
}