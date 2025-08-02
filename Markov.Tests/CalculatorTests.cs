// using Markov.Services.Models;
// using Markov.Services.Services;

// namespace Markov.Tests;

// public class CalculatorTests
// {
//     [Fact]
//     public void CalculateNextMovementProbability_ShouldReturnCorrectProbability()
//     {
//         // Arrange
//         var calculator = new MarkovChainCalculator();
//         var asset = new Asset
//         {
//             Name = "Test",
//             HistoricalData = new List<Candle>
//             {
//                 new() { Movement = Movement.Up },
//                 new() { Movement = Movement.Down },
//                 new() { Movement = Movement.Down },
//                 new() { Movement = Movement.Up },
//                 new() { Movement = Movement.Down },
//                 new() { Movement = Movement.Up },
//             },
//             Source = "Test"
//         };
//         var pattern = new[] { Movement.Up, Movement.Down };

//         // Act
//         var probability = calculator.CalculateNextMovementProbability(asset, pattern);

//         // Assert
//         Assert.Equal(0.5, probability, 5);
//     }

//     [Fact]
//     public void CalculateReversalProbability_ShouldReturnCorrectProbability()
//     {
//         // Arrange
//         var calculator = new ReversalCalculator();
//         var asset = new Asset
//         {
//             Name = "Test",
//             HistoricalData = new List<Candle>
//             {
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(1), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(2), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(3), Movement = Movement.Down }, // Reversal
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(4), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(5), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(6), Movement = Movement.Up }, // Not a reversal
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(7), Movement = Movement.Down },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(8), Movement = Movement.Down },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(9), Movement = Movement.Up }, // Reversal
//             },
//             Source = "Test"
//         };
//         var consecutiveMovements = 2;

//         var parameters = new BacktestParameters { ConsecutiveMovements = consecutiveMovements };

//         // Act
//         var probability = calculator.CalculateReversalProbability(asset, parameters);

//         // Assert
//         Assert.Equal(1, probability.UpReversalPercentage, 5);
//         Assert.Equal(1, probability.DownReversalPercentage, 5);
//         Assert.Single(probability.UpReversalData);
//         Assert.Single(probability.DownReversalData);
//     }

//     [Fact]
//     public void CalculateReversalProbability_WithComplexData_ShouldBehaveCorrectly()
//     {
//         // Arrange
//         var calculator = new ReversalCalculator();
//         var asset = new Asset
//         {
//             Name = "ComplexTest",
//             HistoricalData = new List<Candle>
//             {
//                 // Scenario 1: Exact 2 'Up' sequence with reversal
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(1), Movement = Movement.Down },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(2), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(3), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(4), Movement = Movement.Down }, // Reversal 1 (Up)

//                 // Scenario 2: Exact 2 'Up' sequence without reversal
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(5), Movement = Movement.Down },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(6), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(7), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(8), Movement = Movement.Up },   // Longer sequence, should be ignored

//                 // Scenario 3: Exact 2 'Down' sequence with reversal
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(9), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(10), Movement = Movement.Down },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(11), Movement = Movement.Down },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(12), Movement = Movement.Up },   // Reversal 1 (Down)

//                 // Scenario 4: Exact 2 'Down' sequence at the end of the list
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(13), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(14), Movement = Movement.Down },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(15), Movement = Movement.Down },

//                 // Scenario 5: Another exact 2 'Up' sequence with reversal
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(16), Movement = Movement.Down },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(17), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(18), Movement = Movement.Up },
//                 new() { Timestamp = DateTime.UtcNow.AddMinutes(19), Movement = Movement.Down }, // Reversal 2 (Up)
//             },
//             Source = "Test"
//         };
//         var consecutiveMovements = 2;

//         var parameters = new BacktestParameters { ConsecutiveMovements = consecutiveMovements };

//         // Act
//         var result = calculator.CalculateReversalProbability(asset, parameters);

//         // Assert
//         // There are 3 exact 'Up' sequences of length 2. All are followed by 'Down' (reversal).
//         Assert.Equal(1.0, result.UpReversalPercentage);
//         Assert.Equal(3, result.UpReversalData.Count);

//         // There are 2 exact 'Down' sequences of length 2. Both are followed by 'Up' (reversal).
//         Assert.Equal(1.0, result.DownReversalPercentage);
//         Assert.Equal(2, result.DownReversalData.Count);
//     }
// }