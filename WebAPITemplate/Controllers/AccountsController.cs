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

            if (!BasicFieldsValidatior.IsDocumentIdLengthValid(request.DocumentId))
            {
                errors.Add(string.Format(_localizer["InvalidDocumentIdLength"], BasicFieldsValidatior.DocumentMinLength));
            }

            if (!BasicFieldsValidatior.IsStringValid(request.UserName))
            {
                errors.Add(string.Format(_localizer["InvalidUserNameLength"], BasicFieldsValidatior.StandardStringMaxLength));
            }

            if (!BasicFieldsValidatior.IsEmailValid(request.Email))
            {
                errors.Add(_localizer["InvalidEmailFormat"]);
            }

            if (!BasicFieldsValidatior.IsPasswordLengthValid(request.Password))
            {
                errors.Add(string.Format(_localizer["InvalidPasswordLength"],
                    BasicFieldsValidatior.PasswordMinLength,
                    BasicFieldsValidatior.PasswordMaxLength));
            }

            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest(_localizer["InvalidPasswordsMatch"]);
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
                PasswordHash = Crypto.HashPassword(request.Password)
            };

            try
            {
                _unitOfWork.UsersRepository.Insert(newUser);
                await _unitOfWork.SaveAsync();
            }
            catch (SqlException sqlex)
            {
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    new
                    {
                        Message = _localizer["DatabaseConnectionException"],
                        Errors = sqlex.Message
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = _localizer["InvalidUserCreation"],
                    Errors = ex.Message
                });
            }

            var emailConfirmationToken = UserManager<Users>.ConfirmEmailTokenPurpose;
            var tokenVerificationUrl = Url.Action(
                "VerifyEmail", "Account",
                new
                {
                    newUser.Id,
                    token = emailConfirmationToken
                },
                Request.Scheme);
            await _emailService.SendEmailAsync(request.Email, _localizer["EmailVerificationSubject"],
                string.Format(_localizer["EmailVerificationMessage"], tokenVerificationUrl));

            return Ok(string.Format(_localizer["RegistrationConfirmationMessage"], request.Email));
        }

        [HttpPost]
        [Route("forgot")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = _unitOfWork.UsersRepository.Get(x => x.Email == request.Email).FirstOrDefault();
            if (user == null)
            {
                return BadRequest(_localizer["ForgotPasswordCheckEmailMessage"]);
            }

            var passwordResetToken = UserManager<Users>.ResetPasswordTokenPurpose;
            var passwordResetUrl = Url.Action(
                "ResetPassword", "Account",
                new
                {
                    id = user.Id,
                    token = passwordResetToken
                },
                Request.Scheme);

            await _emailService.SendEmailAsync(request.Email, _localizer["ForgotPasswordSubject"],
                string.Format(_localizer["ForgotPasswordMessage"], passwordResetUrl));

            return Ok(_localizer["ForgotPasswordCheckEmailMessage"]);
        }

        [HttpPost]
        [Route("reset")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var user = _unitOfWork.UsersRepository.Get(x => x.Email == request.Id).FirstOrDefault(); ;
            if (user == null)
            {
                throw new InvalidOperationException();
            }

            if (request.Token != UserManager<Users>.ResetPasswordTokenPurpose)
            {
                return BadRequest("Token incorrect");
            }

            if (request.Password != request.RePassword)
            {
                return BadRequest(_localizer["InvalidPasswordsMatch"]);
            }

            var resetPasswordResult = _unitOfWork.UsersRepository.ResetPassword(user, request.Password);
            if (!resetPasswordResult)
            {
                return BadRequest(_localizer["InvalidPasswordReset"]);
            }

            try
            {
                _unitOfWork.UsersRepository.Update(user);
                await _unitOfWork.SaveAsync();
            }
            catch (SqlException sqlex)
            {
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    new
                    {
                        Message = _localizer["DatabaseConnectionException"],
                        Errors = sqlex.Message
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = _localizer["InvalidPasswordReset"],
                    Errors = ex.Message
                });
            }

            return Ok(_localizer["ResetPasswordSuccessfully"]);
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(_localizer["InvalidLoginCredentials"]);
            }

            var user = _unitOfWork.UsersRepository.Get(x => x.Email == request.Email).FirstOrDefault();
            if (user == null)
            {
                return BadRequest(_localizer["InvalidLoginCredentials"]);
            }

            if (!user.EmailConfirmed)
            {
                return BadRequest(_localizer["EmailNotVerifiedMessage"]);
            }

            if (!Crypto.VerifyHashedPassword(user.PasswordHash, request.Password))
            {
                return BadRequest(_localizer["InvalidLoginCredentials"]);
            }

            return Ok(new
            {
                user.Email,
                user.UserName,
                user.DocumentId,
                request.Password,
                Message = _localizer["LoginSuccessfully"]
            });
        }

        [HttpPost]
        [Route("logout")]
        public IActionResult Logout()
        {
            return Ok(_localizer["LogoutSuccessfully"]);
        }

        [HttpPost]
        [Route("confirm/email")]
        public IActionResult ConfirmEmail(string id, string token)
        {
            var user = _unitOfWork.UsersRepository.GetByID(id);
            if (user == null)
            {
                return BadRequest(_localizer["InvalidLoginCredentials"]);
            }

            if (UserManager<Users>.ConfirmEmailTokenPurpose != token)
            {
                return BadRequest("Token incorrect");
            }

            try
            {
                user.EmailConfirmed = true;
                _unitOfWork.UsersRepository.Update(user);
                _unitOfWork.Save();
            }
            catch (SqlException sqlex)
            {
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    new
                    {
                        Message = _localizer["DatabaseConnectionException"],
                        Errors = sqlex.Message
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = _localizer["InvalidUserCreation"],
                    Errors = ex.Message
                });
            }

            return Ok();
        }
    }
}