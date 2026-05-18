FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NexInvoice.slnx", "./"]
COPY ["src/NexInvoice.API/NexInvoice.API.csproj", "src/NexInvoice.API/"]
COPY ["src/NexInvoice.Application/NexInvoice.Application.csproj", "src/NexInvoice.Application/"]
COPY ["src/NexInvoice.Domain/NexInvoice.Domain.csproj", "src/NexInvoice.Domain/"]
COPY ["src/NexInvoice.Infrastructure/NexInvoice.Infrastructure.csproj", "src/NexInvoice.Infrastructure/"]
RUN dotnet restore "src/NexInvoice.API/NexInvoice.API.csproj"

COPY ["src", "src"]
RUN dotnet publish "src/NexInvoice.API/NexInvoice.API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NexInvoice.API.dll"]
