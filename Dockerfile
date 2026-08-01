FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["GrabAndGo/GrabAndGo.Api.csproj", "GrabAndGo/"]
COPY ["GrabAndGo.Services/GrabAndGo.Services.csproj", "GrabAndGo.Services/"]
COPY ["GrabAndGo.DataAccess/GrabAndGo.DataAccess.csproj", "GrabAndGo.DataAccess/"]
COPY ["GrabAndGo.Models/GrabAndGo.Models.csproj", "GrabAndGo.Models/"]

RUN dotnet restore "GrabAndGo/GrabAndGo.Api.csproj"

COPY . .

RUN dotnet publish "GrabAndGo/GrabAndGo.Api.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "GrabAndGo.Api.dll"]