using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class NHACUNGCAP
{
    public string MaNCC { get; set; } = null!;

    public string? TenNCC { get; set; }

    public string? Email { get; set; }

    public string? SDT { get; set; }

    public string? Diachi { get; set; }

    public virtual ICollection<PHIEUNHAP> PHIEUNHAPs { get; set; } = new List<PHIEUNHAP>();

    public virtual ICollection<SACH> SACHs { get; set; } = new List<SACH>();
}
