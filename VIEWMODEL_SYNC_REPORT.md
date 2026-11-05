# ViewModel Synchronization Report — QuanlyNhaSach

Branch: `ViewmodlesFix` · Date: 2026-11-06 · SDK: .NET SDK 10.0.402 (project targets `net8.0-windows`)

## 1. Executive Summary

The goal was to align the Presentation Layer (ViewModels and XAML bindings) with the real EF Core entities scaffolded from SQL Server, now that the Service Layer no longer uses mock data.

**Main finding:** the C# side was already consistent. The baseline build had **0 errors**, and every ViewModel already used the real entity types, property names, and Service signatures. That is expected, because the C# compiler would have caught any mismatch there. The remaining risk was in **XAML bindings**, which the compiler does not check. A wrong binding path fails silently at runtime and leaves a cell or field blank.

To find those, I wrote a scope-aware binding resolver. It maps each View to its ViewModel (from the code-behind constructor), resolves every `{Binding}` against the actual DataContext type, and follows dotted paths through property types. The DataContext types it handles are:
- the page ViewModel
- the element type of an `ItemsSource` collection, for DataGrid columns, cell templates, row styles, and item templates
- `DataContext.X` reached through `RelativeSource AncestorType=Page`
- an explicit `DataContext="{Binding …}"`

I ran it over all 20 Views and `Resources/Templates.xaml`.

**Result:** 8 broken bindings and 1 Mock-Data leftover binding were found and fixed. Final build: **Build succeeded — 0 Errors, 2 unique warnings** (baseline had 3). The resolver now reports **0 unresolved bindings**.

## 2. Real Entity Inventory

All entities are in namespace `QuanlyNhaSach.Models` and live in `QuanlyNhaSach/Models/`. Columns are listed exactly as declared in the scaffold. Extended (`[NotMapped]`) members come from `*.Extended.cs`.

| Entity | Scaffolded (mapped) properties | Navigations | Key `[NotMapped]` presentation members |
|---|---|---|---|
| `SACH` | `MaSach` (string, PK), `Tensach` string?, `Tacgia` string?, `NXB` string?, `Giaban` decimal?, `Soluongton` byte?, `Theloai` string?, `MaNCC` string?, `Daxoa` bool? | `MaNCCNavigation`, `CHITIETDONHANGs`, `CHITIETGIOHANGs`, `CHITIETPHIEUNHAPs` | `MauBia`, `AnhBia`, `MoTa`, `IsInStock`, `IsOutOfStock`, `FormattedPrice`, `StockStatusText`, `AdminStockStatusText` |
| `NHACUNGCAP` | `MaNCC` (string, PK), `TenNCC`, `Email`, `SDT`, `Diachi` (all string?) | `PHIEUNHAPs`, `SACHs` | `SupplierCode`, `DiaChi` (wraps `Diachi`), `IsActive`, `StatusText`, `ToggleStatusActionText`, `DisplayName` |
| `DONHANG` | `MaDH` (int, PK), `MaKH` string?, `NgayTao` DateOnly, `Phuongthucthanhtoan` string?, `Trangthaigiaohang` string?, `Dathanhtoan` bool?, `Tongtien` decimal? | `MaKHNavigation`, `CHITIETDONHANGs`, `PHIEUXUATs` | `OrderCode`, `HoTenKhachHang`, `SDTKhachHang`, `DiaChiGiaoHang`, `NgayDat` DateTime, `TongTien` decimal, `Status`, `PaymentStatus`, `StockOutStatus`, `Items`, `TrangThai*`, `Formatted*`, `CanRecordPayment`, `CanTransitionStatus`, `RequiredActionText` |
| `CHITIETDONHANG` | `MaDH` int, `MaSach` string (composite PK), `Soluong` byte?, `Tong` decimal? | `MaDHNavigation`, `MaSachNavigation` | `TenSach`, `TacGia`, `SoLuongMua`, `DonGia`, `ThanhTien`, `FormattedDonGia`, `FormattedThanhTien` |
| `PHIEUNHAP` | `MaPN` (int, PK), `MaQly` string?, `MaNCC` string?, `NgayNhap` DateOnly, `Dathanhtoan` bool?, `Tongtien` decimal? | `MaNCCNavigation`, `MaQlyNavigation`, `CHITIETPHIEUNHAPs` | `ReceiptCode`, `TenNCC`, `SdtNCC`, `DiaChiNCC`, `NgayLap`, `TongTien`, `TrangThai`, `NguoiLapHoTen`, `Details`, `BooksSummary`, `FormattedTongTien`, `FormattedNgayLap`, `TrangThaiLabel` |
| `CHITIETPHIEUNHAP` | `MaPN` int, `MaSach` string, `Soluongnhap` byte?, `Tong` decimal? | `MaPNNavigation`, `MaSachNavigation` | `BookCode`, `TenSach`, `TacGia`, `CurrentStock`, `SoLuongNhap`, `DonGiaNhap`, `ThanhTien`, `Formatted*` |
| `NGUOIDUNG` | `MaND` (string, PK), `Tendangnhap` string, `Matkhau` string, `Hoten` string?, `SDT` string?, `Email` string?, `Diachi` string?, `Vaitro` string?, `Trangthai` string? | `DONHANGs`, `GIOHANGs`, `PHIEUNHAPs` | `UserCode`, `Role`, `IsActive`, `RoleDisplayName`, `AccountStatusLabel`, `ToggleLockActionText`, `Initials` |
| `GIOHANG` | `MaGH` int, `MaKH` string?, `NgayCapNhat` DateTime? | `MaKHNavigation`, `CHITIETGIOHANGs` | — |
| `CHITIETGIOHANG` | `MaGH` int, `MaSach` string, `Soluong` byte?, `Tong` decimal? | `MaGHNavigation`, `MaSachNavigation` | — |
| `PHIEUXUAT` | `MaPX` int, `MaDH` int?, `Ghichu` string?, `NgayXuat` DateOnly | `MaDHNavigation` | — |

