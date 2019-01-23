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
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailService;
        private readonly IStringLocalizer<AccountsController> _localizer;

        public UsersController(IUnitOfWork unitOfWork,
            IEmailSender emailService,
            IStringLocalizer<AccountsController> localizer)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _localizer = localizer;
        }

        [HttpPut]
        public async Task<IActionResult> Update(UserRequest request)
        {
            var errors = new List<string>();
            var emailChanged = false;

            var user = _unitOfWork.UsersRepository.GetByID(request.Id);
            if (user == null)
            {
                return BadRequest(_localizer["InvalidUser"].Value);
            }

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
            else
            {
                emailChanged = true;
                user.EmailConfirmed = false;
            }
            // TODO: Adjust after the UI modification
            if (!BasicFieldsValidator.IsStringValid(request.Address))
            {
                errors.Add(string.Format(_localizer["InvalidUserNameLength"].Value, BasicFieldsValidator.StandardStringMaxLength));
            }

            if (!BasicFieldsValidator.IsStringValid(request.PhoneNumber))
            {
                errors.Add(string.Format(_localizer["InvalidUserNameLength"].Value, BasicFieldsValidator.StandardStringMaxLength));
            }

            if (request.BirthDate.Year < 1900 && request.BirthDate.Year > DateTime.Today.AddYears(1).Year)
            {
                errors.Add(string.Format(_localizer["InvalidBirthDateRange"].Value, 1900, DateTime.Today.AddYears(1).Year));
            }

            if (errors.Any())
            {
                return BadRequest(string.Join(Environment.NewLine, errors.Select(e => "- " + e).ToArray()));
            }

            user.UserName = request.UserName;
            user.Email = request.Email;
            user.DocumentId = request.DocumentId;
            user.BirthDate = request.BirthDate;
            user.PhoneNumber = request.PhoneNumber;
            user.Address = request.Address;

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
                        Message = _localizer["DatabaseConnectionException"].Value,
                        Errors = sqlex.Message
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = _localizer["InvalidUserUpdate"].Value,
                    Errors = ex.Message
                });
            }

            if (emailChanged)
            {
                var tokenVerificationUrl = HttpUtility.UrlDecode(request.ConfirmationUrl)
               .Replace("#Id", user.Id)
               .Replace("#Token", UserManager<Users>.ConfirmEmailTokenPurpose);
                await _emailService.SendEmailAsync(request.Email, _localizer["EmailVerificationSubject"].Value,
                    string.Format(_localizer["EmailVerificationMessage"].Value, tokenVerificationUrl));
            }

            return Ok(string.Format(_localizer[emailChanged ? "UserUpdateConfirmationMessageWithEmail"
                : "UserUpdateConfirmationMessage"].Value, request.Email));
        }
    }
}