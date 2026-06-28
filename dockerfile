# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MyApiBlya.csproj", "/src/"]
RUN dotnet restore "MyApiBlya.csproj" /p:EnableEfDesign=false

COPY . .
RUN dotnet publish "MyApiBlya.csproj" -c Release -o /app/publish --no-restore /p:EnableEfDesign=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MyApiBlya.dll"]
