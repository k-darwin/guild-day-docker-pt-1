FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8280

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Proxy.Service.csproj", "./"]
RUN dotnet restore "Proxy.Service.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "Proxy.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Proxy.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_URLS="http://+:8280"
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Proxy.Service.dll"]
