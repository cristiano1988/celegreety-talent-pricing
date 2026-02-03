# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Celegreety.TalentPricing.Backend.csproj", "src/"]
RUN dotnet restore "src/Celegreety.TalentPricing.Backend.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "Celegreety.TalentPricing.Backend.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "Celegreety.TalentPricing.Backend.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Celegreety.TalentPricing.Backend.dll"]