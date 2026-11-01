using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class SACH
{
    public string MaSach { get; set; } = null!;

    public string? Tensach { get; set; }

    public string? Tacgia { get; set; }

    public string? NXB { get; set; }

    public decimal? Giaban { get; set; }

    public byte? Soluongton { get; set; }

    public string? Theloai { get; set; }

    public string? MaNCC { get; set; }

    public virtual NHACUNGCAP? MaNCCNavigation { get; set; }

    public virtual ICollection<CHITIETDONHANG> CHITIETDONHANGs { get; set; } = new List<CHITIETDONHANG>();

    public virtual ICollection<CHITIETGIOHANG> CHITIETGIOHANGs { get; set; } = new List<CHITIETGIOHANG>();

    public virtual ICollection<CHITIETPHIEUNHAP> CHITIETPHIEUNHAPs { get; set; } = new List<CHITIETPHIEUNHAP>();
}
