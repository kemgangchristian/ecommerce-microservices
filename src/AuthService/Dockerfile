FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/AuthService/AuthService.csproj", "src/AuthService/"]
RUN dotnet restore "src/AuthService/AuthService.csproj"
COPY . .
WORKDIR /src/src/AuthService
RUN dotnet publish "AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuthService.dll"]