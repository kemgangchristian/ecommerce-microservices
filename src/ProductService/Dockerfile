FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ProductService/ProductService.csproj", "src/ProductService/"]
COPY ["src/Shared/ECommerce.Contracts/ECommerce.Contracts.csproj", "src/Shared/ECommerce.Contracts/"]
RUN dotnet restore "src/ProductService/ProductService.csproj"
COPY . .
WORKDIR /src/src/ProductService
RUN dotnet publish "ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]