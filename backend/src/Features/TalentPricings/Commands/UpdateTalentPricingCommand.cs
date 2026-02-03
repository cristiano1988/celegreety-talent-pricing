using MediatR;

namespace Features.TalentPricings.Commands;

public record UpdateTalentPricingCommand(
    int TalentId,
    int PersonalPrice,
    int BusinessPrice,
    string? ChangeReason,
    int Version,
    string Currency = "EUR"
) : IRequest;