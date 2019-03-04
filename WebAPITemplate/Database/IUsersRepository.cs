using System.Collections.Generic;
using WebAPITemplate.Database.Models;
using WebAPITemplate.RequestContracts.DataTable;

namespace WebAPITemplate.Database
{
    public interface IUsersRepository : IRepository<Users>
    {
        bool ResetPassword(Users user, string password);
        IEnumerable<Users> Get(DataTableRequest request);
    }
}
