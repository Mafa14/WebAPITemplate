using WebAPITemplate.Database.Models;

namespace WebAPITemplate.Database
{
    public interface IUsersRepository : IRepository<Users>
    {
        bool ResetPassword(Users user, string token, string password);
    }
}
