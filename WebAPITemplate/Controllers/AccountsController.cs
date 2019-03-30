using CryptoHelper;
using Microsoft.AspNetCore.Authorization;
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
using WebAPITemplate.Helpers;
using WebAPITemplate.Helpers.Token;
using WebAPITemplate.Helpers.Validators;
using WebAPITemplate.RequestContracts;

namespace WebAPITemplate.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
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
        [AllowAnonymous]
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
                Email = request.Email,
                PasswordHash = Crypto.HashPassword(request.Password),
                EmailConfirmed = true // TODO: DELETE THIS AFTER DEPLOYING THE EMAILING SERVICE
            };

            try
            {
                _unitOfWork.UsersRepository.Insert(newUser);
                await _unitOfWork.SaveAsync();

                var roles = _unitOfWork.RolesRepository.Get();

                _unitOfWork.UserRolesRepository.Insert(new UserRoles
                {
                    RoleId = roles.First(r => r.Name == SystemRoles.Client.ToString()).Id,
                    UserId = newUser.Id
                });

                var emailToken = Guid.NewGuid().ToString();

                _unitOfWork.UserTokensRepository.Insert(new UserTokens
                {
                    LoginProvider = Globals.EmailLoginProvider,
                    Name = Globals.EmailTokenName,
                    UserId = newUser.Id,
                    Value = emailToken
                });

                await _unitOfWork.SaveAsync();

                var tokenVerificationUrl = HttpUtility.UrlDecode(request.ConfirmationUrl)
                    .Replace("#Id", newUser.Id)
                    .Replace("#Token", emailToken);
                await _emailService.SendEmailAsync(request.Email, _localizer["EmailVerificationSubject"].Value,
                    string.Format(_localizer["EmailVerificationMessage"].Value, tokenVerificationUrl));
            }
            catch (SqlException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["DatabaseConnectionException"].Value);
            }
            catch (Exception)
            {
                return BadRequest(_localizer["InvalidUserCreation"].Value);
            }

            return Ok(string.Format(_localizer["RegistrationConfirmationMessage"].Value, request.Email));
        }

        [HttpPost]
        [AllowAnonymous]
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

            try
            {
                var userTokens = _unitOfWork.UserTokensRepository.Get(ut => ut.UserId == user.Id && ut.Name == Globals.ResetPasswordTokenName);
                string resetToken;

                if (userTokens == null)
                {
                    resetToken = Guid.NewGuid().ToString();

                    _unitOfWork.UserTokensRepository.Insert(new UserTokens
                    {
                        LoginProvider = Globals.ResetPasswordLoginProvider,
                        Name = Globals.ResetPasswordTokenName,
                        UserId = user.Id,
                        Value = resetToken
                    });

                    await _unitOfWork.SaveAsync();
                }
                else
                {
                    resetToken = userTokens.First().Value;
                }

                var tokenVerificationUrl = HttpUtility.UrlDecode(request.ConfirmationUrl)
                    .Replace("#Id", user.Id)
                    .Replace("#Token", resetToken);

                await _emailService.SendEmailAsync(request.Email, _localizer["ForgotPasswordSubject"].Value,
                    string.Format(_localizer["ForgotPasswordMessage"].Value, tokenVerificationUrl));
            }
            catch (SqlException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["DatabaseConnectionException"].Value);
            }
            catch (Exception)
            {
                return BadRequest(_localizer["InvalidPasswordReset"].Value);
            }

            return Ok(_localizer["ForgotPasswordCheckEmailMessage"].Value);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("reset")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var users = _unitOfWork.UsersRepository.Get(u => u.Id == request.Id, includeProperties: u => u.UserTokens);
            Users currentUser;

            if (users == null || users.Count() == 0)
            {
                throw new InvalidOperationException();
            }
            else
            {
                currentUser = users.First();
            }

            if (!currentUser.UserTokens.Any(ut => ut.Name == request.Token))
            {
                return BadRequest(_localizer["MissingHiddenDataMessage"].Value);
            }

            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest(_localizer["InvalidPasswordsMatch"].Value);
            }

            var resetPasswordResult = _unitOfWork.UsersRepository.ResetPassword(currentUser, request.Password);
            if (!resetPasswordResult)
            {
                return BadRequest(_localizer["InvalidPasswordReset"].Value);
            }

            try
            {
                _unitOfWork.UsersRepository.Update(currentUser);
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
        [AllowAnonymous]
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

            object generatedToken = null;
            try
            {
                generatedToken = TokenHandler.GenerateToken(_localizer, _unitOfWork, user, request.Password);
            }
            catch (MissingFieldException mfe)
            {
                return BadRequest(mfe.Message);
            }
            catch (InvalidCastException ice)
            {
                return BadRequest(ice.Message);
            }

            var userRoles = _unitOfWork.UserRolesRepository.Get(ur => ur.UserId == user.Id, includeProperties: r => r.Role);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.DocumentId,
                token = generatedToken,
                roles = userRoles != null && userRoles.Select(ur => ur.Role).Any() ? userRoles.Select(ur => ur.Role.Name) : new List<string>(),
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
        [AllowAnonymous]
        [Route("confirm/email")]
        public IActionResult ConfirmEmail(string id, string token)
        {
            var users = _unitOfWork.UsersRepository.Get(u => u.Id == id, includeProperties: u => u.UserTokens);
            Users currentUser;

            if (users == null)
            {
                return BadRequest(_localizer["InvalidLoginCredentials"].Value);
            }
            else
            {
                currentUser = users.First();
            }

            if (!currentUser.UserTokens.Any(ut => ut.Name == token))
            {
                return BadRequest(_localizer["MissingHiddenDataMessage"].Value);
            }

            if (currentUser.EmailConfirmed)
            {
                return Ok();
            }

            try
            {
                currentUser.EmailConfirmed = true;
                _unitOfWork.UsersRepository.Update(currentUser);
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