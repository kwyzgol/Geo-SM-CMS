namespace WebApp.Data;

public class BanModel
{
    public ulong BanId { get; set; } = 0;
    public string Reason { get; set; } = "";
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public ulong UserId { get; set; } = 0;
    public string Username { get; set; } = "";
    public ulong? ModeratorId { get; set; }
    public string? ModeratorName { get; set; }
}
