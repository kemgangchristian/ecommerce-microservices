FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/OrderService/OrderService.csproj", "src/OrderService/"]
COPY ["src/Shared/ECommerce.Contracts/ECommerce.Contracts.csproj", "src/Shared/ECommerce.Contracts/"]
RUN dotnet restore "src/OrderService/OrderService.csproj"
COPY . .
WORKDIR /src/src/OrderService
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]