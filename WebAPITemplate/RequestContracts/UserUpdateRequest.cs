using System;

namespace WebAPITemplate.RequestContracts
{
    public class UserUpdateRequest
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public string DocumentId { get; set; }
        public string Address { get; set; }
        public string ConfirmationUrl { get; set; }
    }
}
