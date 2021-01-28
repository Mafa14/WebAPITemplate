using CryptoHelper;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using WebAPITemplate.Controllers;
using WebAPITemplate.Database;
using WebAPITemplate.Database.Models;

namespace WebAPITemplate.Helpers.Token
{
    public static class TokenHandler
    {
        public static object GenerateToken(IStringLocalizer<AccountsController> localizer, IUnitOfWork unitOfWork, Users user, string password)
        {
            if (!user.EmailConfirmed)
            {
                throw new MissingFieldException(localizer["EmailNotVerifiedMessage"].Value);
            }

            if (!Crypto.VerifyHashedPassword(user.PasswordHash, password))
            {
                throw new InvalidCastException(localizer["InvalidLoginCredentials"].Value);
            }

            try
            {
                var userToken = unitOfWork.UserTokensRepository.Get(x => x.UserId == user.Id && x.LoginProvider == Globals.DefaultLoginProvider).FirstOrDefault();
                if (userToken != null)
                {
                    if (ValidateToken(userToken.Value))
                    {
                        return userToken.Value;
                    }
                    else
                    {
                        unitOfWork.UserTokensRepository.Delete(userToken);
                    }
                }

                //var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Globals.TokenSecret));
                var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Globals.TokenSecret));
                var claims = new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email)
                };

                var token = new JwtSecurityToken(
                    issuer: Globals.TokenIssuer,
                    audience: Globals.TokenAudience,
                    claims: claims,
                    notBefore: DateTime.Now,
                    expires: DateTime.Now.AddDays(28),
                    signingCredentials: new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256)
                );

                string jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

                unitOfWork.UserTokensRepository.Insert(new Database.Models.UserTokens
                {
                    LoginProvider = Globals.DefaultLoginProvider,
                    Name = Globals.DefaultTokenName,
                    UserId = user.Id,
                    Value = jwtToken
                });
                unitOfWork.Save();

                return jwtToken;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool ValidateToken(string authToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters();

            SecurityToken validatedToken;

            try
            {
                tokenHandler.ValidateToken(authToken, validationParameters, out validatedToken);
            }
            catch (Exception)
            {
                return false;
            }

            return validatedToken != null;
        }

        private static TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Globals.TokenSecret)),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Globals.TokenSecret)),
                ValidateIssuer = true,
                ValidIssuer = Globals.TokenIssuer,
                ValidateAudience = true,
                ValidAudience = Globals.TokenAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        }
    }
}
