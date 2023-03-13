namespace WebApp.Data
{
    public class BannedEmail : Email
    {
        private string _reason = "";
        private DateTime? _date;

        public string Reason
        {
            get => _reason;
            set
            {
                _reason = value;

                Body = $"<h1>You have been banned.</h1>" +
                       $"<p><b>Reason</b>: {value}</p>";
                if (Date != null) Body += $"<p><b>End date</b>: {Date.ToString()}</p>";
            }
        }
        public DateTime? Date
        {
            get => _date;
            set
            {
                _date = value;

                Body = $"<h1>You have been banned.</h1>" +
                       $"<p><b>Reason</b>: {Reason}</p>";
                if (value != null) Body += $"<p><b>End date</b>: {value.ToString()}</p>";
            }
        }

        public BannedEmail()
        {
            Subject = $"[{Cms.Cms.System?.SystemName ?? ""}] Banned";
        }
    }
}