FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app/src
# copy csproj only so restored project will be cached
COPY src/HomeAssistantUnifiLed/HomeAssistantUnifiLed.csproj /app/src/HomeAssistantUnifiLed/
COPY src/dotnet-homeassistant-mqtt-discovery/src/HomeAssistantDiscoveryHelper/HomeAssistantDiscoveryHelper.csproj src/dotnet-homeassistant-mqtt-discovery/src/HomeAssistantDiscoveryHelper/

RUN dotnet restore HomeAssistantUnifiLed/HomeAssistantUnifiLed.csproj
COPY src/ /app/src
RUN dotnet publish -c Release HomeAssistantUnifiLed/HomeAssistantUnifiLed.csproj -o /app/build

FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app/build/ ./
ENTRYPOINT ["dotnet", "HomeAssistantUnifiLed.dll"]