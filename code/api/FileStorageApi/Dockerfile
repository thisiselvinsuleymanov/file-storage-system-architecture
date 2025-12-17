FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FileStorageApi.csproj", "./"]
RUN dotnet restore "FileStorageApi.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "FileStorageApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileStorageApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileStorageApi.dll"]

