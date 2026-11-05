# ЛР5, додаткове завдання: образ Docker для розгортання GameStudio.REST на Render (або будь-якому хостингу контейнерів).

# ---------- збирання ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# спершу лише файли проєктів: відновлення пакетів кешується, поки вони не змінюються
COPY GameStudio.Common/GameStudio.Common.csproj GameStudio.Common/
COPY GameStudio.Infrastructure/GameStudio.Infrastructure.csproj GameStudio.Infrastructure/
COPY GameStudio.REST/GameStudio.REST.csproj GameStudio.REST/
RUN dotnet restore GameStudio.REST/GameStudio.REST.csproj

COPY GameStudio.Common/ GameStudio.Common/
COPY GameStudio.Infrastructure/ GameStudio.Infrastructure/
COPY GameStudio.REST/ GameStudio.REST/
RUN dotnet publish GameStudio.REST/GameStudio.REST.csproj -c Release -o /app/publish --no-restore

# ---------- запуск ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# база даних SQLite з репозиторію (ЛР3); під час запуску застосунок застосовує міграції та створює ролі й адміністратора
COPY --chown=app:app Database/gamestudio.db Database/gamestudio.db
RUN chown app:app /app/Database

ENV ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__GameStudio="Data Source=/app/Database/gamestudio.db" \
    PORT=10000

# застосунок працює від непривілейованого користувача app
USER app
EXPOSE 10000

# Render передає номер порту в змінній PORT; ASP.NET Core слухає його через ASPNETCORE_HTTP_PORTS
CMD ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT} exec dotnet GameStudio.REST.dll"]
