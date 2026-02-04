using Xunit;
using Features.TalentPricings.Commands;
using Features.TalentPricings.Validators;

namespace TalentPricing.UnitTests.Features.TalentPricings;

public class ValidatorTests
{
    [Fact]
    public void CreatePricingValidator_WithInvalidPrices_HasErrors()
    {
        var validator = new CreatePricingValidator();
        var command = new CreateTalentPricingCommand(123, 1000, 500, "EUR"); // Business < Personal

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "BusinessPrice");
    }

    [Fact]
    public void CreatePricingValidator_WithUnsupportedCurrency_HasErrors()
    {
        var validator = new CreatePricingValidator();
        var command = new CreateTalentPricingCommand(123, 1000, 1500, "USD");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Fact]
    public void UpdatePricingValidator_WithInvalidVersion_HasErrors()
    {
        var validator = new UpdatePricingValidator();
        var command = new UpdateTalentPricingCommand(123, 1000, 1500, "Update", 0);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Version");
    }
}
