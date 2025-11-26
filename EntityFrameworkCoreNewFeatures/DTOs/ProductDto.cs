using EntityFrameworkCoreNewFeatures.Models;

namespace EntityFrameworkCoreNewFeatures.DTOs
{
    public record ProductDto(string Name, decimal Price, int CategoryId, ProductSpecifications? ProductSpecifications);
}
