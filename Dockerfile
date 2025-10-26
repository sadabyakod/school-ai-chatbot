# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SchoolAiChatbotBackend/SchoolAiChatbotBackend.csproj", "SchoolAiChatbotBackend/"]
RUN dotnet restore "SchoolAiChatbotBackend/SchoolAiChatbotBackend.csproj"
COPY . .
WORKDIR "/src/SchoolAiChatbotBackend"
RUN dotnet build "SchoolAiChatbotBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SchoolAiChatbotBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SchoolAiChatbotBackend.dll"]