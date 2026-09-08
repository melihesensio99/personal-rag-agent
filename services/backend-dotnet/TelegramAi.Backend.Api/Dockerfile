FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY services/backend-dotnet/TelegramAi.slnx ./services/backend-dotnet/TelegramAi.slnx
COPY services/backend-dotnet/TelegramAi.Backend.Api/TelegramAi.Backend.Api.csproj ./services/backend-dotnet/TelegramAi.Backend.Api/
COPY services/backend-dotnet/TelegramAi.Backend.Application/TelegramAi.Backend.Application.csproj ./services/backend-dotnet/TelegramAi.Backend.Application/
COPY services/backend-dotnet/TelegramAi.Backend.Domain/TelegramAi.Backend.Domain.csproj ./services/backend-dotnet/TelegramAi.Backend.Domain/
COPY services/backend-dotnet/TelegramAi.Backend.Infrastructure/TelegramAi.Backend.Infrastructure.csproj ./services/backend-dotnet/TelegramAi.Backend.Infrastructure/
RUN dotnet restore services/backend-dotnet/TelegramAi.slnx
COPY services/backend-dotnet/TelegramAi.Backend.Api/. ./services/backend-dotnet/TelegramAi.Backend.Api/
COPY services/backend-dotnet/TelegramAi.Backend.Application/. ./services/backend-dotnet/TelegramAi.Backend.Application/
COPY services/backend-dotnet/TelegramAi.Backend.Domain/. ./services/backend-dotnet/TelegramAi.Backend.Domain/
COPY services/backend-dotnet/TelegramAi.Backend.Infrastructure/. ./services/backend-dotnet/TelegramAi.Backend.Infrastructure/
RUN dotnet publish services/backend-dotnet/TelegramAi.Backend.Api/TelegramAi.Backend.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TelegramAi.Backend.Api.dll"]
