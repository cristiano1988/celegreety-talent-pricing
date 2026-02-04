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
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<CreateTalentPricingHandler>> _mockLogger;
    private readonly CreateTalentPricingHandler _handler;

    public CreateTalentPricingHandlerTests()
    {
        _mockRepo = new Mock<ITalentPricingRepository>();
        _mockStripe = new Mock<IStripeService>();
        _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<CreateTalentPricingHandler>>();
        _handler = new CreateTalentPricingHandler(_mockRepo.Object, _mockStripe.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesProductAndPrices()
    {
        // Arrange
        var command = new CreateTalentPricingCommand(123, 5000, 10000, "EUR");
        var productId = "prod_123";
        var personalPriceId = "price_p1";
        var businessPriceId = "price_b1";

        _mockRepo.Setup(r => r.TalentExistsAsync(command.TalentId)).ReturnsAsync(true);
        _mockRepo.Setup(r => r.GetTalentProfileAsync(command.TalentId)).ReturnsAsync((TalentPricingDto?)null);

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
        _mockRepo.Verify(r => r.UpsertWithHistoryAsync(It.Is<TalentPricingDto>(dto => 
            dto.StripeProductId == productId &&
            dto.PersonalPrice == command.PersonalPrice &&
            dto.BusinessPrice == command.BusinessPrice
        ), It.IsAny<string>(), It.IsAny<int?>()), Times.Once);
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
        _mockRepo.Verify(r => r.UpsertWithHistoryAsync(It.IsAny<TalentPricingDto>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TalentDoesNotExist_ThrowsException()
    {
        // Arrange
        var command = new CreateTalentPricingCommand(123, 5000, 10000, "EUR");
        _mockRepo.Setup(r => r.TalentExistsAsync(123)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PricingAlreadyExists_ThrowsException()
    {
        // Arrange
        var command = new CreateTalentPricingCommand(123, 5000, 10000, "EUR");
        _mockRepo.Setup(r => r.TalentExistsAsync(123)).ReturnsAsync(true);
        _mockRepo.Setup(r => r.GetTalentProfileAsync(123)).ReturnsAsync(new TalentPricingDto { StripeProductId = "prod_already_exists" });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
