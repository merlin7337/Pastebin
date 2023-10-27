FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Pastebin/Pastebin.csproj", "Pastebin/"]
RUN dotnet restore "Pastebin/Pastebin.csproj"
COPY . .
WORKDIR "/src/Pastebin"
RUN dotnet build "Pastebin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pastebin.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pastebin.dll"]
