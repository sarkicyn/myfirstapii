# MyApiBlya

MyApiBlya — ASP.NET Core Web API для аутентификации и управления пользователями. API поддерживает регистрацию и вход по логину и паролю, JWT и refresh-токены, OAuth через Google и GitHub, административное управление пользователями и журнал действий.

## Возможности

- регистрация и вход по логину и паролю;
- короткоживущие JWT и ротация refresh-токенов;
- OAuth-вход через Google и GitHub;
- отдельный вход администратора;
- просмотр и переименование профиля;
- история действий пользователя;
- просмотр, блокировка, разблокировка и удаление пользователей администратором;
- временная блокировка пользователя с указанием причины;
- PostgreSQL и миграции Entity Framework Core;
- ограничение частоты запросов по IP и пользователю;
- единая обработка ошибок и журналирование HTTP-запросов;
- health-check, Prometheus-метрики и OpenTelemetry-трассировка;
- Swagger UI в окружении `Development`;
- Docker Compose для локального запуска API и PostgreSQL;
- тесты xUnit, включая интеграционные тесты с PostgreSQL Testcontainers.

## Технологический стек

| Область | Технологии |
| --- | --- |
| Backend | C#, .NET 10, ASP.NET Core Web API |
| База данных | PostgreSQL 17 |
| ORM | Entity Framework Core, Npgsql |
| Аутентификация | JWT, refresh-токены, BCrypt, Google OAuth, GitHub OAuth |
| Кэш | ASP.NET Core `IMemoryCache` |
| Почта | MailKit, SMTP |
| Наблюдаемость | OpenTelemetry, OTLP, Prometheus, ASP.NET Core Health Checks |
| API-документация | Swagger / OpenAPI |
| Контейнеризация | Docker, Docker Compose |
| Тесты | xUnit, Moq, EF Core InMemory, Testcontainers PostgreSQL |

## Структура проекта

```text
.
|-- helpers/                 # ServiceResult, пагинация, ключи кэша и общие сообщения
|-- Controllers/            # HTTP-контроллеры API
|-- Data/                   # AppDbContext
|-- DTOs/                   # модели запросов, ответов и настройки SMTP
|-- Entities/               # сущности EF Core и конфигурация связей
|-- Middlewares/            # correlation ID, проверка путей и журналирование запросов
|-- Migrations/             # миграции EF Core и snapshot модели
|-- MyApiBlya.Tests/        # модульные и интеграционные тесты
|-- Services/               # бизнес-логика, токены, OAuth, почта и фоновые задачи
|-- Program.cs              # регистрация сервисов и HTTP pipeline
|-- docker-compose.yml      # локальные контейнеры API и PostgreSQL
|-- dockerfile              # сборка образа API
`-- MyApiBlya.csproj
```

## Требования

Для запуска через Docker нужны:

- Docker Desktop или Docker Engine;
- Docker Compose v2 (`docker compose`);
- свободные порты `5063` для API и `5432` для PostgreSQL.

Для запуска без Docker дополнительно нужны .NET 10 SDK и локальный PostgreSQL.

## Переменные окружения

ASP.NET Core преобразует двойное подчёркивание `__` во вложенный ключ конфигурации. Например, `Jwt__Key` соответствует `Jwt:Key`.

`docker-compose.yml` читает следующие значения из файла `.env` в корне проекта:

| Переменная | Назначение |
| --- | --- |
| `Database` | имя базы PostgreSQL |
| `Username` | пользователь PostgreSQL |
| `Password` | пароль PostgreSQL |
| `JWT_KEY` | секретный ключ подписи JWT |
| `GoogleClientId` | Google OAuth client ID |
| `GoogleClientSecret` | Google OAuth client secret |
| `GitHubClientId` | GitHub OAuth client ID |
| `GitHubClientSecret` | GitHub OAuth client secret |
| `ADMIN_LOGIN` | логин администратора |
| `ADMIN_PASSWORD_HASH` | BCrypt-хеш пароля администратора |

Настройки SMTP задаются ключами `Smtp__Host`, `Smtp__Port`, `Smtp__Email`, `Smtp__Password` и `Smtp__DisplayName`. Секреты следует хранить в переменных окружения, ASP.NET Core User Secrets или менеджере секретов, а не в отслеживаемых Git файлах.

> Никогда не добавляйте `.env` в Git. Файл уже исключён через `.gitignore`.

## Локальный запуск через Docker Compose

Все команды ниже выполняются из каталога проекта:

```powershell
cd C:\Users\User\Desktop\asp.net\MyApiBlya
```

### 1. Создайте `.env`

В корне проекта создайте файл `.env` со своими значениями:

```env
Database=myapi
Username=postgres
Password=replace_with_database_password

