using System.Threading.Tasks;
using WebAPITemplate.Database.Models;

namespace WebAPITemplate.Database
{
    public interface IUnitOfWork
    {
        IRepository<Roles> RolesRepository { get; }
        IRepository<RoleClaims> RoleClaimsRepository { get; }
        IUsersRepository UsersRepository { get; }
        IRepository<UserClaims> UserClaimsRepository { get; }
        IRepository<UserLogins> UserLoginsRepository { get; }
        IRepository<UserRoles> UserRolesRepository { get; }
        IRepository<UserTokens> UserTokensRepository { get; }

        void Save();
        Task<int> SaveAsync();
        void Dispose();
    }
}

