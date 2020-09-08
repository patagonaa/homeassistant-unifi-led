FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app/src
# copy csproj only so restored project will be cached
COPY src/HomeAssistantUnifiLed/HomeAssistantUnifiLed.csproj /app/src/HomeAssistantUnifiLed/
RUN dotnet restore HomeAssistantUnifiLed/HomeAssistantUnifiLed.csproj
COPY src/ /app/src
RUN dotnet publish -c Release HomeAssistantUnifiLed/HomeAssistantUnifiLed.csproj -o /app/build

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
COPY --from=build /app/build/ ./
ENTRYPOINT ["dotnet", "HomeAssistantUnifiLed.dll"]