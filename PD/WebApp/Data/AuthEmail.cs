namespace WebApp.Data
{
    public class AuthEmail : Email
    {
        private int _code = 0;
        public int Code
        {
            get => _code;
            set
            {
                _code = value;
                Body = $"<p>Your authorization code: <b>{value}</b></p>";
            }
        }

        public AuthEmail()
        {
            Subject = $"[{Cms.Cms.System?.SystemName ?? ""}] Authorization";
        }
    }
}