JWT_KEY=replace_with_a_long_random_secret_key

GoogleClientId=replace_with_google_client_id
GoogleClientSecret=replace_with_google_client_secret
GitHubClientId=replace_with_github_client_id
GitHubClientSecret=replace_with_github_client_secret

ADMIN_LOGIN=admin
ADMIN_PASSWORD_HASH='replace_with_bcrypt_hash'
```

Важно:

- `JWT_KEY` должен быть длинным случайным секретом;
- `ADMIN_PASSWORD_HASH` содержит BCrypt-хеш, а не пароль в открытом виде;
- одинарные кавычки вокруг BCrypt-хеша защищают символы `$` от подстановки Docker Compose;
- для реального OAuth настройте у провайдеров callback URL для локального API;
- если OAuth пока не проверяется, всё равно задайте непустые значения, поскольку приложение проверяет конфигурацию провайдеров при запуске.

Локальные callback URL:

```text
Google: http://localhost:5063/google-path
GitHub: http://localhost:5063/github-path
```

### 2. Проверьте итоговую конфигурацию Compose

Команда проверяет YAML и подстановку обязательных переменных, но не запускает контейнеры:

```powershell
docker compose config --quiet
```

Если `JWT_KEY` отсутствует, Compose завершится с сообщением `JWT_KEY is required`.

### 3. Соберите и запустите контейнеры

```powershell
docker compose up -d --build
```

Будут запущены:

- `postgres` — PostgreSQL 17 с постоянным Docker volume `database`;
- `myApi-backend` — API на порту `5063`.

API ждёт успешного health-check PostgreSQL. При старте приложение автоматически выполняет `Database.MigrateAsync()`, поэтому миграции EF Core применяются к контейнерной базе без отдельной команды `dotnet ef database update`.

### 4. Проверьте состояние контейнеров

```powershell
docker compose ps
```

Посмотреть журналы API и базы:

```powershell
docker compose logs -f api db
```

Для выхода из режима просмотра логов нажмите `Ctrl+C` — контейнеры продолжат работать.

### 5. Проверьте API

Health-check:

```text
http://localhost:5063/health
```

Проверка из PowerShell:

```powershell
Invoke-WebRequest http://localhost:5063/health
```

Swagger UI:

```text
http://localhost:5063/swagger
```

Swagger доступен, потому что `docker-compose.yml` запускает API с `ASPNETCORE_ENVIRONMENT=Development`. В `Production` Swagger кодом отключён.

### 6. Просмотрите метрики запросов

Prometheus-метрики доступны без авторизации по адресу:

```text
http://localhost:5063/metrics
```

Проверка из PowerShell:

```powershell
Invoke-WebRequest http://localhost:5063/metrics
```

Endpoint возвращает текст в формате Prometheus. В нём публикуются метрики ASP.NET Core, HTTP-клиента и .NET runtime. После нескольких запросов к API обновите `/metrics`, чтобы увидеть накопленные HTTP-метрики.

Для сбора Prometheus используйте target:

```yaml
scrape_configs:
  - job_name: myapiblya
    static_configs:
      - targets: ["host.docker.internal:5063"]
