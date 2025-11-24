FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only required csproj files
COPY FileProcessing.Domain/FileProcessing.Domain.csproj FileProcessing.Domain/
COPY FileProcessing.Application/FileProcessing.Application.csproj FileProcessing.Application/
COPY FileProcessing.Infrastructure/FileProcessing.Infrastructure.csproj FileProcessing.Infrastructure/
COPY FileProcessing.API/FileProcessing.API.csproj FileProcessing.API/

RUN dotnet restore FileProcessing.API/FileProcessing.API.csproj

# Copy only the project directories (not the entire src)
COPY FileProcessing.Domain/ FileProcessing.Domain/
COPY FileProcessing.Application/ FileProcessing.Application/
COPY FileProcessing.Infrastructure/ FileProcessing.Infrastructure/
COPY FileProcessing.API/ FileProcessing.API/

RUN dotnet publish FileProcessing.API/FileProcessing.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileProcessing.API.dll"]
