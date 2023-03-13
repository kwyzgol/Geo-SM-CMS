namespace WebApp.Data;

public class ReCaptchaResponse
{
    public bool Success { get; set; }
    public string Challenge_ts { get; set; }
    public string Hostname { get; set; }
    public float Score { get; set; }
}
