# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 10000

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AcxiomCRM/AcxiomCRM.csproj", "AcxiomCRM/"]
RUN dotnet restore "AcxiomCRM/AcxiomCRM.csproj"
COPY . .
WORKDIR "/src/AcxiomCRM"
RUN dotnet build "AcxiomCRM.csproj" -c Release -o /app/build

# Publish image
FROM build AS publish
RUN dotnet publish "AcxiomCRM.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

ENTRYPOINT ["dotnet", "AcxiomCRM.dll"]
