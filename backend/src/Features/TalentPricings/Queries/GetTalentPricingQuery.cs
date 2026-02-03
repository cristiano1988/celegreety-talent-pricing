using MediatR;
using Features.TalentPricings.Models;

namespace Features.TalentPricings.Queries;

public record GetTalentPricingQuery(int TalentId)
    : IRequest<TalentPricingWithHistoryDto?>;