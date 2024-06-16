namespace Camping
{
    public class UserUpdateRequest
    {
        public string CurrentUsername { get; set; }
        public string NewEmail { get; set; }
        public string CurrentPassword { get; set; }

        public string NewPassword { get; set; } 
        public string NewUsername { get; set; }
    }

}
