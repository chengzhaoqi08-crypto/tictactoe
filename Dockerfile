# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Port binding is handled in Program.cs: PORT env var if set (Render/Fly/etc.),
# otherwise 8080. EXPOSE documents the default for plain `docker run`.
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicTacToe.dll"]
