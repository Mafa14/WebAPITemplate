using CryptoHelper;
using System.Collections.Generic;
using System.Linq;
using WebAPITemplate.Database.Models;
using WebAPITemplate.Helpers.DataTables;
using WebAPITemplate.Helpers.Validators;
using WebAPITemplate.RequestContracts.DataTable;

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

        public IEnumerable<Users> Get(DataTableRequest request)
        {
            IQueryable<Users> query = _dbSet;
            var filter = ExpressionsGenerator.GetFilter<Users>(request.Columns, "users");

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (request.Order != null && request.Order.Count() > 0)
            {
                query = ExpressionsGenerator.OrderFilter(query, request.Columns, request.Order);
            }

            return query.Skip(request.Start).Take(request.Length).ToList();
        }
    }
}
