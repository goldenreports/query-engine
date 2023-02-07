FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["GoldenReports.QueryEngine/GoldenReports.QueryEngine.csproj", "GoldenReports.QueryEngine/"]
RUN dotnet restore "GoldenReports.QueryEngine/GoldenReports.QueryEngine.csproj"
COPY . .
WORKDIR "/src/GoldenReports.QueryEngine"
RUN dotnet build "GoldenReports.QueryEngine.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GoldenReports.QueryEngine.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GoldenReports.QueryEngine.dll"]
