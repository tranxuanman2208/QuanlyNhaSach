using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class CHITIETPHIEUNHAP
{
    public int MaPN { get; set; }

    public string MaSach { get; set; } = null!;

    public byte? Soluongnhap { get; set; }

    public decimal? Tong { get; set; }

    public virtual PHIEUNHAP MaPNNavigation { get; set; } = null!;

    public virtual SACH MaSachNavigation { get; set; } = null!;
}
