using CryptoHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using WebAPITemplate.Database;
using WebAPITemplate.Database.Models;
using WebAPITemplate.Helpers;
using WebAPITemplate.RequestContracts;

namespace WebAPITemplate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokensController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<AccountsController> _localizer;

        public TokensController(IUnitOfWork unitOfWork, IStringLocalizer<AccountsController> localizer)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
        }

        [HttpPost]
        public IActionResult Create(TokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email y/o Contraseña invalidos.");
            }

            var user = _unitOfWork.UsersRepository.Get(x => x.Email == request.Email).FirstOrDefault();
            if (user == null)
            {
                return BadRequest("Email y/o Contraseña invalidos.");
            }

            return new ObjectResult(GenerateToken(user, request.Password));
        }

        private object GenerateToken(Users user, string password)
        {
            if (!user.EmailConfirmed)
            {
                return BadRequest(_localizer["EmailNotVerifiedMessage"]);
            }

            if (!Crypto.VerifyHashedPassword(user.PasswordHash, password))
            {
                return BadRequest(_localizer["InvalidLoginCredentials"]);
            }

            try
            {
                var userToken = _unitOfWork.UserTokensRepository.Get(x => x.UserId == user.Id && x.LoginProvider == Globals.DefaultLoginProvider).FirstOrDefault();
                if (userToken != null)
                {
                    if (ValidateToken(userToken.Value))
                    {
                        return userToken.Value;
                    }
                    else
                    {
                        _unitOfWork.UserTokensRepository.Delete(userToken);
                    }
                }

                var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Globals.TokenSecret));
                var claims = new Claim[] {
                    new Claim(ClaimTypes.Name, Globals.TokenName),
                    new Claim(JwtRegisteredClaimNames.Email, Globals.TokenEmail)
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

                _unitOfWork.UserTokensRepository.Insert(new Database.Models.UserTokens
                {
                    LoginProvider = Globals.DefaultLoginProvider,
                    Name = Globals.DefaultTokenName,
                    UserId = user.Id,
                    Value = jwtToken
                });

                return jwtToken;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool ValidateToken(string authToken)
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

        private TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Globals.TokenSecret)),
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