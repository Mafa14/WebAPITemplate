using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAPITemplate.Helpers;
using WebAPITemplate.RequestContracts;

namespace WebAPITemplate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokensController : ControllerBase
    {
        [HttpPost]
        public IActionResult Create(TokenRequest tokenRequest)
        {
            // TODO: Validate the user and password
            //if (IsValidUserAndPasswordCombination(username, password))
            return new ObjectResult(GenerateToken(tokenRequest.UserName));
            //return BadRequest();
        }

        private object GenerateToken(string username)
        {
            // TODO: Save the Token for this user.
            // TODO: Check if it has expired, if not provide the saved Token and if it did then create a new one and save it.
            try
            {
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
                return jwtToken;
            }
            catch (Exception)
            {
                // TODO: Log exception
                return null;
            }
        }
    }
}