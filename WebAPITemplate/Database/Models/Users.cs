﻿using System;
using System.Collections.Generic;

namespace WebAPITemplate.Database.Models
{
    public partial class Users
    {
        public Users()
        {
            UserClaims = new HashSet<UserClaims>();
            UserLogins = new HashSet<UserLogins>();
            UserRoles = new HashSet<UserRoles>();
            UserTokens = new HashSet<UserTokens>();
        }

        public string Id { get; set; }
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string ConcurrencyStamp { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime BirthDate { get; set; }
        public string DocumentId { get; set; }
        public string Address { get; set; }

        public ICollection<UserClaims> UserClaims { get; set; }
        public ICollection<UserLogins> UserLogins { get; set; }
        public ICollection<UserRoles> UserRoles { get; set; }
        public ICollection<UserTokens> UserTokens { get; set; }
    }
}
