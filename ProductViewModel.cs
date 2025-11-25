using SimpleInventoryApp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;

namespace SimpleInventoryApp
{
    public class ProductViewModel : INotifyPropertyChanged
    {
        private string productName;
        private string productCategory;
        private string productQuantity;
        private string productSearchQuery;
        private Product selectedProduct;
        private ICollectionView productView;

        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<Product> FilteredProducts { get; set; }

        public string ProductName
        {
            get => productName;
            set  { productName = value; OnPropertyChanged(nameof(productName)); }
        }

        public string ProductCategory
        {
            get => productCategory;
            set { productCategory = value; OnPropertyChanged(nameof(productCategory)); }
        }

        public string ProductQuantity
        {
            get => productQuantity;
            set { productQuantity = value; OnPropertyChanged(nameof(productQuantity)); }
        }

        public string ProductSearchQuery
        {
            get => productSearchQuery;
            set { productSearchQuery = value; OnPropertyChanged(nameof(productSearchQuery)); FilterProducts();  }
        }

        public Product SelectedProduct
        {
            get => selectedProduct;
            set
            {
                selectedProduct = value;
                if (selectedProduct != null)
                {
                    productName = selectedProduct.ProductName;
                    productCategory = selectedProduct.ProductCategory;
                    productQuantity = selectedProduct.ProductQuantity.ToString();
                }
                OnPropertyChanged(nameof(selectedProduct));
            }
        }

        public ICollectionView ProductView
        {
            get => productView;
            set { productView = value; OnPropertyChanged(nameof(productView)); }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ICommand ClearSearchCommand {  get; }

        public ProductViewModel()
        {
            Products = new ObservableCollection<Product>();
            FilteredProducts = new ObservableCollection<Product>();
            AddCommand = new DelegateCmd(_ => AddItem());
            EditCommand = new DelegateCmd(_ => EditItem(), _ => SelectedProduct != null);
            DeleteCommand = new DelegateCmd(_ => DeleteItem(), _ => SelectedProduct != null);
            ClearSearchCommand = new DelegateCmd(_ => ClearSearchQuery());
            ProductView = CollectionViewSource.GetDefaultView(Products);
            ProductView.Filter = FilterPredicate;
        }

        private void ClearSearchQuery()
        {
            ProductSearchQuery =String.Empty;
        }

        private void AddItem()
        {
            if (int.TryParse(ProductQuantity, out int quantity))
            {
                var item = new Product { ProductName = ProductName, ProductCategory = ProductCategory, ProductQuantity = quantity };
                Products.Add(item);
                ClearInputs();
            }
        }

        private void EditItem()
        {
            if (SelectedProduct != null && int.TryParse(ProductQuantity, out int quantity))
            {
                SelectedProduct.ProductName = ProductName;
                SelectedProduct.ProductCategory = ProductCategory;
                SelectedProduct.ProductQuantity = quantity;
                FilterProducts();
                ClearInputs();
            }
        }

        private void DeleteItem()
        {
            if (SelectedProduct != null)
            {
                Products.Remove(SelectedProduct);
                FilterProducts();
                ClearInputs();
            }
        }

        private void FilterProducts()
        {
            ProductView.Refresh();
        }

        private void ClearInputs()
        {
            ProductName = string.Empty;
            ProductCategory = string.Empty;
            ProductQuantity = string.Empty;
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is Product item)
            {
                return string.IsNullOrWhiteSpace(ProductSearchQuery) ||
                       item.ProductName.Contains(ProductSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                       item.ProductCategory.Contains(ProductSearchQuery, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void PerformClearSearch(object commandParameter)
        {
        }
    }
}
