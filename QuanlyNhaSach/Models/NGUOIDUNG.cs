using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class NGUOIDUNG
{
    public string MaND { get; set; } = null!;

    public string Tendangnhap { get; set; } = null!;

    public string Matkhau { get; set; } = null!;

    public string? Hoten { get; set; }

    public string? SDT { get; set; }

    public string? Email { get; set; }

    public string? Diachi { get; set; }

    public string? Vaitro { get; set; }

    public string? Trangthai { get; set; }

    public virtual ICollection<DONHANG> DONHANGs { get; set; } = new List<DONHANG>();
}
