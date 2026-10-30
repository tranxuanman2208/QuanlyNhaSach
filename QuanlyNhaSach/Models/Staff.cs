using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    /// <summary>
    /// Employee (Staff) account model inheriting the unified User identity and authentication model.
    /// </summary>
    public class Staff : User
    {
        public Staff()
        {
            Role = UserRole.Staff;
            IsActive = true;
        }
    }
}
