using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    public class Manager : User
    {
        public Manager()
        {
            Role = UserRole.Manager;
            IsActive = true;
        }
    }
}