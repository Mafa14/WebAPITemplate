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
            var root = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            SqlDataConnection = root.GetConnectionString("DataConnection");

            var urlConfigurations = new UrlConfigurations
            {
                MainURL = root.GetValue<string>("UrlConfigurations:MainURL"),
                SecureMainURL = root.GetValue<string>("UrlConfigurations:SecureMainURL"),
                RegistrationURL = root.GetValue<string>("UrlConfigurations:RegistrationURL"),
                SecureRegistrationURL = root.GetValue<string>("UrlConfigurations:SecureRegistrationURL"),
                ResetURL = root.GetValue<string>("UrlConfigurations:ResetURL"),
                SecureResetURL = root.GetValue<string>("UrlConfigurations:SecureResetURL")
            };
            UrlConfigurations = urlConfigurations;

            var emailConfigurations = new EmailConfigurations
            {
                PrimaryDomain = root.GetValue<string>("EmailConfigurations:PrimaryDomain"),
                PrimaryPort = root.GetValue<int>("EmailConfigurations:PrimaryPort"),
                SecondayDomain = root.GetValue<string>("EmailConfigurations:SecondayDomain"),
                SecondaryPort = root.GetValue<int>("EmailConfigurations:SecondaryPort"),
                UsernameEmail = root.GetValue<string>("EmailConfigurations:UsernameEmail"),
                UsernamePassword = root.GetValue<string>("EmailConfigurations:UsernamePassword"),
                FromEmail = root.GetValue<string>("EmailConfigurations:FromEmail"),
                ToEmail = root.GetValue<string>("EmailConfigurations:ToEmail"),
                CcEmail = root.GetValue<string>("EmailConfigurations:CcEmail")
            };
            EmailConfigurations = emailConfigurations;
        }

        public string SqlDataConnection { get; }
        public UrlConfigurations UrlConfigurations { get; }
        public EmailConfigurations EmailConfigurations { get; }
    }
}