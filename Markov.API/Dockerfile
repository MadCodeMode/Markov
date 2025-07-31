# Use the .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution file and project files from the correct locations
COPY Markov.sln .
COPY Markov.API/ ./Markov.API/
COPY Markov.Services/ ./Markov.Services/
COPY Markov.Tests/ ./Markov.Tests/

# Restore dependencies for the solution
RUN dotnet restore "Markov.sln"

# Copy the rest of the source code
COPY . .

# Publish the API project
WORKDIR "/src/Markov.API"
RUN dotnet publish "Markov.API.csproj" -c Release -o /app/publish

# Use the ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Markov.API.dll"]
