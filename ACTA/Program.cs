using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ACTA
{
    public class AppSettings
    {
        // AD LDS Connection
        private static string _adldsHost = null;
        private static string _adldsContainer = null;
        private static string _adldsUsername = null;
        private static string _adldsPassword = null;

        public static string AdldsHost => _adldsHost ?? (_adldsHost = GetRequired("adlds:host"));
        public static string AdldsContainer => _adldsContainer ?? (_adldsContainer = GetRequired("adlds:container"));
        public static string AdldsUsername => _adldsUsername ?? (_adldsUsername = GetRequired("adlds:username"));
        public static string AdldsPassword => _adldsPassword ?? (_adldsPassword = GetRequired("adlds:password"));

        // Test Flags
        public static bool SearchUserEnabled => GetBool("test:searchUser:enabled");
        public static bool ChangePasswordEnabled => GetBool("test:changePassword:enabled");
        public static bool CreateAccountEnabled => GetBool("test:createAccount:enabled");

        // Search User
        public static string SearchUserUsername => GetRequired("test:searchUser:username");

        // Change Password
        public static string ChangePasswordUsername => GetRequired("test:changePassword:username");
        public static string ChangePasswordOldPassword => GetRequired("test:changePassword:oldPassword");
        public static string ChangePasswordNewPassword => GetRequired("test:changePassword:newPassword");

        // Create Account
        public static string CreateAccountUsername => GetRequired("test:createAccount:username");
        public static string CreateAccountPassword => GetRequired("test:createAccount:password");

        // Helpers
        private static string GetRequired(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (value == null)
                throw new ConfigurationErrorsException(string.Format("Missing required config key: '{0}'", key));
            return value;
        }

        private static bool GetBool(string key)
        {
            return string.Equals(ConfigurationManager.AppSettings[key], "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class AuthentDTO
    {
        public string UserIdentifier { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class AdldsTestRunner
    {
        private readonly PrincipalContext _context;

        public AdldsTestRunner()
        {
            _context = new PrincipalContext(
                ContextType.ApplicationDirectory,
                AppSettings.AdldsHost,
                AppSettings.AdldsContainer,
                AppSettings.AdldsUsername,
                AppSettings.AdldsPassword
            );
        }

        public void RunAll()
        {
            if (AppSettings.SearchUserEnabled)
                Log(SearchUser(AppSettings.SearchUserUsername));

            if (AppSettings.ChangePasswordEnabled)
                Log(ChangePassword(new AuthentDTO
                {
                    UserIdentifier = AppSettings.ChangePasswordUsername,
                    OldPassword = AppSettings.ChangePasswordOldPassword,
                    NewPassword = AppSettings.ChangePasswordNewPassword
                }));

            if (AppSettings.CreateAccountEnabled)
                Log(CreateAccount(new AuthentDTO
                {
                    UserIdentifier = AppSettings.CreateAccountUsername,
                    NewPassword = AppSettings.CreateAccountPassword
                }));

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey(true);
        }

        public string SearchUser(string userIdentifier)
        {
            try
            {
                var user = UserPrincipal.FindByIdentity(_context, userIdentifier);

                if (user == null)
                    return "Account not found.";

                var company = GetProperty(user, "company");
                return string.Format("Account found. Company: {0}", company);
            }
            catch (Exception ex)
            {
                return FormatException(ex);
            }
        }

        public string ChangePassword(AuthentDTO dto)
        {
            try
            {
                var user = UserPrincipal.FindByIdentity(_context, dto.UserIdentifier);

                if (user == null)
                    return "User not found.";

                var isValid = _context.ValidateCredentials(dto.UserIdentifier, dto.OldPassword, ContextOptions.SimpleBind);

                if (!isValid)
                    return "Invalid old password.";

                user.SetPassword(dto.NewPassword);
                user.Save();
                return "Password changed successfully.";
            }
            catch (Exception ex)
            {
                return FormatException(ex);
            }
        }

        public string CreateAccount(AuthentDTO dto)
        {
            try
            {
                var existing = UserPrincipal.FindByIdentity(_context, dto.UserIdentifier);

                if (existing != null)
                    return "User already exists.";

                using (var user = new UserPrincipal(_context))
                {
                    user.Name = dto.UserIdentifier;
                    user.UserPrincipalName = dto.UserIdentifier;

                    user.SetPassword(dto.NewPassword);
                    user.Save();
                    user.Enabled = true;
                    user.Save();

                    return "User created successfully.";
                }
            }
            catch (Exception ex)
            {
                return FormatException(ex);
            }
        }

        private static string GetProperty(Principal principal, string property)
        {
            using (var entry = principal.GetUnderlyingObject() as DirectoryEntry)
            {
                if (entry != null && entry.Properties.Contains(property))
                {
                    var value = entry.Properties[property].Value;
                    return value != null ? value.ToString() : string.Empty;
                }
                return string.Empty;
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }

        private static string FormatException(Exception ex)
        {
            if (ex.InnerException == null)
                return ex.Message;
            return string.Format("{0} - {1}", ex.Message, ex.InnerException.Message);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            new AdldsTestRunner().RunAll();
        }
    }
}
