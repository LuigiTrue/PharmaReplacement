FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RepyPharma.csproj", "."]
RUN dotnet restore "RepyPharma.csproj"
COPY . .
RUN dotnet publish "RepyPharma.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/storage
ENTRYPOINT ["sh", "-c", "dotnet RepyPharm.dll --urls http://0.0.0.0:${PORT:-8080}"]
