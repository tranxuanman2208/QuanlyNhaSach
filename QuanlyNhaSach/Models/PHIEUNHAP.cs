using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class PHIEUNHAP
{
    public int MaPN { get; set; }

    public string? MaQly { get; set; }

    public string? MaNCC { get; set; }

    public DateOnly NgayNhap { get; set; }

    public virtual NHACUNGCAP? MaNCCNavigation { get; set; }

    public virtual NGUOIDUNG? MaQlyNavigation { get; set; }

    public virtual ICollection<CHITIETPHIEUNHAP> CHITIETPHIEUNHAPs { get; set; } = new List<CHITIETPHIEUNHAP>();
}
