namespace Features.TalentPricings.Commands;


public class CreateTalentPricingResult
{
public int TalentId { get; set; }
public string StripeProductId { get; set; } = default!;
public int PersonalPrice { get; set; }
public int BusinessPrice { get; set; }
public string StripePersonalPriceId { get; set; } = default!;
public string StripeBusinessPriceId { get; set; } = default!;
public DateTimeOffset LastSyncedAt { get; set; }
}