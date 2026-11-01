using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class DONHANG
{
    public int MaDH { get; set; }

    public string? MaKH { get; set; }

    public DateOnly NgayTao { get; set; }

    public string? Phuongthucthanhtoan { get; set; }

    public string? Trangthaigiaohang { get; set; }

    public virtual NGUOIDUNG? MaKHNavigation { get; set; }

    public virtual ICollection<CHITIETDONHANG> CHITIETDONHANGs { get; set; } = new List<CHITIETDONHANG>();

    public virtual ICollection<PHIEUXUAT> PHIEUXUATs { get; set; } = new List<PHIEUXUAT>();
}
