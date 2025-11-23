FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./FileProcessing.API/FileProcessing.API.csproj FileProcessing.API/
COPY ./FileProcessing.Domain/FileProcessing.Domain.csproj FileProcessing.Domain/
COPY ./FileProcessing.Infrastructure/FileProcessing.Infrastructure.csproj FileProcessing.Infrastructure/
COPY ./FileProcessing.Messaging/FileProcessing.Messaging.csproj FileProcessing.Messaging/

RUN dotnet restore FileProcessing.API/FileProcessing.API.csproj

COPY ./src/ .
RUN dotnet build FileProcessing.API/FileProcessing.API.csproj -c Release -o /app/build
RUN dotnet publish FileProcessing.API/FileProcessing.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileProcessing.API.dll"]
