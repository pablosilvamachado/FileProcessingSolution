FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FileProcessing.Domain/FileProcessing.Domain.csproj FileProcessing.Domain/
COPY FileProcessing.Application/FileProcessing.Application.csproj FileProcessing.Application/
COPY FileProcessing.Infrastructure/FileProcessing.Infrastructure.csproj FileProcessing.Infrastructure/
COPY FileProcessing.Worker/FileProcessing.Worker.csproj FileProcessing.Worker/

RUN dotnet restore FileProcessing.Worker/FileProcessing.Worker.csproj

COPY FileProcessing.Domain/ FileProcessing.Domain/
COPY FileProcessing.Application/ FileProcessing.Application/
COPY FileProcessing.Infrastructure/ FileProcessing.Infrastructure/
COPY FileProcessing.Worker/ FileProcessing.Worker/

RUN dotnet publish FileProcessing.Worker/FileProcessing.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileProcessing.Worker.dll"]
