namespace Services;

public interface IStripeService
{
    Task<string> CreateProductAsync(int talentId, string talentName);
    Task<string> CreatePriceAsync(string productId, int amount, string currency, string priceType);
    Task ArchivePriceAsync(string priceId);
}