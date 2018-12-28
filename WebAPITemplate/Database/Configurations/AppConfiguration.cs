using Microsoft.Extensions.Configuration;
using System.IO;

namespace WebAPITemplate.Database.Configurations
{
    public class AppConfiguration
    {
        private static AppConfiguration _appConfiguration = null;

        public static AppConfiguration Configurations
        {
            get
            {
                if (_appConfiguration == null)
                {
                    _appConfiguration = new AppConfiguration();
                }
                return _appConfiguration;
            }
        }

        private AppConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            configurationBuilder.AddJsonFile(path, false);

            var root = configurationBuilder.Build();
            SqlDataConnection = root.GetConnectionString("DataConnection");

            var urlConfigurations = new UrlConfigurations
            {
                MainURL = root.GetValue<string>("MainURL"),
                SecureMainURL = root.GetValue<string>("SecureMainURL"),
                RegistrationURL = root.GetValue<string>("RegistrationURL"),
                SecureRegistrationURL = root.GetValue<string>("SecureRegistrationURL"),
                ResetURL = root.GetValue<string>("ResetURL"),
                SecureResetURL = root.GetValue<string>("SecureResetURL")
            };
            UrlConfigurations = urlConfigurations;

            var emailConfigurations = new EmailConfigurations
            {
                PrimaryDomain = root.GetValue<string>("PrimaryDomain"),
                PrimaryPort = root.GetValue<int>("PrimaryPort"),
                SecondayDomain = root.GetValue<string>("SecondayDomain"),
                SecondaryPort = root.GetValue<int>("SecondaryPort"),
                UsernameEmail = root.GetValue<string>("UsernameEmail"),
                UsernamePassword = root.GetValue<string>("UsernamePassword"),
                FromEmail = root.GetValue<string>("FromEmail"),
                ToEmail = root.GetValue<string>("ToEmail"),
                CcEmail = root.GetValue<string>("CcEmail")
            };
            EmailConfigurations = emailConfigurations;
        }

        public string SqlDataConnection { get; }
        public UrlConfigurations UrlConfigurations { get; }
        public EmailConfigurations EmailConfigurations { get; }
    }
}