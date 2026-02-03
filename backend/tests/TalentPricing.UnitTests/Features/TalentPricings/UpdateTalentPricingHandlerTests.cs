using Moq;
using Xunit;
using Features.TalentPricings.Commands;
using Features.TalentPricings.Interfaces;
using Features.TalentPricings.Models;
using Services;

namespace TalentPricing.UnitTests.Features.TalentPricings;

public class UpdateTalentPricingHandlerTests
{
    private readonly Mock<ITalentPricingRepository> _mockRepo;
    private readonly Mock<IStripeService> _mockStripe;
    private readonly UpdateTalentPricingHandler _handler;

    public UpdateTalentPricingHandlerTests()
    {
        _mockRepo = new Mock<ITalentPricingRepository>();
        _mockStripe = new Mock<IStripeService>();
        _handler = new UpdateTalentPricingHandler(_mockRepo.Object, _mockStripe.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ArchivesOldAndCreatesNewPrices()
    {
        // Arrange
        var command = new UpdateTalentPricingCommand(123, 6000, 12000, "Inflation");
        var currentPricing = new TalentPricingWithHistoryDto
        {
            Current = new TalentPricingDto
            {
                TalentId = 123,
                StripeProductId = "prod_123",
                PersonalPrice = 5000,
                BusinessPrice = 10000,
                StripePersonalPriceId = "price_old_p",
                StripeBusinessPriceId = "price_old_b"
            }
        };

        var newPersonalPriceId = "price_new_p";
        var newBusinessPriceId = "price_new_b";

        _mockRepo.Setup(r => r.GetTalentPricingWithHistoryAsync(123, It.IsAny<int>()))
            .ReturnsAsync(currentPricing);

        _mockStripe.Setup(s => s.CreatePriceAsync("prod_123", command.PersonalPrice, "EUR", "personal"))
            .ReturnsAsync(newPersonalPriceId);
        _mockStripe.Setup(s => s.CreatePriceAsync("prod_123", command.BusinessPrice, "EUR", "business"))
            .ReturnsAsync(newBusinessPriceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // 1. Check archiving
        _mockStripe.Verify(s => s.ArchivePriceAsync("price_old_p"), Times.Once);
        _mockStripe.Verify(s => s.ArchivePriceAsync("price_old_b"), Times.Once);

        // 2. Check DB update
        _mockRepo.Verify(r => r.UpsertTalentPricingAsync(It.Is<TalentPricingDto>(dto =>
            dto.StripeProductId == "prod_123" &&
            dto.PersonalPrice == 6000 &&
            dto.StripePersonalPriceId == newPersonalPriceId
        )), Times.Once);

        // 3. Check history insert
        _mockRepo.Verify(r => r.InsertPricingHistoryAsync(
            123,
            6000,
            12000,
            "prod_123",
            newPersonalPriceId,
            newBusinessPriceId,
            "Inflation"
        ), Times.Once);
    }

    [Fact]
    public async Task Handle_BusinessPriceLowerThanPersonal_ThrowsException()
    {
        // Arrange
        var command = new UpdateTalentPricingCommand(123, 5000, 4000, "Error");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PricingDoesNotExist_ThrowsException()
    {
        // Arrange
        var command = new UpdateTalentPricingCommand(999, 5000, 10000, "New");
        _mockRepo.Setup(r => r.GetTalentPricingWithHistoryAsync(999, It.IsAny<int>()))
            .ReturnsAsync((TalentPricingWithHistoryDto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
