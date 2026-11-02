using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class CHITIETDONHANG
{
    public int MaDH { get; set; }

    public string MaSach { get; set; } = null!;

    public byte? Soluong { get; set; }

    public decimal? Tong { get; set; }

    public virtual DONHANG MaDHNavigation { get; set; } = null!;

    public virtual SACH MaSachNavigation { get; set; } = null!;
}