Non-entity presentation models used by ViewModels: `CartItem`, `StockOutLineItem`, `DashboardStatistics`, `RevenueChartPoint`, `BestSellingBookItem`, `OperationResult<T>`.

**Naming caveat:** the scaffold uses lower-case second words on several columns (`Tensach`, `Tacgia`, `Giaban`, `Soluongton`, `Hoten`, `Diachi`, `Tendangnhap`). Some extended classes deliberately expose PascalCase aliases instead (`CHITIETDONHANG.TenSach`, `NHACUNGCAP.DiaChi`, `DONHANG.TongTien`). As a result, `TenSach` is valid against an order line but **not** against `SACH`, and `DiaChi` is valid against a supplier but **not** against `NGUOIDUNG`. Every broken binding found in this work came from this asymmetry.

## 3. ViewModels Updated

| ViewModel | Changes Made | Status |
|---|---|---|
| `ViewModels/Customer/CartCheckoutViewModel.cs` | Added the `EmailError` / `HasEmailError` properties that `CartCheckoutPage.xaml` already bound to, plus email validation in `UpdateFieldErrors`, `CanConfirmCheckout`, the submit guard, and `ResetFormFields`. The rule mirrors `OrderService.CreateCustomerOrder`: non-blank, ends with `@gmail.com`, has a local part, and contains no whitespace. | Completed |

I inspected all other ViewModels (18 files under `ViewModels/`). They already matched the entities and Service contracts, so they were left unchanged.

## 4. Property and Data Type Changes

| Location | Previous Reference | Correct Reference | Reason |
|---|---|---|---|
| CustomerManagementPage / StaffManagementPage (row type `NGUOIDUNG`) | `HoTen` | `Hoten` | Case-sensitive mismatch with the scaffolded column |
| same | `Username` | `Tendangnhap` | No such member exists; the login name column is `Tendangnhap` |
| same | `DiaChi` | `Diachi` | `NGUOIDUNG` has no `DiaChi` alias (only `NHACUNGCAP` has one) |
| CartCheckoutPage (`CartCheckoutViewModel`) | `EmailError`, `HasEmailError` (missing) | Implemented as `[ObservableProperty]` plus a computed bool | The XAML bound to members that did not exist |
| CustomerProfilePage order grid (row type `DONHANG`) | `MaDH` → `CustomerOrderTotalConverter` | `TongTien` (decimal) → same converter | Leftover from mock data. Passing the ID made the converter call `GetMockOrderTotal(maDh)`, which runs a DB query per uncached row. The real, already-loaded `decimal` total is now bound directly. |

Data types: currency is `decimal`/`decimal?` everywhere; no ViewModel uses `double` for money. Dates stay `DateOnly`/`DateTime` and are formatted only through converters or `Formatted*` properties. Stock (`byte?`) is accepted by `StockBadge*Converter`, and `DateOnly` is accepted by `CustomerOrderDateConverter`.

## 5. XAML Files Modified

- `Views/Manager/CustomerManagementPage.xaml`: full name (cell template), username, and address columns.
- `Views/Manager/StaffManagementPage.xaml`: same three columns.
- `Views/Customer/CustomerProfilePage.xaml`: order total column now binds `TongTien`.
- `Views/Customer/CartCheckoutPage.xaml`: **not modified**. Its `EmailError` / `HasEmailError` bindings were correct; the ViewModel was missing the members.

No ResourceDictionary needed changes. `Templates.xaml` (DataTypes `SACH` and `DONHANG`) already used the real names (`Tensach`, `Tacgia`, `Soluongton`, `Trangthaigiaohang`).

