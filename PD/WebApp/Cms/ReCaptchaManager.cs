using System.Text.Json;

namespace WebApp.Cms;

public class ReCaptchaManager : DatabaseConnected
{
    public bool ReCaptchaEnabled { get; set; } = false;
    public string ReCaptchaPublicKey { get; set; } = "";
    public string ReCaptchaPrivateKey { get; set; } = "";
    public float ReCaptchaMinimumScore { get; set; } = 0.5f;

    public async Task<bool> Verify(string? recaptchaToken = null)
    {
        if (ReCaptchaEnabled && recaptchaToken != null)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(5);
                Uri uri = new Uri(
                    $"https://www.google.com/recaptcha/api/siteverify?" +
                    $"secret={ReCaptchaPrivateKey}&" +
                    $"response={recaptchaToken}");
                HttpResponseMessage response = await client.PostAsync(uri, null);

                string responseJson = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var responseObject = JsonSerializer.Deserialize<ReCaptchaResponse>(responseJson, options);
                return responseObject != null &&
                       responseObject.Success &&
                       responseObject.Score >= ReCaptchaMinimumScore;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        else
        {
            return true;
        }
    }

}