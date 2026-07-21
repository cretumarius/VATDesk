# syntax=docker/dockerfile:1

# --- Stage 1: build the frontend ---
FROM node:20-alpine AS frontend-build
WORKDIR /frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# --- Stage 2: build & publish the API ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src

COPY VatDesk.sln ./
COPY src/VatDesk.Core/VatDesk.Core.csproj src/VatDesk.Core/
COPY src/VatDesk.Infrastructure/VatDesk.Infrastructure.csproj src/VatDesk.Infrastructure/
COPY src/VatDesk.Api/VatDesk.Api.csproj src/VatDesk.Api/
COPY tests/VatDesk.Tests/VatDesk.Tests.csproj tests/VatDesk.Tests/
COPY src/ src/
RUN dotnet publish src/VatDesk.Api/VatDesk.Api.csproj -c Release -o /app/publish

# --- Stage 3: runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=api-build /app/publish ./
COPY --from=frontend-build /frontend/dist ./wwwroot

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VatDesk.Api.dll"]
