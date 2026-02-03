using MediatR;
using Features.TalentPricings.Interfaces;
using Features.TalentPricings.Models;

namespace Features.TalentPricings.Queries;

public class GetTalentPricingHandler
    : IRequestHandler<GetTalentPricingQuery, TalentPricingWithHistoryDto?>
{
    private readonly ITalentPricingRepository _repository;

    public GetTalentPricingHandler(ITalentPricingRepository repository)
    {
        _repository = repository;
    }

    public async Task<TalentPricingWithHistoryDto?> Handle(
        GetTalentPricingQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetTalentPricingWithHistoryAsync(
            request.TalentId);
    }
}