using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkCoreNewFeatures.Models
{
    public class ProductSpecifications
    {
        public string? Color { get; set; }
        public string? Size { get; set; }
        public double? Weight { get; set; }
        public string? Brand { get; set; }
        public string? Material { get; set; }
        public string? Model { get; set; }
        public int? WarrantyMonths { get; set; }
        public bool? InStock { get; set; } = true;
        [NotMapped]
        public Dictionary<string, string> CustomAttributes { get; set; } = new Dictionary<string, string>();

    }
}
