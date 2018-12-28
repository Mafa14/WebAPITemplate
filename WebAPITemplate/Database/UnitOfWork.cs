using System;
using System.Threading.Tasks;
using WebAPITemplate.Database.Models;

namespace WebAPITemplate.Database
{
    public class UnitOfWork : IDisposable, IUnitOfWork
    {
        private DatabaseContext _context = new DatabaseContext();
        private IRepository<Roles> _rolesRepository;
        private IRepository<RoleClaims> _roleClaimsRepository;
        private IUsersRepository _userRepository;
        private IRepository<UserClaims> _userClaimsRepository;
        private IRepository<UserLogins> _userLoginsRepository;
        private IRepository<UserRoles> _userRolesRepository;
        private IRepository<UserTokens> _userTokensRepository;
        private bool disposed = false;

        IRepository<Roles> IUnitOfWork.RolesRepository
        {
            get
            {
                if (this._rolesRepository == null)
                {
                    this._rolesRepository = new Repository<Roles>(_context);
                }
                return _rolesRepository;
            }
        }

        IRepository<RoleClaims> IUnitOfWork.RoleClaimsRepository
        {
            get
            {
                if (this._roleClaimsRepository == null)
                {
                    this._roleClaimsRepository = new Repository<RoleClaims>(_context);
                }
                return _roleClaimsRepository;
            }
        }

        IUsersRepository IUnitOfWork.UsersRepository
        {
            get
            {
                if (this._userRepository == null)
                {
                    this._userRepository = new UsersRepository(_context);
                }
                return _userRepository;
            }
        }

        IRepository<UserClaims> IUnitOfWork.UserClaimsRepository
        {
            get
            {
                if (this._userClaimsRepository == null)
                {
                    this._userClaimsRepository = new Repository<UserClaims>(_context);
                }
                return _userClaimsRepository;
            }
        }

        IRepository<UserLogins> IUnitOfWork.UserLoginsRepository
        {
            get
            {
                if (this._userLoginsRepository == null)
                {
                    this._userLoginsRepository = new Repository<UserLogins>(_context);
                }
                return _userLoginsRepository;
            }
        }

        IRepository<UserRoles> IUnitOfWork.UserRolesRepository
        {
            get
            {
                if (this._userRolesRepository == null)
                {
                    this._userRolesRepository = new Repository<UserRoles>(_context);
                }
                return _userRolesRepository;
            }
        }

        IRepository<UserTokens> IUnitOfWork.UserTokensRepository
        {
            get
            {
                if (this._userTokensRepository == null)
                {
                    this._userTokensRepository = new Repository<UserTokens>(_context);
                }
                return _userTokensRepository;
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }
    }
}
