using CryptoHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using WebAPITemplate.Database;
using WebAPITemplate.Database.Models;
using WebAPITemplate.Helpers.Validators;
using WebAPITemplate.RequestContracts;

namespace WebAPITemplate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailService;
        private readonly IStringLocalizer<AccountsController> _localizer;

        public AccountsController(
            IUnitOfWork unitOfWork,
            IEmailSender emailService,
            IStringLocalizer<AccountsController> localizer)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _localizer = localizer;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            List<string> errors = new List<string>();

            if (!BasicFieldsValidator.IsDocumentIdLengthValid(request.DocumentId))
            {
                errors.Add(string.Format(_localizer["InvalidDocumentIdLength"].Value, BasicFieldsValidator.DocumentMinLength));
            }

            if (!BasicFieldsValidator.IsStringValid(request.UserName))
            {
                errors.Add(string.Format(_localizer["InvalidUserNameLength"].Value, BasicFieldsValidator.StandardStringMaxLength));
            }

            if (!BasicFieldsValidator.IsEmailValid(request.Email))
            {
                errors.Add(_localizer["InvalidEmailFormat"].Value);
            }

            if (!BasicFieldsValidator.IsPasswordLengthValid(request.Password))
            {
                errors.Add(string.Format(_localizer["InvalidPasswordLength"].Value,
                    BasicFieldsValidator.PasswordMinLength,
                    BasicFieldsValidator.PasswordMaxLength));
            }

            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest(_localizer["InvalidPasswordsMatch"].Value);
            }

            if (request.ConfirmationUrl == null)
            {
                return BadRequest(_localizer["MissingHiddenDataMessage"].Value);
            }

            if (errors.Any())
            {
                return BadRequest(string.Join(Environment.NewLine, errors.Select(e => "- " + e).ToArray()));
            }

            var newUser = new Users
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = request.DocumentId,
                UserName = request.UserName,
                NormalizedUserName = request.UserName,
                Email = request.Email,
                NormalizedEmail = request.Email,
                PasswordHash = Crypto.HashPassword(request.Password),
                EmailConfirmed = true // TODO: DELETE THIS AFTER DEPLOYING THE EMAILING SERVICE
            };

            try
            {
                _unitOfWork.UsersRepository.Insert(newUser);
                await _unitOfWork.SaveAsync();
            }
            catch (SqlException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["DatabaseConnectionException"].Value);
            }
            catch (Exception)
            {
                return BadRequest(_localizer["InvalidUserCreation"].Value);
            }

            var tokenVerificationUrl = HttpUtility.UrlDecode(request.ConfirmationUrl)
                .Replace("#Id", newUser.Id)
                .Replace("#Token", UserManager<Users>.ConfirmEmailTokenPurpose);
            await _emailService.SendEmailAsync(request.Email, _localizer["EmailVerificationSubject"].Value,
                string.Format(_localizer["EmailVerificationMessage"].Value, tokenVerificationUrl));

            return Ok(string.Format(_localizer["RegistrationConfirmationMessage"].Value, request.Email));
        }

        [HttpPost]
        [Route("forgot")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = _unitOfWork.UsersRepository.Get(x => x.Email == request.Email).FirstOrDefault();
            if (user == null)
            {
                return BadRequest(_localizer["ForgotPasswordCheckEmailMessage"].Value);
            }

            if (request.ConfirmationUrl == null)
            {
                return BadRequest(_localizer["MissingHiddenDataMessage"].Value);
            }

            var tokenVerificationUrl = HttpUtility.UrlDecode(request.ConfirmationUrl)
                .Replace("#Id", user.Id)
                .Replace("#Token", UserManager<Users>.ResetPasswordTokenPurpose);

            await _emailService.SendEmailAsync(request.Email, _localizer["ForgotPasswordSubject"].Value,
                string.Format(_localizer["ForgotPasswordMessage"].Value, tokenVerificationUrl));

            return Ok(_localizer["ForgotPasswordCheckEmailMessage"].Value);
        }

        [HttpPost]
        [Route("reset")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var user = _unitOfWork.UsersRepository.GetByID(request.Id);
            if (user == null)
            {
                throw new InvalidOperationException();
            }

            if (request.Token != UserManager<Users>.ResetPasswordTokenPurpose)
            {
                return BadRequest(_localizer["MissingHiddenDataMessage"].Value);
            }

            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest(_localizer["InvalidPasswordsMatch"].Value);
            }

            var resetPasswordResult = _unitOfWork.UsersRepository.ResetPassword(user, request.Password);
            if (!resetPasswordResult)
            {
                return BadRequest(_localizer["InvalidPasswordReset"].Value);
            }

            try
            {
                _unitOfWork.UsersRepository.Update(user);
                await _unitOfWork.SaveAsync();
            }
            catch (SqlException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["DatabaseConnectionException"].Value);
            }
            catch (Exception)
            {
                return BadRequest(_localizer["InvalidPasswordReset"].Value);
            }

            return Ok(_localizer["ResetPasswordSuccessfully"].Value);
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login(LoginRequest request)
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

            if (!user.EmailConfirmed)
            {
                return BadRequest(_localizer["EmailNotVerifiedMessage"].Value);
            }

            if (!Crypto.VerifyHashedPassword(user.PasswordHash, request.Password))
            {
                return BadRequest(_localizer["InvalidLoginCredentials"].Value);
            }

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.DocumentId,
                request.Password,
                message = _localizer["LoginSuccessfully"].Value
            });
        }

        [HttpPost]
        [Route("logout")]
        public IActionResult Logout()
        {
            return Ok(_localizer["LogoutSuccessfully"].Value);
        }

        [HttpPost]
        [Route("confirm/email")]
        public IActionResult ConfirmEmail(string id, string token)
        {
            var user = _unitOfWork.UsersRepository.GetByID(id);
            if (user == null)
            {
                return BadRequest(_localizer["InvalidLoginCredentials"].Value);
            }

            if (UserManager<Users>.ConfirmEmailTokenPurpose != token)
            {
                return BadRequest(_localizer["MissingHiddenDataMessage"].Value);
            }

            if (user.EmailConfirmed)
            {
                return Ok();
            }

            try
            {
                user.EmailConfirmed = true;
                _unitOfWork.UsersRepository.Update(user);
                _unitOfWork.Save();
            }
            catch (SqlException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["DatabaseConnectionException"].Value);
            }
            catch (Exception)
            {
                return BadRequest(_localizer["InvalidUserUpdate"].Value);
            }

            return Ok();
        }

        [HttpPost]
        [Route("change")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var user = _unitOfWork.UsersRepository.GetByID(request.Id);
            if (user == null)
            {
                throw new InvalidOperationException();
            }

            if (!Crypto.VerifyHashedPassword(user.PasswordHash, request.OldPassword))
            {
                return BadRequest(_localizer["InvalidPassword"].Value);
            }

            if (request.NewPassword != request.NewConfirmPassword)
            {
                return BadRequest(_localizer["InvalidPasswordsMatch"].Value);
            }

            var resetPasswordResult = _unitOfWork.UsersRepository.ResetPassword(user, request.NewPassword);
            if (!resetPasswordResult)
            {
                return BadRequest(_localizer["InvalidPasswordReset"].Value);
            }

            try
            {
                _unitOfWork.UsersRepository.Update(user);
                await _unitOfWork.SaveAsync();
            }
            catch (SqlException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["DatabaseConnectionException"].Value);
            }
            catch (Exception)
            {
                return BadRequest(_localizer["InvalidPasswordReset"].Value);
            }

            return Ok(_localizer["ResetPasswordSuccessfully"].Value);
        }
    }
}