#!/bin/sh
set -e

dotnet Markov.ConsoleApp.dll fetch-crypto BTCUSDT
dotnet Markov.ConsoleApp.dll calc-markov BTCUSDT Up,Up,Up,Down,Down
dotnet Markov.ConsoleApp.dll calc-reversal BTCUSDT 4
