using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class GIOHANG
{
    public int MaGH { get; set; }

    public string? MaKH { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual NGUOIDUNG? MaKHNavigation { get; set; }

    public virtual ICollection<CHITIETGIOHANG> CHITIETGIOHANGs { get; set; } = new List<CHITIETGIOHANG>();
}
