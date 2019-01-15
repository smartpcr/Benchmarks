FROM microsoft/dotnet:2.1-sdk-stretch AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . .
RUN dotnet publish -c release -o out

FROM microsoft/dotnet:2.1-sdk-stretch AS driver
WORKDIR /driver

# copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . .
RUN dotnet publish -c release -o out

FROM microsoft/dotnet:2.1-aspnetcore-runtime-stretch-slim AS runtime
WORKDIR /app
COPY --from=build /app/out ./
COPY --from=driver /driver/out ./driver
ENTRYPOINT ["dotnet", "Benchmarks.UI.Server.dll", "--driverPath", "./"]
