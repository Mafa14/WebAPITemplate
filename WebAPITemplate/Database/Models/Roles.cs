using System.Collections.Generic;

namespace WebAPITemplate.Database.Models
{
    public partial class Roles
    {
        public Roles()
        {
            RoleClaims = new HashSet<RoleClaims>();
            UserRoles = new HashSet<UserRoles>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string ConcurrencyStamp { get; set; }

        public ICollection<RoleClaims> RoleClaims { get; set; }
        public ICollection<UserRoles> UserRoles { get; set; }
    }
}
