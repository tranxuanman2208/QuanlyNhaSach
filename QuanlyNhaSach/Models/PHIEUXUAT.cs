using System;
using System.Collections.Generic;

namespace QuanlyNhaSach.Models;

public partial class PHIEUXUAT
{
    public int MaPX { get; set; }

    public int? MaDH { get; set; }

    public string? Ghichu { get; set; }

    public DateOnly NgayXuat { get; set; }

    public virtual DONHANG? MaDHNavigation { get; set; }
}
