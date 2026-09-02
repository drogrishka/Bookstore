FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/Bookstore.Domain/Bookstore.Domain.csproj src/Bookstore.Domain/
COPY src/Bookstore.Application/Bookstore.Application.csproj src/Bookstore.Application/
COPY src/Bookstore.Infrastructure/Bookstore.Infrastructure.csproj src/Bookstore.Infrastructure/
COPY src/Bookstore.Api/Bookstore.Api.csproj src/Bookstore.Api/

RUN dotnet restore src/Bookstore.Api/Bookstore.Api.csproj

COPY src/ src/
RUN dotnet publish src/Bookstore.Api/Bookstore.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]