```

Если Prometheus работает в той же Docker-сети и обращается к сервису по имени Compose, target должен быть `api:5063`.

### 7. Остановите приложение

Остановить и удалить контейнеры, сохранив данные PostgreSQL:

```powershell
docker compose down
```

Запустить их снова:

```powershell
docker compose up -d
```

Удаление volume уничтожит локальные данные базы. Выполняйте эту команду только когда база больше не нужна:

```powershell
docker compose down -v
```

## Локальный запуск без Docker

1. Запустите PostgreSQL и создайте базу.
2. Задайте секреты через environment variables или User Secrets.
3. Укажите строку подключения.
4. Восстановите зависимости и запустите API.

Пример PowerShell:

```powershell
cd C:\Users\User\Desktop\asp.net\MyApiBlya
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=myapi;Username=postgres;Password=replace_me"
$env:JWT_KEY="replace_with_a_long_random_secret_key"
$env:Authentication__Google__ClientId="replace_me"
$env:Authentication__Google__ClientSecret="replace_me"
$env:Authentication__GitHub__ClientId="replace_me"
$env:Authentication__GitHub__ClientSecret="replace_me"
$env:ADMIN_LOGIN="admin"
$env:ADMIN_PASSWORD_HASH="replace_with_bcrypt_hash"
dotnet restore
dotnet run
```

Приложение само применяет миграции при старте. Не направляйте локальную конфигурацию на общую или production-базу без явного намерения изменить её схему.

## Основные API endpoints

| Метод | Endpoint | Доступ | Описание |
| --- | --- | --- | --- |
| `POST` | `/api/users/register` | Public | регистрация пользователя |
| `POST` | `/api/users/login` | Public | вход по логину и паролю |
| `POST` | `/api/users/refresh` | Public | ротация JWT и refresh-токена |
| `POST` | `/api/users/admin` | Public | вход администратора |
| `POST` | `/api/users/logout` | JWT | отзыв refresh-токена |
| `GET` | `/api/users/me` | JWT | профиль текущего пользователя |
| `PUT` | `/api/users/rename` | JWT | переименование текущего пользователя |
| `GET` | `/api/users/history` | JWT | история действий текущего пользователя |
| `GET` | `/api/users/{id}` | Admin JWT | пользователь по ID |
| `GET` | `/api/users/all` | Admin JWT | постраничный список пользователей |
| `DELETE` | `/api/users/deleteUser` | Admin JWT | удаление пользователя |
| `PUT` | `/api/users/blockUser` | Admin JWT | временная блокировка пользователя |
| `PUT` | `/api/users/UnblockUser` | Admin JWT | разблокировка пользователя |
| `GET` | `/api/users/login/google` | Public | начало Google OAuth |
| `GET` | `/api/users/login/github` | Public | начало GitHub OAuth |
| `GET` | `/health` | Public | состояние API |
| `GET` | `/metrics` | Public | Prometheus-метрики |

JWT передаётся стандартным заголовком:

```http
Authorization: Bearer <jwt-token>
```

## OAuth

Начало OAuth-входа:

```text
http://localhost:5063/api/users/login/google
http://localhost:5063/api/users/login/github
```

После успешной авторизации backend создаёт или находит пользователя, выдаёт JWT и refresh-токен и перенаправляет браузер на `/api/users/auth-complete`. Страница сохраняет токены в `sessionStorage` текущей вкладки и позволяет скопировать их.

## Миграции EF Core

Создать миграцию:

```powershell
dotnet ef migrations add DescriptiveMigrationName
```

Просмотреть список миграций:

```powershell
dotnet ef migrations list
```

Применить миграции вручную:

```powershell
dotnet ef database update
```

Обычный запуск приложения уже применяет ожидающие миграции автоматически.

## Тесты

```powershell
dotnet test MyApiBlya.Tests/MyApiBlya.Tests.csproj
```

Часть тестов использует PostgreSQL Testcontainers, поэтому для полного прогона должен работать Docker. Тестовый контейнер создаётся перед тестами, к нему применяются миграции, а после тестов он удаляется.

## Наблюдаемость

- `/health` — health-check приложения;
- `/metrics` — метрики в формате Prometheus;
- HTTP-запросы журналируются с методом, путём, статусом, IP и длительностью;
- для каждого запроса создаётся correlation ID в области логирования;
- трассировка ASP.NET Core, HTTP client и EF Core отправляется через OTLP на endpoint, заданный в `Program.cs`.

## Безопасность

- не храните реальные секреты в README, `.env.example` или отслеживаемых `appsettings*.json`;
- не публикуйте `.env`;
- используйте длинный случайный JWT-ключ;
- храните только BCrypt-хеш административного пароля;
- ограничьте публичный доступ к `/metrics` на уровне reverse proxy или сети при production-развёртывании;
- перед production-запуском вынесите секреты в отдельный secret manager;
- учитывайте, что запуск API автоматически применяет миграции к настроенной базе.

## Автор

Maintainer: `sarkicyn`
