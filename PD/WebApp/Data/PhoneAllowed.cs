using System.Text.RegularExpressions;

namespace WebApp.Data
{
    public class PhoneAllowed
    {
        private readonly string _countryCode;
        private readonly int _maxNumbers;

        public string CountryCode => _countryCode;
        public int MaxNumbers => _maxNumbers;

        public PhoneAllowed(string countryCode, int maxNumbers)
        {
            _countryCode = countryCode;
            _maxNumbers = maxNumbers;
        }

        public bool Verify(string phoneNumber)
        {
            string pattern = $"^\\{_countryCode}\\d{{{_maxNumbers}}}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }
    }
}
