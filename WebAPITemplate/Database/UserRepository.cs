using CryptoHelper;
using WebAPITemplate.Database.Models;
using WebAPITemplate.Helpers.Validators;

namespace WebAPITemplate.Database
{
    public class UsersRepository : Repository<Users>, IUsersRepository
    {
        public UsersRepository(DatabaseContext context) : base(context) { }

        public bool ResetPassword(Users user, string password)
        {
            if (password == null)
            {
                return false;
            }

            if (!BasicFieldsValidator.IsPasswordLengthValid(password))
            {
                return false;
            }

            user.PasswordHash = Crypto.HashPassword(password);
            
            return true;
        }
    }
}