## 6. Service Contract Synchronization

The Services are concrete classes registered without interfaces (`BookService`, `SupplierService`, `OrderService`, `ImportService`, `InventoryService`, `UserManagementService`, `CartService`, `AuthService`). All are **synchronous** and return EF entities, `IReadOnlyList<T>`, `OperationResult` / `OperationResult<T>`, or presentation DTOs (`DashboardStatistics`, `RevenueChartPoint`, `BestSellingBookItem`, `StockOutLineItem`).

Every ViewModel collection matches its Service return element type, for example:
- `ObservableCollection<SACH>` ← `BookService.SearchAndFilterBooks`
- `ObservableCollection<NGUOIDUNG>` ← `UserManagementService.SearchAccounts`
- `ObservableCollection<StockOutLineItem>` ← `InventoryService.GetStockOutLineItems`
- `ObservableCollection<BestSellingBookItem>` ← `OrderService.GetTopSellingBooks`

No Service was changed. No ViewModel blocks the UI thread with `.Result` or `.Wait()`. Because the Services are synchronous, no async conversion was required.

## 7. Build Results

Command: `dotnet restore QuanlyNhaSach.sln` then `dotnet build QuanlyNhaSach.sln` (a single project: `QuanlyNhaSach/QuanlyNhaSach.csproj`).

| | Errors | Unique warnings |
|---|---|---|
| Baseline (before changes) | 0 | 3 — `CS8604` AuthService.cs:189, `CS8602` AuthService.cs:278, `CS0414` CartCheckoutViewModel.cs:29 |
| After batch 1 (account grids) | 0 | 3 |
| Final | **0** | **2** — the two pre-existing `AuthService` nullable warnings. `CS0414` was resolved because `_hasUserTouchedEmail` is now used. |

MSBuild prints each warning twice because WPF compiles a temporary `*_wpftmp.csproj`, so the raw counts are 6 → 4.

**Build succeeded — 0 Errors.**

## 8. Runtime Binding Verification

| Check | Status |
|---|---|
| Static XAML inspection (scope-aware resolver over all Views and Templates.xaml) | **Done**: 0 unresolved paths after the fixes |
| Command bindings (`*Command`, including `DataContext.*Command` via RelativeSource) | **Done statically**: all resolve to `[RelayCommand]`-generated commands on the owning ViewModel |
| GUI launch, DataGrid population, command execution | **Not performed.** The app needs a live SQL Server and interactive navigation through every role (customer, staff, manager). This remains outstanding. |
| WPF binding diagnostics in Debug output (`System.Windows.Data Error: 40`) | **Not performed.** Recommended: run under the Visual Studio debugger, open each page, and check the Output window. |

Limits of the static resolver: it does not evaluate converters, `MultiBinding`, or `ElementName` bindings, and it treats `ComboBox` item templates over `string` collections as untyped. None of the views relies on these for entity data.

## 9. Files Created or Modified

- Modified `QuanlyNhaSach/ViewModels/Customer/CartCheckoutViewModel.cs`
- Modified `QuanlyNhaSach/Views/Manager/CustomerManagementPage.xaml`
- Modified `QuanlyNhaSach/Views/Manager/StaffManagementPage.xaml`
- Modified `QuanlyNhaSach/Views/Customer/CustomerProfilePage.xaml`
- Created `VIEWMODEL_SYNC_REPORT.md`

Nothing was committed or pushed. No schema change, no migration, and no scaffold regeneration.

## 10. Remaining Issues

1. **`CustomerProfileViewModel` accesses the database directly** (`new QuanlyNhaSachContext()` at three sites: profile/order load, `GetMockOrderTotal`, and `UpdateProfile`). This breaks the MVVM / Service-layer rule. It is the only ViewModel that does this. Moving the logic into a Service is a refactor beyond binding synchronization, so it was left unchanged.
2. **Possible privacy bug in the same ViewModel:** when the signed-in customer has no orders, `LoadCustomerAndOrdersFromDatabase` falls back to showing the 10 most recent orders of *all* customers. It also substitutes hard-coded demo data (`KH001`, "Nguyễn Minh Anh") when no session user is found. These are business-logic issues and were not changed.
3. `CustomerProfileViewModel.GetMockOrderTotal` is misnamed: it reads real totals from the DB. It is now only reached through the converter's fallback branches.
4. Server-side email validation in `OrderService.CreateCustomerOrder` accepts the bare string `@gmail.com` (length ≥ 10) and does not trim the input. The new client-side rule is stricter, so valid submissions are unaffected.
5. Two pre-existing nullable warnings in `AuthService.cs` (lines 189 and 278) remain. They are unrelated to this work.
6. Runtime/GUI verification is still outstanding (see §8).
