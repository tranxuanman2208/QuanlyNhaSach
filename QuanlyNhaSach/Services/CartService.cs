using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QuanlyNhaSach.Models;

namespace QuanlyNhaSach.Services
{
    public partial class CartService : ObservableObject
    {
        private readonly MockDataService _mockDataService;

        public ObservableCollection<CartItem> Items { get; } = new();

        public int TotalQuantity => Items.Sum(i => i.SoLuongMua);
        public decimal TotalAmount => Items.Sum(i => i.ThanhTien);
        public string FormattedTotalAmount => $"{TotalAmount:N0} ₫";
        public bool IsEmpty => Items.Count == 0;

        public event EventHandler? CartChanged;

        public CartService(MockDataService mockDataService)
        {
            _mockDataService = mockDataService ?? throw new ArgumentNullException(nameof(mockDataService));
            Items.CollectionChanged += OnItemsCollectionChanged;
        }

        public OperationResult AddToCart(Book? book, int quantity = 1)
        {
            if (book == null)
            {
                return OperationResult.Failure("Không tìm thấy thông tin sách cần thêm.");
            }
            return AddToCart(book.MaSach, quantity);
        }

        public OperationResult AddToCart(int maSach, int quantity = 1)
        {
            if (quantity < 1)
            {
                return OperationResult.Failure("Số lượng thêm vào giỏ hàng phải lớn hơn hoặc bằng 1.");
            }

            var sharedBook = _mockDataService.GetBookById(maSach);
            if (sharedBook == null)
            {
                return OperationResult.Failure("Sách không tồn tại trong hệ thống.");
            }

            if (sharedBook.SoLuongTon <= 0)
            {
                return OperationResult.Failure($"Sách \"{sharedBook.TenSach}\" hiện đã hết hàng.");
            }

            var existingItem = Items.FirstOrDefault(i => i.Book.MaSach == maSach);
            int currentInCart = existingItem?.SoLuongMua ?? 0;
            int newTotalQuantity = currentInCart + quantity;

            if (newTotalQuantity > sharedBook.SoLuongTon)
            {
                return OperationResult.Failure(
                    $"Không đủ số lượng tồn kho cho sách \"{sharedBook.TenSach}\". " +
                    $"Tồn kho hiện tại: {sharedBook.SoLuongTon} cuốn (Trong giỏ đã có: {currentInCart} cuốn).");
            }

            if (existingItem != null)
            {
                existingItem.SetValidatedQuantity(newTotalQuantity);
            }
            else
            {
                var newItem = new CartItem(sharedBook, quantity);
                Items.Add(newItem);
            }

            NotifyTotalsChanged();
            return OperationResult.Success($"Đã thêm {quantity} cuốn \"{sharedBook.TenSach}\" vào giỏ hàng.");
        }

        public OperationResult IncreaseQuantity(CartItem? item)
        {
            if (item == null)
            {
                return OperationResult.Failure("Mục giỏ hàng không hợp lệ.");
            }
            return UpdateQuantity(item, item.SoLuongMua + 1);
        }

        public OperationResult DecreaseQuantity(CartItem? item)
        {
            if (item == null)
            {
                return OperationResult.Failure("Mục giỏ hàng không hợp lệ.");
            }

            if (item.SoLuongMua <= 1)
            {
                return OperationResult.Failure("Số lượng mua tối thiểu là 1. Nếu không muốn mua, vui lòng nhấn nút 'Xóa'.");
            }

            return UpdateQuantity(item, item.SoLuongMua - 1);
        }

        public OperationResult UpdateQuantity(CartItem? item, int newQuantity)
        {
            if (item == null || item.Book == null)
            {
                return OperationResult.Failure("Mục giỏ hàng không hợp lệ.");
            }

            if (newQuantity < 1)
            {
                item.SetValidatedQuantity(item.SoLuongMua);
                return OperationResult.Failure("Số lượng mua không được nhỏ hơn 1.");
            }

            var sharedBook = _mockDataService.GetBookById(item.Book.MaSach);
            if (sharedBook == null)
            {
                return OperationResult.Failure("Sách không còn tồn tại trong danh mục.");
            }

            if (newQuantity > sharedBook.SoLuongTon)
            {
                item.SetValidatedQuantity(item.SoLuongMua);
                return OperationResult.Failure(
                    $"Số lượng yêu cầu ({newQuantity}) vượt quá số lượng tồn kho hiện tại ({sharedBook.SoLuongTon} cuốn) của sách \"{sharedBook.TenSach}\".");
            }

            item.SetValidatedQuantity(newQuantity);
            NotifyTotalsChanged();
            return OperationResult.Success();
        }

        public OperationResult RemoveItem(CartItem? item)
        {
            if (item == null)
            {
                return OperationResult.Failure("Không tìm thấy sản phẩm cần xóa.");
            }

            if (Items.Remove(item))
            {
                NotifyTotalsChanged();
                return OperationResult.Success($"Đã xóa \"{item.Book.TenSach}\" khỏi giỏ hàng.");
            }

            return OperationResult.Failure("Sản phẩm không có trong giỏ hàng.");
        }

        public void ClearCart()
        {
            Items.Clear();
            NotifyTotalsChanged();
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (CartItem oldItem in e.OldItems)
                {
                    PropertyChangedEventManager.RemoveHandler(oldItem, OnCartItemPropertyChanged, string.Empty);
                }
            }

            if (e.NewItems != null)
            {
                foreach (CartItem newItem in e.NewItems)
                {
                    PropertyChangedEventManager.AddHandler(newItem, OnCartItemPropertyChanged, string.Empty);
                }
            }

            NotifyTotalsChanged();
        }

        private void OnCartItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.SoLuongMua) ||
                e.PropertyName == nameof(CartItem.ThanhTien) ||
                string.IsNullOrEmpty(e.PropertyName))
            {
                NotifyTotalsChanged();
            }
        }

        public void NotifyTotalsChanged()
        {
            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(FormattedTotalAmount));
            OnPropertyChanged(nameof(IsEmpty));
            CartChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
