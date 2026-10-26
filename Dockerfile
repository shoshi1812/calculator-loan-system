# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Project files first, so the restore layer stays cached when only the code changes
COPY CalculatorLending.Core/CalculatorLending.Core.csproj CalculatorLending.Core/
COPY CalculatorLending.Data/CalculatorLending.Data.csproj CalculatorLending.Data/
COPY CalculatorLending.Service/CalculatorLending.Service.csproj CalculatorLending.Service/
COPY CalculatorLending.API/CalculatorLending.API.csproj CalculatorLending.API/
RUN dotnet restore CalculatorLending.API/CalculatorLending.API.csproj

COPY . .
RUN dotnet publish CalculatorLending.API/CalculatorLending.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CalculatorLending.API.dll"]