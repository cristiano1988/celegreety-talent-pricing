namespace Common.Configuration;

public class StripeSettings
{
    public const string SectionName = "Stripe";
    public string SecretKey { get; set; } = string.Empty;
}
