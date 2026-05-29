# Multi-stage build Dockerfile for AcoTestApi
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files first to restore dependencies (for efficient caching)
COPY src/AcoTestApi.Domain/AcoTestApi.Domain.csproj src/AcoTestApi.Domain/
COPY src/AcoTestApi.Application/AcoTestApi.Application.csproj src/AcoTestApi.Application/
COPY src/AcoTestApi.Infrastructure/AcoTestApi.Infrastructure.csproj src/AcoTestApi.Infrastructure/
COPY src/AcoTestApi.API/AcoTestApi.API.csproj src/AcoTestApi.API/

# Restore dependencies directly targeting the Web API project (which automatically restores referenced classlibs)
RUN dotnet restore src/AcoTestApi.API/AcoTestApi.API.csproj

# Copy all source files
COPY src/ src/

# Build and publish
WORKDIR /app/src/AcoTestApi.API
RUN dotnet publish -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port and configure environment
ENV ASPNETCORE_HTTP_PORTS=5000
ENV PORT=5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "AcoTestApi.API.dll"]
