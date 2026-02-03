namespace Features.TalentPricings.Models;

public class TalentPricingDto
{
    public int TalentId { get; set; }
    public string StripeProductId { get; set; } = default!;
    public int PersonalPrice { get; set; }
    public int BusinessPrice { get; set; }
    public string StripePersonalPriceId { get; set; } = default!;
    public string StripeBusinessPriceId { get; set; } = default!;
    public DateTimeOffset PricesLastSyncedAt { get; set; }
    public int Version { get; set; }
}