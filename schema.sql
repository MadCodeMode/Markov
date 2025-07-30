CREATE TABLE IF NOT EXISTS Candles (
    AssetName VARCHAR(10) NOT NULL,
    Timestamp TIMESTAMP NOT NULL,
    Movement INT NOT NULL,
    PRIMARY KEY (AssetName, Timestamp)
);
