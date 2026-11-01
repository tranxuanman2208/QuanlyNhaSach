using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QuanlyNhaSach.Models;

public partial class QuanLyNhaSachContext : DbContext
{
    public QuanLyNhaSachContext()
    {
    }

    public QuanLyNhaSachContext(DbContextOptions<QuanLyNhaSachContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CHITIETDONHANG> CHITIETDONHANGs { get; set; }

    public virtual DbSet<CHITIETGIOHANG> CHITIETGIOHANGs { get; set; }

    public virtual DbSet<CHITIETPHIEUNHAP> CHITIETPHIEUNHAPs { get; set; }

    public virtual DbSet<DONHANG> DONHANGs { get; set; }

    public virtual DbSet<GIOHANG> GIOHANGs { get; set; }

    public virtual DbSet<NGUOIDUNG> NGUOIDUNGs { get; set; }

    public virtual DbSet<NHACUNGCAP> NHACUNGCAPs { get; set; }

    public virtual DbSet<PHIEUNHAP> PHIEUNHAPs { get; set; }

    public virtual DbSet<PHIEUXUAT> PHIEUXUATs { get; set; }

    public virtual DbSet<SACH> SACHs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=QuanlyNhaSach;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CHITIETDONHANG>(entity =>
        {
            entity.HasKey(e => new { e.MaDH, e.MaSach }).HasName("pk_MaDH_MaSach");

            entity.ToTable("CHITIETDONHANG");

            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Tong).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.MaDHNavigation).WithMany(p => p.CHITIETDONHANGs)
                .HasForeignKey(d => d.MaDH)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaDH_DH");

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.CHITIETDONHANGs)
                .HasForeignKey(d => d.MaSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaSach_DH");
        });

        modelBuilder.Entity<CHITIETGIOHANG>(entity =>
        {
            entity.HasKey(e => new { e.MaGH, e.MaSach }).HasName("pk_MaGH_MaSach");

            entity.ToTable("CHITIETGIOHANG");

            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Tong).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.MaGHNavigation).WithMany(p => p.CHITIETGIOHANGs)
                .HasForeignKey(d => d.MaGH)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaGH_GH");

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.CHITIETGIOHANGs)
                .HasForeignKey(d => d.MaSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaSach_GH");
        });

        modelBuilder.Entity<CHITIETPHIEUNHAP>(entity =>
        {
            entity.HasKey(e => new { e.MaPN, e.MaSach }).HasName("pk_MaSach_MaPN");

            entity.ToTable("CHITIETPHIEUNHAP");

            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Tong).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.MaPNNavigation).WithMany(p => p.CHITIETPHIEUNHAPs)
                .HasForeignKey(d => d.MaPN)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaPN");

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.CHITIETPHIEUNHAPs)
                .HasForeignKey(d => d.MaSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_MaSach_PN");
        });

        modelBuilder.Entity<DONHANG>(entity =>
        {
            entity.HasKey(e => e.MaDH).HasName("PK__DONHANG__27258661");

            entity.ToTable("DONHANG");

            entity.Property(e => e.MaKH)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.Phuongthucthanhtoan)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Trangthaigiaohang).HasMaxLength(100);

            entity.HasOne(d => d.MaKHNavigation).WithMany(p => p.DONHANGs)
                .HasForeignKey(d => d.MaKH)
                .HasConstraintName("fk_MaKH_DH");
        });

        modelBuilder.Entity<GIOHANG>(entity =>
        {
            entity.HasKey(e => e.MaGH).HasName("PK__GIOHANG__2725AE85");

            entity.ToTable("GIOHANG");

            entity.HasIndex(e => e.MaKH, "UQ__GIOHANG__2725CF1F8B45322B").IsUnique();

            entity.Property(e => e.MaKH)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.MaKHNavigation).WithMany(p => p.GIOHANGs)
                .HasForeignKey(d => d.MaKH)
                .HasConstraintName("fk_MaKH_GH");
        });

        modelBuilder.Entity<NGUOIDUNG>(entity =>
        {
            entity.HasKey(e => e.MaND).HasName("PK__NGUOIDUN__2725D724");

            entity.ToTable("NGUOIDUNG");

            entity.Property(e => e.MaND)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Diachi).HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Hoten).HasMaxLength(100);
            entity.Property(e => e.Matkhau)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.SDT)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Tendangnhap)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Trangthai).HasMaxLength(50);
            entity.Property(e => e.Vaitro).HasMaxLength(50);
        });

        modelBuilder.Entity<NHACUNGCAP>(entity =>
        {
            entity.HasKey(e => e.MaNCC).HasName("PK_MANCC");

            entity.ToTable("NHACUNGCAP");

            entity.Property(e => e.MaNCC)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Diachi).HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SDT)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenNCC).HasMaxLength(100);
        });

        modelBuilder.Entity<PHIEUNHAP>(entity =>
        {
            entity.HasKey(e => e.MaPN).HasName("PK__PHIEUNHA__2725E7F4");

            entity.ToTable("PHIEUNHAP");

            entity.Property(e => e.MaNCC)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaQly)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayNhap).HasDefaultValueSql("(CONVERT([date],getdate()))");

            entity.HasOne(d => d.MaNCCNavigation).WithMany(p => p.PHIEUNHAPs)
                .HasForeignKey(d => d.MaNCC)
                .HasConstraintName("fk_MaNCC_PN");

            entity.HasOne(d => d.MaQlyNavigation).WithMany(p => p.PHIEUNHAPs)
                .HasForeignKey(d => d.MaQly)
                .HasConstraintName("FK_MAQLY_PN");
        });

        modelBuilder.Entity<PHIEUXUAT>(entity =>
        {
            entity.HasKey(e => e.MaPX).HasName("PK__PHIEUXUA__2725E7F2");

            entity.ToTable("PHIEUXUAT");

            entity.Property(e => e.Ghichu).HasMaxLength(200);
            entity.Property(e => e.NgayXuat).HasDefaultValueSql("(CONVERT([date],getdate()))");

            entity.HasOne(d => d.MaDHNavigation).WithMany(p => p.PHIEUXUATs)
                .HasForeignKey(d => d.MaDH)
                .HasConstraintName("FK_MaDH");
        });

        modelBuilder.Entity<SACH>(entity =>
        {
            entity.HasKey(e => e.MaSach).HasName("PK__SACH__B235742D");

            entity.ToTable("SACH");

            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Giaban).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MaNCC)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NXB).HasMaxLength(100);
            entity.Property(e => e.Tacgia).HasMaxLength(100);
            entity.Property(e => e.Tensach).HasMaxLength(100);
            entity.Property(e => e.Theloai).HasMaxLength(100);

            entity.HasOne(d => d.MaNCCNavigation).WithMany(p => p.SACHs)
                .HasForeignKey(d => d.MaNCC)
                .HasConstraintName("fk_MaNCC");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
