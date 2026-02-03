using Stripe;

namespace Services;

public class StripeService : IStripeService
{
    public StripeService(IConfiguration configuration)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreateProductAsync(int talentId, string talentName)
    {
        var options = new ProductCreateOptions
        {
            Name = talentName,
            Metadata = new Dictionary<string, string>
            {
                { "talent_id", talentId.ToString() },
                { "type", "talent_booking" }
            }
        };

        var service = new ProductService();
        var product = await service.CreateAsync(options);

        return product.Id;
    }

    public async Task<string> CreatePriceAsync(
        string productId,
        int amount,
        string currency,
        string priceType)
    {
        var options = new PriceCreateOptions
        {
            Product = productId,
            UnitAmount = amount,
            Currency = currency.ToLower(),
            Metadata = new Dictionary<string, string>
            {
                { "price_type", priceType }
            }
        };

        var service = new PriceService();
        var price = await service.CreateAsync(options);

        return price.Id;
    }

    public async Task ArchivePriceAsync(string priceId)
    {
        var service = new PriceService();
        await service.UpdateAsync(priceId, new PriceUpdateOptions
        {
            Active = false
        });
    }
}