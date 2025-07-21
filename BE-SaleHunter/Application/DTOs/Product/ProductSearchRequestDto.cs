namespace BE_SaleHunter.Application.DTOs.Product;

public class ProductSearchRequestDto
{
    public string? Query { get; init; } = "";
    public long? StoreId { get; init; }
    public string? Category { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? SortBy { get; init; } = "popularity"; // Default sort by popularity
    public string? Brand { get; init; }
}

public static class ProductSortOptions
{
    public const string Popularity = "popularity";
    public const string PriceAsc = "price_asc";
    public const string PriceDsc = "price_dsc";
    public const string Rating = "rating";
    public const string Newest = "newest";
    public const string BestDeal = "best_deal";
    public const string NearestStore = "nearest_store";
}