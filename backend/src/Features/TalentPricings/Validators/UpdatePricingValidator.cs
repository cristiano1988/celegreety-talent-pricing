using FluentValidation;
using Features.TalentPricings.Commands;

namespace Features.TalentPricings.Validators;

public class UpdatePricingValidator : AbstractValidator<UpdateTalentPricingCommand>
{
    public UpdatePricingValidator()
    {
        RuleFor(x => x.TalentId).NotEmpty();
        
        RuleFor(x => x.PersonalPrice)
            .GreaterThan(0)
            .LessThan(2000000000)
            .WithMessage("Personal price must be between 0.01 and 20,000,000.00 EUR");

        RuleFor(x => x.BusinessPrice)
            .GreaterThan(0)
            .LessThan(2000000000)
            .GreaterThanOrEqualTo(x => x.PersonalPrice)
            .WithMessage("Business price must be between 0.01 and 20,000,000.00 EUR and >= Personal price");

        RuleFor(x => x.Currency)
            .Equal("EUR")
            .WithMessage("Only EUR is supported currently.");
            
        RuleFor(x => x.Version).GreaterThan(0);
    }
}
