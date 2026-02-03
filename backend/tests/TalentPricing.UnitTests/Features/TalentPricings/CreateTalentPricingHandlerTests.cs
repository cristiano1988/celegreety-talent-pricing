using Moq;
using Xunit;
using Features.TalentPricings.Commands;
using Features.TalentPricings.Interfaces;
using Features.TalentPricings.Models;
using Services;

namespace TalentPricing.UnitTests.Features.TalentPricings;

public class CreateTalentPricingHandlerTests
{
    private readonly Mock<ITalentPricingRepository> _mockRepo;
    private readonly Mock<IStripeService> _mockStripe;
    private readonly CreateTalentPricingHandler _handler;

    public CreateTalentPricingHandlerTests()
    {
        _mockRepo = new Mock<ITalentPricingRepository>();
        _mockStripe = new Mock<IStripeService>();
        _handler = new CreateTalentPricingHandler(_mockRepo.Object, _mockStripe.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesProductAndPrices()
    {
        // Arrange
        var command = new CreateTalentPricingCommand(123, 5000, 10000, "EUR");
        var productId = "prod_123";
        var personalPriceId = "price_p1";
        var businessPriceId = "price_b1";

        _mockStripe.Setup(s => s.CreateProductAsync(command.TalentId, It.IsAny<string>()))
            .ReturnsAsync(productId);
        _mockStripe.Setup(s => s.CreatePriceAsync(productId, command.PersonalPrice, command.Currency, "personal"))
            .ReturnsAsync(personalPriceId);
        _mockStripe.Setup(s => s.CreatePriceAsync(productId, command.BusinessPrice, command.Currency, "business"))
            .ReturnsAsync(businessPriceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(productId, result.StripeProductId);
        Assert.Equal(personalPriceId, result.StripePersonalPriceId);
        Assert.Equal(businessPriceId, result.StripeBusinessPriceId);
        
        // Verify DB calls
        _mockRepo.Verify(r => r.UpsertTalentPricingAsync(It.Is<TalentPricingDto>(dto => 
            dto.StripeProductId == productId &&
            dto.PersonalPrice == command.PersonalPrice &&
            dto.BusinessPrice == command.BusinessPrice
        )), Times.Once);

        _mockRepo.Verify(r => r.InsertPricingHistoryAsync(
            command.TalentId,
            command.PersonalPrice,
            command.BusinessPrice,
            productId,
            personalPriceId,
            businessPriceId,
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task Handle_BusinessPriceLowerThanPersonal_ThrowsException()
    {
        // Arrange
        var command = new CreateTalentPricingCommand(123, 5000, 4000, "EUR"); // Business < Personal

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
        
        // Verify no interactions
        _mockStripe.Verify(s => s.CreateProductAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _mockRepo.Verify(r => r.UpsertTalentPricingAsync(It.IsAny<TalentPricingDto>()), Times.Never);
    }
}
