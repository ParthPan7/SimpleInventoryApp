using System.ComponentModel.DataAnnotations;

namespace SimpleInventoryApp
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name too long")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public string ProductCategory { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int ProductQuantity { get; set; }
    }
}
