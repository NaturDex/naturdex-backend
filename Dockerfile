FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["NaturDex.Api/NaturDex.Api.csproj", "NaturDex.Api/"]
COPY ["NaturDex.Core/NaturDex.Core.csproj", "NaturDex.Core/"]

RUN dotnet restore "NaturDex.Api/NaturDex.Api.csproj"

COPY . .

RUN dotnet build "NaturDex.Api/NaturDex.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NaturDex.Api/NaturDex.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
CMD ["dotnet", "NaturDex.Api.dll"]