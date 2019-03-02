using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using WebAPITemplate.Database;
using WebAPITemplate.Helpers.Token;
using WebAPITemplate.RequestContracts;

namespace WebAPITemplate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                return BadRequest(_localizer["InvalidLoginCredentials"].Value);
            }

            var user = _unitOfWork.UsersRepository.Get(x => x.Email == request.Email).FirstOrDefault();
            if (user == null)
            {
                return BadRequest(_localizer["InvalidLoginCredentials"].Value);
            }

            try
            {
                return new ObjectResult(TokenHandler.GenerateToken(_localizer, _unitOfWork, user, request.Password));
            }
            catch (MissingFieldException mfe)
            {
                return BadRequest(mfe.Message);
            }
            catch (InvalidCastException ice)
            {
                return BadRequest(ice.Message);
            }
        }
    }
}