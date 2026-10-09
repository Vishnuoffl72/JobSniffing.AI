FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["PulseJob.Api.csproj","./"]
RUN dotnet restore "PulseJob.Api.csproj"

COPY . .
RUN dotnet publish "PulseJob.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

ENTRYPOINT ["dotnet", "PulseJob.Api.dll"]