FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PaymentGateway.sln", "."]
COPY ["PaymentGateway.Api/PaymentGateway.Api.csproj", "PaymentGateway.Api/"]
COPY ["PaymentGateway.Core/PaymentGateway.Core.csproj", "PaymentGateway.Core/"]
RUN dotnet restore "PaymentGateway.sln"
COPY . .
RUN dotnet build "PaymentGateway.sln" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaymentGateway.Api/PaymentGateway.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5000 5001
ENTRYPOINT ["dotnet", "PaymentGateway.Api.dll"]