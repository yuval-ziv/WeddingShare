namespace WeddingShare.Models
{
    public class LoginModel
    {
        public LoginModel()
        {
            this.Username = string.Empty;
            this.Password = string.Empty;
        }

        public string Username { get; set; }
        public string Password { get; set; }
    }
}