using System.Net.Mail;
using System.Text.RegularExpressions;

namespace WebAPITemplate.Helpers.Validators
{
    public static class BasicFieldsValidator
    {
        public const int StandardStringMaxLength = 256;
        public const int DatabaseStringMaxLength = 4000;
        public const int PasswordMinLength = 6;
        public const int PasswordMaxLength = 20;
        public const int DocumentMinLength = 6;

        public static bool IsBooleanValid(string value, out bool parsedValue)
        {
            return bool.TryParse(value, out parsedValue);
        }

        public static bool IsStringValid(string value, int minLength = 0, int maxLength = StandardStringMaxLength)
        {
            return value.Length > minLength && value.Length <= maxLength;
        }

        public static bool IsIntegerValid(string value, out int parsedValue, int min = int.MinValue, int max = int.MaxValue)
        {
            return int.TryParse(value, out parsedValue) && (parsedValue >= min && parsedValue <= max);
        }

        public static bool IsDecimalValid(string value, out decimal parsedValue, decimal min = decimal.MinValue, decimal max = decimal.MaxValue)
        {
            return decimal.TryParse(value, out parsedValue) && (parsedValue >= min && parsedValue <= max);
        }

        public static bool IsFloatValid(string value, out float parsedValue, float min = float.MinValue, float max = float.MaxValue)
        {
            return float.TryParse(value, out parsedValue) && (parsedValue >= min && parsedValue <= max);
        }

        public static bool IsDocumentIdLengthValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length >= DocumentMinLength;
        }

        public static bool IsEmailValid(string value)
        {
            try
            {
                var result = new MailAddress(value);
                return result.Address == value;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPasswordLengthValid(string value)
        {
            return value.Length >= PasswordMinLength;
        }

        public static bool IsPasswordValid(string value, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                errorMessage = "PasswordEmptyMessage";
            }

            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasLowerChar = new Regex(@"[a-z]+");
            var hasSymbols = new Regex(@"[a-zA-Z0-9 ]+");

            if (value.Length < PasswordMinLength || value.Length > PasswordMaxLength)
            {
                errorMessage = "PasswordLengthMessage";
                return false;
            }

            if (!hasLowerChar.IsMatch(value))
            {
                errorMessage = "PasswordLowerCaseMessage";
                return false;
            }

            if (!hasUpperChar.IsMatch(value))
            {
                errorMessage = "PasswordUpperCaseMessage";
                return false;
            }

            if (!hasNumber.IsMatch(value))
            {
                errorMessage = "PasswordNumericMessage";
                return false;
            }

            if (!hasSymbols.IsMatch(value))
            {
                errorMessage = "PasswordSpecialCharMessage";
                return false;
            }

            return true;
        }
    }
}
