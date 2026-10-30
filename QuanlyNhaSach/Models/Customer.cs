using QuanlyNhaSach.Models.Enums;

namespace QuanlyNhaSach.Models
{
    /// <summary>
    /// Customer account model inheriting the unified User identity and authentication model.
    /// </summary>
    public class Customer : User
    {
        public Customer()
        {
            Role = UserRole.Customer;
            IsActive = true;
        }
    }
}
