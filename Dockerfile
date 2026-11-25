FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ServicePractice.csproj ./
RUN dotnet restore

COPY . .

RUN dotnet publish ServicePractice.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /data && chown app:app /app /data
USER app

ENTRYPOINT ["dotnet", "ServicePractice.dll"]