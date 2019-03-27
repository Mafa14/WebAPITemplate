using CryptoHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAPITemplate.Database.Models;
using WebAPITemplate.Helpers;

namespace WebAPITemplate.Database.Seeds
{
    public class AppUsersSeed
    {
        public static async Task SeedAsync(DatabaseContext context, int? retry = 0)
        {
            context.Database.EnsureCreated();

            int retryForAvailability = retry.Value;
            string adminRoleId = Guid.NewGuid().ToString();
            string adminUserId = Guid.NewGuid().ToString();

            try
            {
                if (!context.Roles.Any())
                {
                    context.Roles.AddRange(GetPreconfiguredRoles(adminRoleId));

                    await context.SaveChangesAsync();
                }

                if (!context.Users.Any())
                {
                    context.Users.AddRange(GetPreconfiguredUsers(adminUserId));

                    await context.SaveChangesAsync();
                }

                if (!context.UserRoles.Any())
                {
                    context.UserRoles.AddRange(GetPreconfiguredUserRoles(adminRoleId, adminUserId));

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                if (retryForAvailability < 10)
                {
                    retryForAvailability++;
                    await SeedAsync(context, retryForAvailability);
                }
            }
        }

        static IEnumerable<Roles> GetPreconfiguredRoles(string adminRoleId)
        {
            return new List<Roles>()
            {
                new Roles() { Id = adminRoleId, Name = SystemRoles.Admin.ToString(), ConcurrencyStamp = new TimeSpan().ToString()},
                new Roles() { Id = Guid.NewGuid().ToString(), Name = SystemRoles.Client.ToString(), ConcurrencyStamp = new TimeSpan().ToString()}
            };
        }

        static IEnumerable<Users> GetPreconfiguredUsers(string adminUserId)
        {
            return new List<Users>()
            {
                new Users() {
                    Id = adminUserId,
                    DocumentId = "1000000-0",
                    UserName = "Admin",
                    BirthDate = new DateTime(),
                    Email = "admin@gmail.com",
                    EmailConfirmed = true,
                    PasswordHash = Crypto.HashPassword("admin898"),
                    ConcurrencyStamp = new TimeSpan().ToString()
                }
            };
        }

        static IEnumerable<UserRoles> GetPreconfiguredUserRoles(string adminRoleId, string adminUserId)
        {
            return new List<UserRoles>()
            {
                new UserRoles() {RoleId = adminRoleId, UserId = adminUserId }
            };
        }
    }
}