#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Markov.ConsoleApp/Markov.ConsoleApp.csproj", "Markov.ConsoleApp/"]
COPY ["Markov.Core/Markov.Core.csproj", "Markov.Core/"]
RUN dotnet restore "./Markov.ConsoleApp/Markov.ConsoleApp.csproj"
COPY . .
WORKDIR "/src/Markov.ConsoleApp"
RUN dotnet build "./Markov.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Markov.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY run-commands.sh .
RUN chmod +x run-commands.sh
ENTRYPOINT ["./run-commands.sh"]
USER app
