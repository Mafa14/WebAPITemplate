namespace WebAPITemplate.RequestContracts
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
        public string ConfirmationUrl { get; set; }
    }
}
