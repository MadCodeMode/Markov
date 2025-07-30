using Dapper;
using Markov.Core.Interfaces;
using Markov.Core.Models;
using Npgsql;

namespace Markov.Core.Repositories;

public class DataRepository : IDataRepository
{
    private readonly string _connectionString;

    public DataRepository(string connectionString)
    {
        _connectionString = connectionString;
        Initialize();
    }

    private void Initialize()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = new NpgsqlCommand
        {
            Connection = connection,
            CommandText = @"
                CREATE TABLE IF NOT EXISTS Candles (
                    AssetName VARCHAR(10) NOT NULL,
                    Timestamp TIMESTAMP NOT NULL,
                    Movement INT NOT NULL,
                    PRIMARY KEY (AssetName, Timestamp)
                );"
        };
        command.ExecuteNonQuery();
    }

    public async Task<Asset> GetAssetAsync(string assetName)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var candles = await connection.QueryAsync<Candle>("SELECT * FROM Candles WHERE AssetName = @AssetName", new { AssetName = assetName });
        
        return new Asset
        {
            Name = assetName,
            HistoricalData = candles.ToList()
        };
    }

    public async Task SaveAssetAsync(Asset asset)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        await connection.ExecuteAsync("DELETE FROM Candles WHERE AssetName = @AssetName", new { AssetName = asset.Name });

        foreach (var candle in asset.HistoricalData)
        {
            await connection.ExecuteAsync("INSERT INTO Candles (AssetName, Timestamp, Movement) VALUES (@AssetName, @Timestamp, @Movement)", new { AssetName = asset.Name, candle.Timestamp, candle.Movement });
        }
    }
}