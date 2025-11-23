FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./FileProcessing.Worker/FileProcessing.Worker.csproj FileProcessing.Worker/
COPY ./FileProcessing.Domain/FileProcessing.Domain.csproj FileProcessing.Domain/
COPY ./FileProcessing.Infrastructure/FileProcessing.Infrastructure.csproj FileProcessing.Infrastructure/
COPY ./FileProcessing.Messaging/FileProcessing.Messaging.csproj FileProcessing.Messaging/

RUN dotnet restore FileProcessing.Worker/FileProcessing.Worker.csproj

COPY ./src/ .
RUN dotnet build FileProcessing.Worker/FileProcessing.Worker.csproj -c Release -o /app/build
RUN dotnet publish FileProcessing.Worker/FileProcessing.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileProcessing.Worker.dll"]
