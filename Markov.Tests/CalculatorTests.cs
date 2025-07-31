using Markov.Services.Models;
using Markov.Services.Services;

namespace Markov.Tests;

public class CalculatorTests
{
    [Fact]
    public void CalculateNextMovementProbability_ShouldReturnCorrectProbability()
    {
        // Arrange
        var calculator = new MarkovChainCalculator();
        var asset = new Asset
        {
            Name = "Test",
            HistoricalData = new List<Candle>
            {
                new() { Movement = Movement.Up },
                new() { Movement = Movement.Down },
                new() { Movement = Movement.Down },
                new() { Movement = Movement.Up },
                new() { Movement = Movement.Down },
                new() { Movement = Movement.Up },
            },
            Source = "Test"
        };
        var pattern = new[] { Movement.Up, Movement.Down };

        // Act
        var probability = calculator.CalculateNextMovementProbability(asset, pattern);

        // Assert
        Assert.Equal(0.5, probability, 5);
    }

    [Fact]
    public void CalculateReversalProbability_ShouldReturnCorrectProbability()
    {
        // Arrange
        var calculator = new ReversalCalculator();
        var asset = new Asset
        {
            Name = "Test",
            HistoricalData = new List<Candle>
            {
                new() { Movement = Movement.Up },
                new() { Movement = Movement.Up },
                new() { Movement = Movement.Down },
                new() { Movement = Movement.Up },
                new() { Movement = Movement.Up },
                new() { Movement = Movement.Up },
            },
            Source = "Test"
        };
        var consecutiveMovements = 2;

        // Act
        var probability = calculator.CalculateReversalProbability(asset, consecutiveMovements);

        // Assert
        Assert.Equal(0.5, probability.UpReversalPercentage, 5);
        Assert.Equal(0.5, probability.DownReversalPercentage, 5);
    }
}