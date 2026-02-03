namespace Features.TalentPricings.Models;

public class TalentPricingWithHistoryDto
{
    public TalentPricingDto Current { get; set; } = default!;
    public List<PricingHistoryDto> History { get; set; } = new();
}