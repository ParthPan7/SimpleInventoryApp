using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;

namespace SimpleInventoryApp
{
    public class ProductViewModel : INotifyPropertyChanged ,IDataErrorInfo
    {
        private readonly ProductDbContext _db;
        private string productName;
        private bool productNameActivated;
        private string productCategory;
        private bool productCategoryActivated;
        private bool productQuantityActivated;
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
                productName = string.IsNullOrWhiteSpace(value) ? null : value; 
                OnPropertyChanged(nameof(ProductName));
                LoadSuggestions(productName);
                AutoPopulateFields(productName);
                productNameActivated = true;
            }
        }

        [Required]
        public string ProductCategory
        {
            get => productCategory;
            set 
            { 
                productCategory = string.IsNullOrWhiteSpace(value) ? null:value; 
                OnPropertyChanged(nameof(ProductCategory));
                productCategoryActivated = true;
            }
        }

        [Range(0, int.MaxValue)]
        public string ProductQuantity
        {
            get => productQuantity;
            set 
            { 
                productQuantity = string.IsNullOrWhiteSpace(value) ? null : value; 
                OnPropertyChanged(nameof(ProductQuantity));
                productQuantityActivated = true;
            }
        }

        public string ProductSearchQuery
        {
            get => productSearchQuery;
            set { productSearchQuery = value; OnPropertyChanged(nameof(ProductSearchQuery)); FilterProducts();  }
        }

        public Product SelectedProduct
        {
            get => selectedProduct;
            set
            {
                selectedProduct = value;
                if (selectedProduct != null)
                {
                    ProductName = selectedProduct.ProductName;
                    AutoPopulateFields(SelectedProduct.ProductName);
                    ProductCategory = selectedProduct.ProductCategory;
                    ProductQuantity = selectedProduct.ProductQuantity.ToString();
                    IsSuggestionOpen = false;
                    OnPropertyChanged(nameof(IsSuggestionOpen));
                }
                OnPropertyChanged(nameof(SelectedProduct));
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

        public string Error => throw new NotImplementedException();

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

                var item = new Product { ProductName = ProductName?.Trim(), ProductCategory = ProductCategory?.Trim(), ProductQuantity = quantity };
                try 
                {
                    _db.Products.Add(item);
                    _db.SaveChanges();
                    Products.Add(item);
                    ClearInputs();
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
                   
            }
            
        }

        private void EditItem()
        {
            if (SelectedProduct == null)
            {
                dialogMessageService.Show("No product selected", "Validation Error");
                return;
            }

            if (!int.TryParse(ProductQuantity, out int quantity))
            {
                dialogMessageService.Show("Valid quantity is required", "Validation Error");
                return;
            }

            SelectedProduct.ProductName = ProductName?.Trim();
            SelectedProduct.ProductCategory = ProductCategory?.Trim();
            SelectedProduct.ProductQuantity = quantity;

            try
            {
                _db.Products.Update(SelectedProduct);
                _db.SaveChanges();
                FilterProducts();
                ClearInputs();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx)
                {
                    switch (sqlEx.Number)
                    {
                        case 2601: 
                        case 2627: 
                            dialogMessageService.Show("Item can't be updated. A product with this name already exists.", "Duplicate Item");
                            break;

                        case 547: 
                            dialogMessageService.Show("Invalid category reference. Please select a valid category.", "Constraint Error");
                            break;

                        case 515: 
                            dialogMessageService.Show("Required fields are missing. Please fill all mandatory fields.", "Not Null Constraint");
                            break;

                        default:
                            dialogMessageService.Show($"Database error (Code {sqlEx.Number}): {sqlEx.Message}", "Error");
                            break;
                    }
                }
                else
                {
                    dialogMessageService.Show($"Error: {ex.Message}", "Error");
                }

                var entry = _db.Entry(SelectedProduct);
                entry.Reload();
                OnPropertyChanged(nameof(SelectedProduct));
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
            productNameActivated = false;
            productCategoryActivated = false;
            productQuantityActivated = false;
            OnPropertyChanged(nameof(ProductName));
            OnPropertyChanged(nameof(ProductCategory));
            OnPropertyChanged(nameof(ProductQuantity));
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

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(ProductName):
                        if (!productNameActivated)
                            return null;
                        if (string.IsNullOrWhiteSpace(ProductName))
                            return "Product name can't be empty";
                        if (ProductName.Length < 3)
                            return "Product name must be at least 3 characters.";
                        if (ProductName.Length > 50)
                            return "Product name cannot exceed 50 characters.";
                        if (!Regex.IsMatch(ProductName, @"^[a-zA-Z0-9\s\-]+$"))
                            return "Product name can only contain letters, numbers, spaces, or dashes.";
                        break;

                    case nameof(ProductCategory):
                        if (!productCategoryActivated)
                            return null;
                        if (string.IsNullOrWhiteSpace(ProductCategory))
                            return "Product Category can't be empty";
                        if (ProductCategory.Length < 3)
                            return "Category must be at least 3 characters.";
                        if (ProductCategory.Length > 30)
                            return "Category cannot exceed 30 characters.";
                        if (!Regex.IsMatch(ProductCategory, @"^[a-zA-Z0-9\s\-]+$"))
                            return "Category can only contain letters, numbers, spaces, or dashes.";
                        break;

                    case nameof(ProductQuantity):
                        if (!productQuantityActivated)
                            return null;
                        if (string.IsNullOrWhiteSpace(ProductQuantity))
                            return "Valid product quantity is required";
                        if (!int.TryParse(ProductQuantity, out var qty))
                            return "Quantity must be a valid integer.";
                        if (qty < 0)
                            return "Quantity must be a non-negative number.";
                        break;
                }
                return null;
            }
        }


    }
}
