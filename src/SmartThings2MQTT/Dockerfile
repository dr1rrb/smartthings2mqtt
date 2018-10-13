FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY SmartThings2MQTT/SmartThings2MQTT.csproj SmartThings2MQTT/
RUN dotnet restore SmartThings2MQTT/SmartThings2MQTT.csproj
COPY . .
WORKDIR /src/SmartThings2MQTT
RUN dotnet build SmartThings2MQTT.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SmartThings2MQTT.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SmartThings2MQTT.dll"]
