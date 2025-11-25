using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SimpleInventoryApp
{
    public class ProductViewModel : INotifyPropertyChanged
    {
        private readonly ProductDbContext _db;
        private string productName;
        private string productCategory;
        private string productQuantity;
        private string productSearchQuery;
        private Product selectedProduct;
        private bool isSuggestionOpen;
        private ICollectionView productView;
        private IDialogMessageService dialogMessageService;

        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<Product> FilteredProducts { get; set; }

        public ObservableCollection<string> ProductSuggestions { get; set; } = new();

        private void LoadSuggestions(string input)
        {
            ProductSuggestions.Clear();
            if (!string.IsNullOrWhiteSpace(input))
            {
                var matches = _db.Products
                .Where(i => i.ProductName.StartsWith(input))
                .Select(i => i.ProductName)
                .Take(10)
                .ToList();

                foreach (var m in matches)
                    ProductSuggestions.Add(m);
                
                IsSuggestionOpen = true;
            }
            else 
            { 
                IsSuggestionOpen= false;
            }
            
        }

        private void AutoPopulateFields(string input)
        {
            var existing = _db.Products
                .FirstOrDefault(i => i.ProductName == input);

            if (existing != null)
            {
                ProductCategory = existing.ProductCategory;
                ProductQuantity = existing.ProductQuantity.ToString();
            }
        }

        [Required]
        public string ProductName
        {
            get => productName;
            set  
            { 
                productName = value.Trim(); 
                OnPropertyChanged(nameof(productName));
                LoadSuggestions(productName);
                AutoPopulateFields(productName);
            }
        }

        public string ProductCategory
        {
            get => productCategory;
            set { productCategory = value.Trim(); OnPropertyChanged(nameof(productCategory)); }
        }

        [Range(0, int.MaxValue)]
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
                    AutoPopulateFields(SelectedProduct.ProductName);
                    productCategory = selectedProduct.ProductCategory;
                    productQuantity = selectedProduct.ProductQuantity.ToString();
                    IsSuggestionOpen = false;
                    OnPropertyChanged(nameof(IsSuggestionOpen));
                }
                OnPropertyChanged(nameof(selectedProduct));
            }
        }

        public ICollectionView ProductView
        {
            get => productView;
            set { productView = value; OnPropertyChanged(nameof(productView)); }
        }

        public bool IsSuggestionOpen
        {
            get => isSuggestionOpen;
            set { isSuggestionOpen = value; OnPropertyChanged(nameof(IsSuggestionOpen)); }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ICommand ClearSearchCommand {  get; }

        public ProductViewModel()
        {
            _db = new ProductDbContext();
            _db.Database.EnsureCreated();

            Products = new ObservableCollection<Product>(_db.Products.ToList());
            FilteredProducts = new ObservableCollection<Product>();
            AddCommand = new DelegateCmd(_ => AddItem());
            EditCommand = new DelegateCmd(_ => EditItem(), _ => SelectedProduct != null);
            DeleteCommand = new DelegateCmd(_ => DeleteItem(), _ => SelectedProduct != null);
            ClearSearchCommand = new DelegateCmd(_ => ClearSearchQuery());
            ProductView = CollectionViewSource.GetDefaultView(Products);
            ProductView.Filter = FilterPredicate;
            dialogMessageService = new DialogMessageService();
        }

        private void ClearSearchQuery()
        {
            ProductSearchQuery =String.Empty;
        }

        private void AddItem()
        {
            if (int.TryParse(ProductQuantity, out int quantity))
            {
                bool recordExists = _db.Products
            .Any(i => i.ProductName == ProductName);

                if (recordExists)
                {
                    dialogMessageService.Show("Item Already Exist in a record (Can't be added, Try Editing)","Duplicate Entry");
                    return;
                }

                var item = new Product { ProductName = ProductName, ProductCategory = ProductCategory, ProductQuantity = quantity };
                try 
                {
                    _db.Products.Add(item);
                    _db.SaveChanges();
                } 
                catch (DbUpdateException ex) 
                {
                    if (ex.InnerException is SqlException sqlEx)
                    {
                        switch (sqlEx.Number)
                        {
                            case 2601: // Duplicate key row
                                dialogMessageService.Show("Item can't be added. It already exists.",
                                                "Duplicate Item");
                                break;

                            case 547: // Foreign key violation
                                dialogMessageService.Show("Invalid category reference. Please select a valid category.",
                                                "Constraint Error");
                                break;

                            case 515: // Not Null violation
                                dialogMessageService.Show("Required fields are missing. Please fill all mandatory fields.",
                                                "Constraint Error");
                                break;

                            default:
                                dialogMessageService.Show("Database error: " + sqlEx.Message,
                                                "Error");
                                break;
                        }
                    }
                }
                
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
                _db.Products.Update(SelectedProduct);
                _db.SaveChanges();
                FilterProducts();
                ClearInputs();
            }
        }

        private void DeleteItem()
        {
            if (SelectedProduct != null)
            {
                _db.Products.Remove(SelectedProduct);
                _db.SaveChanges();
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
                       item.ProductName.Contains(ProductSearchQuery.Trim(), StringComparison.OrdinalIgnoreCase) ||
                       item.ProductCategory.Contains(ProductSearchQuery.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
