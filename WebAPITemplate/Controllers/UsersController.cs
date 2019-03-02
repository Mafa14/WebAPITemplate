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
using WebAPITemplate.Database;
using WebAPITemplate.Helpers.Validators;
using WebAPITemplate.RequestContracts;
using WebAPITemplate.RequestContracts.DataTable;
using WebAPITemplate.ResponseContracts;
using WebAPITemplate.ResponseContracts.DataTable;

namespace WebAPITemplate.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet]
        public IActionResult Get(string id)
        {
            var user = _unitOfWork.UsersRepository.GetByID(id);
            if (user == null)
            {
                return BadRequest(_localizer["InvalidUser"].Value);
            }

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.BirthDate,
                user.DocumentId,
                user.Address
            });
        }

        [HttpPost]
        [Route("all")]
        public IActionResult GetAll(DataTableRequest request)
        {
            var users = _unitOfWork.UsersRepository.Get();
            if (users == null)
            {
                return BadRequest(_localizer["InvalidUser"].Value);
            }

            var result = new List<UserListResponse>();

            foreach (var user in users)
            {
                result.Add(new UserListResponse()
                {
                    UserName = user.UserName,
                    DocumentId = user.DocumentId,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                });
            }

            return Ok(new DataTableResponse()
            {
                Draw = request.Draw,
                RecordsFiltered = users.Count(),
                RecordsTotal = users.Count(),
                Data = result.ToArray(),
                Error = string.Empty
            });
        }

        //[HttpPost]
        //[Route("all")]
        //public IActionResult GetAll()
        //{
        //    var users = _unitOfWork.UsersRepository.Get();
        //    if (users == null)
        //    {
        //        return BadRequest(_localizer["InvalidUser"].Value);
        //    }

        //    return Ok(users);
        //}

        [HttpPut]
        public async Task<IActionResult> Update(UserUpdateRequest request)
        {
            var errors = new List<string>();

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
            user.DocumentId = request.DocumentId;
            user.BirthDate = request.BirthDate;
            user.PhoneNumber = request.PhoneNumber;
            user.Address = request.Address;

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
                return BadRequest(_localizer["InvalidUserUpdate"].Value);
            }

            return Ok(_localizer["UserUpdateConfirmationMessage"].Value);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            var user = _unitOfWork.UsersRepository.GetByID(id);
            if (user == null)
            {
                return BadRequest(_localizer["InvalidUser"].Value);
            }

            try
            {
                _unitOfWork.UsersRepository.Delete(user);
                await _unitOfWork.SaveAsync();
            }
            catch (SqlException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["DatabaseConnectionException"].Value);
            }
            catch (Exception)
            {
                return BadRequest(_localizer["InvalidUserDelete"].Value);
            }

            return Ok(_localizer["UserDeleteConfirmationMessage"].Value);
        }
    }
}