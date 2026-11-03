using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    public class User
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
    }
}