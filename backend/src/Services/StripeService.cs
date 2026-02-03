using Stripe;
using Polly;
using Polly.Retry;

namespace Services;

public class StripeService : IStripeService
{
    private readonly ResiliencePipeline _pipeline;

    public StripeService(IConfiguration configuration)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<StripeException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential
            })
            .Build();
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

        var requestOptions = new RequestOptions { IdempotencyKey = Guid.NewGuid().ToString() };
        var service = new ProductService();
        
        var product = await _pipeline.ExecuteAsync(async cancellationToken => 
            await service.CreateAsync(options, requestOptions, cancellationToken));

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

        var requestOptions = new RequestOptions { IdempotencyKey = Guid.NewGuid().ToString() };
        var service = new PriceService();

        var price = await _pipeline.ExecuteAsync(async cancellationToken => 
            await service.CreateAsync(options, requestOptions, cancellationToken));

        return price.Id;
    }

    public async Task ArchivePriceAsync(string priceId)
    {
        var service = new PriceService();
        var requestOptions = new RequestOptions { IdempotencyKey = Guid.NewGuid().ToString() };

        await _pipeline.ExecuteAsync(async cancellationToken =>
            await service.UpdateAsync(priceId, new PriceUpdateOptions
            {
                Active = false
            }, requestOptions, cancellationToken));
    }

    public async Task ArchiveProductAsync(string productId)
    {
        var service = new ProductService();
        var requestOptions = new RequestOptions { IdempotencyKey = Guid.NewGuid().ToString() };

        await _pipeline.ExecuteAsync(async cancellationToken =>
            await service.UpdateAsync(productId, new ProductUpdateOptions
            {
                Active = false
            }, requestOptions, cancellationToken));
    }
}