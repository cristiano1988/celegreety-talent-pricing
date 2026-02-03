using MediatR;

namespace Features.TalentPricings.Commands;

public record CreateTalentPricingCommand(
int TalentId,
int PersonalPrice,
int BusinessPrice,
string Currency
) : IRequest<CreateTalentPricingResult>;