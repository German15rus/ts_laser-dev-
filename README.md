# TS Laser CRM (C# Backend)

Новый backend переписан на **ASP.NET Core (.NET 10)** с сохранением API-контракта для текущего frontend.

## Что сделано

- Перенесены API-маршруты из FastAPI в ASP.NET Core.
- Добавлен EF Core + миграции (`InitialCreate`).
- Сохранена cookie-auth модель (`tslaser_session`).
- Сохранены существующие HTML/JS шаблоны и URL страниц.
- Реализован экспорт CSV/XLSX.
- Добавлен импорт данных из legacy SQLite (`--import-legacy`).
- Проведена очистка структуры репозитория (артефакты Python/macOS).

## Структура

- `TsLaser.Crm.slnx` — решение
- `src/TsLaser.Crm.Api` — backend
- `src/TsLaser.Crm.Api/Templates` — HTML templates (legacy)
- `src/TsLaser.Crm.Api/wwwroot/static` — статические assets
- `docs/api-contract.md` — зафиксированный контракт API

## Быстрый старт

```bash
dotnet restore
dotnet build TsLaser.Crm.slnx
```

Запуск API:

```bash
dotnet run --project src/TsLaser.Crm.Api/TsLaser.Crm.Api.csproj -- --urls http://127.0.0.1:5080
```

## Импорт legacy данных

```bash
dotnet run --project src/TsLaser.Crm.Api/TsLaser.Crm.Api.csproj -- --import-legacy legacy_python_backend/tslaser.db.backup
```

## Конфигурация

`src/TsLaser.Crm.Api/appsettings.json`:

- `ConnectionStrings:DefaultConnection`
- `Auth:Password` (для dev)
- `Auth:PasswordHash` (рекомендуется для production, BCrypt)

## Примечание

Согласно вашему решению, пункты 8 и 9 (тестовый контур и релизный rollout-процесс) не реализовывались в этой итерации.
