using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class CHITIETGIOHANG
{
    public int MaGH { get; set; }

    public string MaSach { get; set; } = null!;

    public byte? Soluong { get; set; }

    public decimal? Tong { get; set; }

    public virtual GIOHANG MaGHNavigation { get; set; } = null!;

    public virtual SACH MaSachNavigation { get; set; } = null!;
}
