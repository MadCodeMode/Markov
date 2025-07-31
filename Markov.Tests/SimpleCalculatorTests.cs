using Markov.Services.Models;
using Markov.Services.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace Markov.Tests;

public class SimpleCalculatorTests
{
    [Fact]
    public void CalculateReversalProbability_WithSimpleData_ShouldBehaveCorrectly()
    {
        // Arrange
        var calculator = new ReversalCalculator();
        var asset = new Asset
        {
            Name = "SimpleTest",
            HistoricalData = new List<Candle>
            {
                new() { Timestamp = DateTime.UtcNow.AddMinutes(1), Movement = Movement.Up },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(2), Movement = Movement.Down },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(3), Movement = Movement.Up },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(4), Movement = Movement.Up },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(5), Movement = Movement.Down }, // Reversal for the first U,U sequence
                new() { Timestamp = DateTime.UtcNow.AddMinutes(6), Movement = Movement.Up },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(7), Movement = Movement.Up },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(8), Movement = Movement.Up },   // This is a sequence of 3, should be ignored
                new() { Timestamp = DateTime.UtcNow.AddMinutes(9), Movement = Movement.Down },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(10), Movement = Movement.Down },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(11), Movement = Movement.Up },   // Reversal for the first D,D sequence
                new() { Timestamp = DateTime.UtcNow.AddMinutes(12), Movement = Movement.Down },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(13), Movement = Movement.Down }, // This is a sequence of 2 at the end, no reversal
            },
            Source = "Test"
        };
        var consecutiveMovements = 2;

        // Act
        var result = calculator.CalculateReversalProbability(asset, consecutiveMovements);

        // Assert
        // There is 1 exact 'Up' sequence of length 2, and it is followed by a 'Down' (reversal).
        Assert.Equal(1.0, result.UpReversalPercentage);
        Assert.Single(result.UpReversalDates);

        // There are 2 exact 'Down' sequences of length 2. One is followed by 'Up' (reversal), one is at the end.
        Assert.Equal(0.5, result.DownReversalPercentage);
        Assert.Single(result.DownReversalDates);
    }
}
