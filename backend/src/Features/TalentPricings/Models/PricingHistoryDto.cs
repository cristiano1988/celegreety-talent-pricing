namespace Features.TalentPricings.Models;

public class PricingHistoryDto
{
    public int PersonalPrice { get; set; }
    public int BusinessPrice { get; set; }
    public string? ChangeReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}