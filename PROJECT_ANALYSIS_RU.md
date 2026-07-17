# Анализ проекта MyApiBlya

Дата анализа: **13 июля 2026 года**  
Объект анализа: актуальный исходный проект `MyApiBlya`  
Исключены из основного анализа: исторические каталоги `buildcheck-*`, `bin/`, `obj/` и скомпилированные DLL.

## 1. Краткий вывод

`MyApiBlya` — backend на ASP.NET Core Web API для регистрации, аутентификации и управления пользователями. Проект реализует не только базовый CRUD, но и полноценный набор прикладных backend-механик:

- логин и регистрация по логину/паролю;
- BCrypt-хеширование паролей;
- короткоживущие JWT-токены и refresh-токены;
- ротация refresh-токенов;
- отдельный административный вход и роль `Admin`;
- Google OAuth и GitHub OAuth;
- профили пользователей, смена логина и история действий;
- административный просмотр, пагинация, блокировка, разблокировка и удаление пользователей;
- временная блокировка с причиной и сроком;
- кэширование пользователей и истории действий;
- middleware для correlation ID, HTTP-логирования и ограничения разрешенных путей;
- rate limiting по IP и идентификатору пользователя;
- фоновые задачи очистки истории и автоматической разблокировки;
- email-уведомления через SMTP/MailKit;
- PostgreSQL через Entity Framework Core/Npgsql;
- миграции EF Core;
- Swagger/OpenAPI;
- health-check, Prometheus-метрики и OpenTelemetry-трассировка;
- Docker/Docker Compose;
- модульные тесты и интеграционные тесты с PostgreSQL Testcontainers.

По набору технологий проект отражает навыки **уверенного Junior+/начинающего Middle C# backend-разработчика**. Самые сильные стороны — аутентификация, работа с EF Core/PostgreSQL, DI, middleware, тестирование, контейнеризация и наблюдаемость. До production-ready уровня требуется закрыть несколько важных проблем безопасности и надежности, перечисленных в разделе 14.

Оценка уровня — это оценка реализованных в репозитории практик, а не формальная аттестация разработчика.

## 2. Объективные метрики проекта

| Метрика | Значение | Откуда получено |
|---|---:|---|
| Target Framework | `.NET 10` | `MyApiBlya.csproj` |
| Production C# файлов | 51 | подсчет исходников без тестов, designer-файлов и snapshot |
| Production C# строк | около 3 340 | статический подсчет строк |
| Тестовых C# файлов | 9 | `MyApiBlya.Tests` |
| Тестовых строк | около 1 570 | статический подсчет строк |
| Контроллеров | 4 | `Controllers/` |
| Файлов сервисного слоя | 13 | `Services/` |
| Endpoint-методов | 18 | атрибуты `HttpGet/HttpPost/HttpPut/HttpDelete` |
| HTTP GET endpoints | 9 | атрибуты маршрутизации |
| HTTP POST endpoints | 5 | атрибуты маршрутизации |
| HTTP PUT endpoints | 3 | атрибуты маршрутизации |
| HTTP DELETE endpoints | 1 | атрибут маршрутизации |
| `Authorize`-атрибутов | 9 | контроллеры |
| `AllowAnonymous`-атрибутов | 9 | контроллеры |
| EF Core migration C# файлов | 32 | `Migrations/`, включая designer и snapshot |
| PackageReference в API | 18 | `MyApiBlya.csproj` |
| Тестов, обнаруженных test runner | 61 | фактический запуск `dotnet test` |
| Прошедших тестов без PostgreSQL suite | 56 | фактический запуск с фильтром |
| Тестов, заблокированных отсутствием Docker | 5 | `AdminUsersPostgresTests` |
| Release build errors | 0 | `dotnet build -c Release --no-restore` |
| Release build warnings | 2 | имя миграции `fix` в нижнем регистре |

### Результат проверки

```text
dotnet build MyApiBlya.csproj --no-restore -c Release
Сборка успешно завершена. Ошибок: 0. Предупреждений: 2.
```

```text
dotnet test MyApiBlya.Tests\MyApiBlya.Tests.csproj --no-restore
Всего: 61. Пройдено: 56. Не пройдено: 5.
```

Пять падений полного запуска связаны с тем, что `Testcontainers.PostgreSql` не смог подключиться к Docker Engine через `npipe://./pipe/docker_engine`. При исключении PostgreSQL suite остальные 56 тестов проходят:

```text
dotnet test ... --filter "FullyQualifiedName!~AdminUsersPostgresTests"
Пройдено: 56. Не пройдено: 0.
```

## 3. Архитектура и поток запроса

Проект построен как классический ASP.NET Core Web API с разделением на контроллеры, сервисы, persistence-слой и cross-cutting инфраструктуру.

```text
HTTP client
    |
    v
CorrelationId -> CORS -> Authentication -> Authorization -> RateLimiter
    |                                                        |
    +--> AllowedPathMiddleware -> RequestLoggingMiddleware --+
                                                                 v
                                                            Controllers
                                                                 |
                                                                 v
                                                              Services
                                                                 |
                                  +------------------------------+------------------+
                                  |                              |                  |
                                  v                              v                  v
                         EF Core / PostgreSQL             Google/GitHub OAuth    SMTP
                                  |
                                  v
                         User / Action / History

                    OpenTelemetry traces + Prometheus metrics + Console logs
```

### Основные слои

| Слой | Назначение | Основные файлы |
|---|---|---|
| Bootstrap / pipeline | регистрация DI, auth, telemetry, middleware и routes | [`Program.cs`](Program.cs) |
| Controllers | HTTP-контракты, status codes, авторизация endpoint-ов | [`Controllers/`](Controllers/) |
| Services | бизнес-логика пользователей, auth, токенов, OAuth, почты и аудита | [`Services/`](Services/) |
| Data | EF Core DbContext и маппинг моделей | [`Data/AppDbContext.cs`](Data/AppDbContext.cs) |
| Entities | persistent-модели пользователей и действий | [`Entities/`](Entities/) |
| DTOs | модели входных данных и ответов | [`DTOs/`](DTOs/) |
| Helpers | единый результат сервиса, mapping ошибок, cache keys, pagination | [`helpers/`](helpers/) |
| Middlewares | correlation ID, request logging, allow-list путей | [`Middlewares/`](Middlewares/) |
| Migrations | версионирование PostgreSQL-схемы | [`Migrations/`](Migrations/) |
| Tests | unit/controller/service и PostgreSQL integration tests | [`MyApiBlya.Tests/`](MyApiBlya.Tests/) |

## 4. Технологический стек

### Backend

- C# с `Nullable` и `ImplicitUsings`.
- ASP.NET Core Web API на .NET 10.
- Dependency Injection с регистрацией интерфейсов и реализаций.
- `async/await` и передача `CancellationToken` в большинство операций.
- Attribute routing, `ApiController`, model binding и стандартные HTTP-ответы.

### Data access

- Entity Framework Core 10.
- PostgreSQL 17.
- Npgsql Entity Framework Core provider.
- `DbContext`, LINQ, `AsNoTracking`, `FirstOrDefaultAsync`, `ToListAsync`, `CountAsync`.
- EF Core migrations и автоматическое применение миграций на старте приложения.

### Authentication и security

- `Microsoft.AspNetCore.Authentication.JwtBearer`.
- JWT с HMAC-SHA256.
- Claims: `NameIdentifier`, `Name`, `Role`.
- BCrypt через `BCrypt.Net-Next`.
- Refresh-токены на основе криптографического генератора случайных байтов и SHA-256 hash.
- Google OAuth через `Microsoft.AspNetCore.Authentication.Google`.
- GitHub OAuth через `AspNet.Security.OAuth.GitHub`.
- `Authorize`, `AllowAnonymous`, role-based authorization.
- Fixed-window rate limiting.
- CORS policy.

### Infrastructure и integration

- `IMemoryCache` для локального кэширования.
- MailKit/MimeKit для SMTP.
- `BackgroundService` для фоновых задач.
- Docker multi-stage build.
- Docker Compose для API и PostgreSQL.
- Health checks.
- Swagger/OpenAPI через Swashbuckle.
- OpenTelemetry traces и metrics.
- Prometheus scraping endpoint.

### Tests

- xUnit.
- Moq.
- EF Core InMemory.
- Testcontainers PostgreSQL.
- Microsoft.NET.Test.Sdk и Coverlet collector.

## 5. Реализованная функциональность

### Регистрация и логин

`AuthService` реализует:

1. Проверку отсутствующего DTO.
2. Проверку пустого логина и пароля.
3. Ограничение длины логина/пароля.
4. Проверку допустимых символов логина через регулярное выражение.
5. Проверку занятости логина при регистрации.
6. BCrypt-хеширование пароля.
7. Создание пользователя с ролью `User`.
8. Создание refresh-токена и сохранение его hash.
9. Генерацию JWT.
10. Добавление действия в историю.
11. Отправку приветственного email.

При входе дополнительно проверяются существование пользователя, BCrypt-пароль и блокировка аккаунта.

### JWT

В [`JwtService.cs`](Services/JwtService.cs) реализованы два типа токена:

- пользовательский токен с ролью `User`;
- административный токен с ролью `Admin`.

В токен помещаются:

- идентификатор пользователя;
- логин;
- роль;
- audience `MyClients`;
- срок действия 3 минуты;
- HMAC-SHA256 подпись.

В `Program.cs` включены проверки подписи, audience и lifetime. `ClockSkew` установлен в `TimeSpan.Zero`.

### Refresh-токены

В [`RefreshTokenService.cs`](Services/RefreshTokenService.cs) реализован следующий механизм:

- генерируется 64 случайных байта;
- значение кодируется в Base64;
- в базе хранится SHA-256 hash refresh-токена;
- срок действия устанавливается на 7 дней;
- при refresh генерируется новый JWT и новый refresh-токен.

То есть в проекте заложена модель rotation, а не бессрочного повторного использования одного refresh-токена.

### Административный контур

Администратор аутентифицируется через отдельный endpoint `/api/users/admin`. Логин администратора и BCrypt-хеш пароля берутся из конфигурации `ADMIN_LOGIN` и `ADMIN_PASSWORD_HASH`.

После успешного входа:

- пользователь Admin создается в базе, если его еще нет;
- генерируются admin JWT и refresh-токен;
- действие записывается в history;
- кэш пользователя инвалидируется.

Административные endpoints защищены одновременно JWT и ролью `Admin`.

### OAuth

Реализованы два OAuth-потока:

- `/api/users/login/google` → Google callback → поиск/создание пользователя → JWT + refresh;
- `/api/users/login/github` → GitHub callback → поиск/создание пользователя → JWT + refresh.

Для OAuth используется временная cookie-схема `sexScheme`. После callback приложение:

1. извлекает `ClaimsPrincipal`;
2. находит пользователя по provider + provider user id;
3. создает пользователя при первом входе;
4. проверяет блокировку;
5. создает refresh-токен;
6. выдает JWT;
7. записывает действие;
8. выполняет sign out временной cookie-схемы;
9. перенаправляет на `/api/users/auth-complete`.

`auth-complete` содержит встроенную HTML/JavaScript-страницу, которая читает токены из URL fragment, сохраняет их в `sessionStorage` и позволяет скопировать их.

### Профиль пользователя

Реализованы операции:

- получение текущего профиля `/api/users/me`;
- смена логина `/api/users/rename`;
- получение истории `/api/users/history`;
- logout с очисткой refresh token hash.

Для защищенных действий применяется `ActiveUserFilter`, который проверяет, существует ли пользователь, и не заблокирован ли он.

### Управление пользователями администратором

Администратор может:

- получить пользователя по ID;
- получить список пользователей с пагинацией;
- удалить пользователя;
- заблокировать пользователя на заданное количество минут/часов/дней;
- указать причину блокировки;
- разблокировать пользователя;
- получать audit history действий.

После изменений кэш пользователя инвалидируется.

### История действий

История реализована через нормализованные таблицы:

- `UserAction` — справочник уникальных текстовых действий;
- `UserActionHistory` — факт действия пользователя с timestamp;
- `User` — пользователь и навигационная коллекция истории.

Записываются действия входа, регистрации, refresh-токенов, logout, просмотра профиля, просмотра пользователей, удаления, блокировки, разблокировки и смены имени.

### Фоновые задачи

[`BackgroundLoggingService.cs`](Services/BackgroundLoggingService.cs) выполняет две задачи:

- удаление старых записей истории;
- автоматическую разблокировку пользователей после истечения `BlockedUntill`.

История удаляется, если она старше одного дня. Это означает, что реализована не бессрочная audit history, а короткое окно хранения.

## 6. API endpoints

Все endpoint-ы используют общий префикс `/api/users`.

| Метод | Endpoint | Доступ | Назначение |
|---|---|---|---|
| `POST` | `/api/users/register` | public, IP limit | регистрация пользователя |
| `POST` | `/api/users/login` | public, IP limit | вход по логину и паролю |
| `POST` | `/api/users/refresh` | public, IP limit | обновление JWT и refresh-токена |
| `POST` | `/api/users/admin` | public, IP limit | вход администратора |
| `POST` | `/api/users/logout` | JWT, active user | выход и отзыв refresh-токена |
| `GET` | `/api/users/me` | JWT, active user | профиль текущего пользователя |
| `PUT` | `/api/users/rename` | JWT, active user | смена логина текущего пользователя |
| `GET` | `/api/users/history` | JWT, active user | история действий текущего пользователя |
| `GET` | `/api/users/{id}` | Admin JWT, active user | пользователь по ID |
| `GET` | `/api/users/all` | Admin JWT, active user | список пользователей с пагинацией |
| `DELETE` | `/api/users/deleteUser` | Admin JWT, active user | удаление пользователя |
| `PUT` | `/api/users/blockUser` | Admin JWT, active user | временная блокировка |
| `PUT` | `/api/users/UnblockUser` | Admin JWT, active user | разблокировка |
| `GET` | `/api/users/login/google` | public | начало Google OAuth |
| `GET` | `/api/users/google-callback` | public | Google OAuth callback |
| `GET` | `/api/users/login/github` | public | начало GitHub OAuth |
| `GET` | `/api/users/github-callback` | public | GitHub OAuth callback |
| `GET` | `/api/users/auth-complete` | public | страница завершения OAuth и передачи токенов |

Для части OAuth endpoints установлен `ApiExplorerSettings(IgnoreApi = true)`, поэтому они скрыты из Swagger-документации.

## 7. Модель данных

### Таблица users

Модель [`User.cs`](Entities/User.cs) содержит:

- `Id`;
- `Login`;
- `Password` — BCrypt hash для локальной учетной записи;
- `Email`;
- `Role`;
- `RefreshTokenHash`;
- `RefreshTokenExpiresAt`;
- `Provider` и `ProviderUserId` для OAuth;
- `CreatedAt`;
- `IsBlocked`;
- `BlockedUntill`;
- `Cause`;
- навигационные коллекции истории.

В `AppDbContext` настроены:

- уникальный индекс по `Login`;
- уникальный индекс по `Id`;
- уникальный индекс по `ProviderUserId`;
- индексы по тексту действия;
- названия таблиц и колонок;
- связи user → histories и action → histories;
- default `IsBlocked = false`.

### Таблицы действий

`UserAction` хранит справочник действий, а `UserActionHistory` связывает пользователя с действием и фиксирует время выполнения.

### Миграции

В проекте присутствует 32 migration-related C# файла и `AppDbContextModelSnapshot`. История миграций показывает последовательное развитие модели: пользователи, блокировка, refresh token, история, переименование классов, permissions и API-изменения.

При запуске приложения вызывается:

```csharp
await db.Database.MigrateAsync();
```

Это автоматически применяет миграции к базе данных до регистрации маршрутов.

## 8. Кэширование

Используется `IMemoryCache`.

Кэшируются:

- пользователь по ID — примерно 3 минуты;
- факт отсутствия пользователя — примерно 15 секунд;
- текущий пользователь — примерно 50 секунд;
- история пользователя — примерно 100 секунд.

Для ключей используется [`CacheKeys.cs`](helpers/CacheKeys.cs). После изменения пользователя кэш инвалидируется в нескольких сервисах и контроллерах.

Плюс такого подхода — снижение числа одинаковых запросов в БД. Ограничение — `IMemoryCache` локален конкретному процессу. При нескольких репликах API значения не синхронизируются, поэтому для production-scale сценария потребуется Redis или другой распределенный cache.

## 9. Метрики, логирование и наблюдаемость

### Технические метрики

| Механизм | Реализация | Что можно получить |
|---|---|---|
| Health check | `AddHealthChecks`, `/health` | состояние endpoint приложения |
| Prometheus | `AddPrometheusExporter`, `/metrics` | HTTP, runtime и client metrics в формате Prometheus |
| OpenTelemetry traces | ASP.NET Core instrumentation | длительность и дерево серверных запросов |
| HTTP client instrumentation | OpenTelemetry | внешние HTTP-вызовы |
| EF Core instrumentation | OpenTelemetry | трассировку запросов EF Core |
| Runtime instrumentation | OpenTelemetry metrics | runtime/.NET показатели |
| Request logging | `RequestLoggingMiddleware` | IP, method, path, status code, duration ms |
| Correlation scope | `CorrelationIdMiddleware` | correlation ID в logging scope |
| Console logging | `ClearProviders` + `AddConsole` | централизованный stdout/stderr поток |

OTLP exporter сейчас настроен на `http://localhost:4317`. В Docker это означает localhost внутри контейнера API, поэтому endpoint collector нужно вынести в конфигурацию и указать сетевое имя collector-сервиса.

### Бизнес-метрики и audit metrics

Явных custom counters/histograms для регистрации, логина, количества блокировок, OAuth или ошибок в коде не обнаружено. Вместо этого есть:

- история действий в PostgreSQL;
- количество пользователей через `TotalCount` в пагинации;
- HTTP/runtime/EF Core telemetry;
- логи событий с structured logging placeholders.

То есть техническая observability реализована, а прикладные метрики уровня бизнеса пока представлены audit history и логами, но не отдельными Prometheus counters.

### Важная особенность health-check

Вызван `AddHealthChecks()`, но отдельный `AddNpgSql(...)` или другой DB health check не зарегистрирован. Поэтому `/health` подтверждает, что приложение отвечает, но не обязательно подтверждает доступность PostgreSQL в момент проверки.

## 10. Безопасность: реализованные практики

Хорошие практики, присутствующие в коде:

- пароли не хранятся в открытом виде, используется BCrypt;
- refresh token хранится в виде hash;
- refresh token генерируется через `RandomNumberGenerator`;
- JWT имеет короткий срок жизни;
- проверяются подпись, audience и срок действия JWT;
- административные endpoints защищены ролью `Admin`;
- есть отдельная проверка заблокированного пользователя;
- используется `CancellationToken` для многих запросов к БД и внешних операций;
- включен rate limiting;
- глобальный exception handler не возвращает клиенту stack trace;
- секреты для JWT, OAuth и admin password предусмотрены в environment variables / Docker Compose;
- `.env` включен в `.gitignore`.

## 11. Тестирование

Тестовый проект показывает, что разработчик умеет проверять не только happy path.

Покрытые направления:

- регистрация и валидация входных данных;
- логин с корректными и некорректными данными;
- заблокированный пользователь;
- refresh token: пустой, неизвестный, истекший и корректный;
- refresh для обычного пользователя и администратора;
- OAuth controller responses;
- logout;
- профиль и история пользователя;
- административные операции;
- PostgreSQL integration tests через Testcontainers.

Используются два уровня тестов:

1. Unit/controller tests с Moq и EF Core InMemory.
2. Integration tests с PostgreSQL 17 в Testcontainers.

Практический результат текущего окружения: 56 тестов проходят, а PostgreSQL suite требует работающий Docker Engine.

## 12. Containerization и запуск

### Dockerfile

[`dockerfile`](dockerfile) использует multi-stage build:

1. SDK image `.NET 10` для restore/publish.
2. ASP.NET runtime image `.NET 10` для запуска.
3. В runtime копируется только publish output.
4. Запуск выполняется под `$APP_UID`, то есть предусмотрен non-root runtime.

### Docker Compose

[`docker-compose.yml`](docker-compose.yml) поднимает:

- `api` на порту `5063`;
- `postgres:17` на порту `5432`;
- named volume `database`;
- health check PostgreSQL через `pg_isready`;
- health check API через `/health`;
- `depends_on` с условием `service_healthy` для БД;
- передачу JWT, OAuth, admin credentials и database connection string через environment variables.

`docker-compose.prod.yml` сейчас пустой, поэтому полноценный production Compose-профиль еще не оформлен.

## 13. Навыки C#/.NET-разработчика, которые отражает проект

### 13.1 ASP.NET Core Web API

Проект демонстрирует умение:

- строить REST API;
- использовать attribute routing;
- управлять status codes;
- применять `ApiController` и model validation;
- разделять публичные и защищенные routes;
- использовать `IActionResult` и typed service results;
- создавать собственный validation response через `InvalidModelStateResponseFactory`;
- подключать Swagger/OpenAPI.

### 13.2 Dependency Injection и абстракции

Сервисы подключаются через интерфейсы:

- `IAuthService`;
- `IUserService`;
- `IOAuthService`;
- `IJwtTokenService`;
- `IRefreshTokenService`;
- `IPasswordHashService`;
- `IUserActionService`;
- `INotificationService`;
- `IGoogleUserService`;
- `IGitHubUserService`.

Это показывает понимание DI, слабой связанности и возможности подменять зависимости в тестах.

### 13.3 Асинхронность

Большинство операций I/O сделано асинхронно:

- EF Core queries;
- сохранение данных;
- отправка email;
- OAuth callback;
- генерация и сохранение токенов;
- background operations.

В методы передается `CancellationToken`, что отражает понимание отмены HTTP-запросов и фоновых задач.

### 13.4 Entity Framework Core

Проект демонстрирует:

- создание `DbContext`;
- Fluent API mapping;
- индексы и unique constraints;
- navigation properties;
- relationships;
- migrations;
- `AsNoTracking` для read-only запросов;
- LINQ pagination через `Skip/Take`;
- автоматическое применение миграций.

### 13.5 Authentication и authorization

Отражены практические навыки:

- JWT bearer pipeline;
- claims и роли;
- custom authentication events;
- OAuth providers;
- refresh-token lifecycle;
- отдельная admin authentication flow;
- блокировка аккаунтов;
- action filter для проверки активного пользователя.

### 13.6 Security engineering

Проект демонстрирует понимание базовых security controls:

- BCrypt вместо хранения паролей;
- hash refresh token;
- криптографически стойкая генерация токенов;
- короткий TTL JWT;
- rate limiting;
- запрет неразрешенных путей;
- отсутствие stack trace в ответе API;
- конфигурация секретов через environment variables.

### 13.7 Middleware и cross-cutting concerns

Реализованы собственные middleware для:

- correlation ID;
- structured request logging;
- контроля allow-list путей.

Это хороший показатель понимания pipeline-модели ASP.NET Core.

### 13.8 Caching

В коде есть:

- именованные cache keys;
- cache-aside чтение;
- negative caching для отсутствующих пользователей;
- TTL;
- cache invalidation после изменения данных.

Отдельно заметно понимание, что после mutation кэш нужно удалять.

### 13.9 Background processing

`BackgroundService` показывает умение:

- запускать длительную фоновую работу;
- создавать scoped dependency через `IServiceScopeFactory`;
- использовать `CancellationToken` для graceful shutdown;
- выполнять периодические операции с EF Core.

### 13.10 Integration skills

Проект интегрируется с:

- PostgreSQL;
- Google OAuth;
- GitHub OAuth;
- SMTP;
- Prometheus;
- OpenTelemetry collector;
- Docker.

Для C#-разработчика это показывает опыт не только isolated code, но и сборки работающей backend-системы вокруг внешних зависимостей.

### 13.11 Testing

Отражены навыки:

- xUnit assertions;
- Moq setups/verifications;
- проверка ошибок и status codes;
- тестирование service result;
- InMemory EF Core для быстрых тестов;
- Testcontainers для приближенного к production PostgreSQL.

### 13.12 DevOps и observability

Есть опыт:

- multi-stage Docker build;
- запуска под non-root пользователем;
- Compose health checks;
- readiness dependency через `depends_on`;
- миграций при старте;
- Prometheus endpoint;
- OpenTelemetry traces/metrics;
- structured console logging.

## 14. Найденные риски и зоны роста

Ниже перечислены именно наблюдения по текущему коду, а не абстрактные рекомендации.

### Критический приоритет

#### 1. Секрет SMTP хранится в конфигурации

В `appsettings.json` и `appsettings.Development.json` обнаружены значения SMTP password. Даже если репозиторий локальный, это credential exposure.

Что сделать:

- немедленно отозвать/перевыпустить SMTP app password;
- удалить секрет из tracked/config files;
- хранить его только в environment variables, User Secrets или secret manager;
- проверить логи и историю файлов;
- не включать значение в отчеты, commit messages и issue tracker.

#### 2. Пароль SMTP пишется в лог

В [`emailService.cs`](Services/emailService.cs) присутствует логирование `_options.Password` через `LogWarning`. Это позволяет получить credential из логов даже после переноса конфигурации в environment.

Что сделать: полностью удалить логирование SMTP password и заменить его на безопасный диагностический признак, например host/port без credential.

#### 3. Refresh rotation после refresh-запроса может не сохраняться

В `AuthService.RefreshAllTokens` пользователь загружается через `AsNoTracking()`. Затем новые значения передаются в `SaveRefreshTokenAsync`, но объект остается detached, а явного `Attach/Update` и `SaveChangesAsync` для этой сущности в ветке refresh нет.

Следствие: API может вернуть новый refresh-токен, но его новый hash не будет записан в PostgreSQL. Это ломает ожидаемую rotation-семантику и может приводить к повторному использованию старого значения.

Что сделать:

- загружать пользователя tracked без `AsNoTracking`, либо;
- явно прикреплять entity и помечать refresh-поля modified;
- явно сохранять изменения в транзакции;
- добавить интеграционный тест, который после refresh читает пользователя заново из БД и проверяет новый hash.

#### 4. JWT и refresh token передаются через URL fragment и sessionStorage

OAuth flow формирует redirect с `#jwt=...&refresh=...`, а HTML сохраняет токены в `sessionStorage` и показывает их в textarea.

Это удобная демонстрационная схема, но для production она повышает риск утечки через XSS, browser extensions, screenshots и ручное копирование.

Предпочтительнее:

- secure, HttpOnly, SameSite cookies;
- или одноразовый authorization code, который frontend обменивает на токены через backend;
- обязательный HTTPS;
- отсутствие отображения долгоживущего refresh token на странице.

### Высокий приоритет

#### 5. Health endpoint не проверяет PostgreSQL

`AddHealthChecks()` подключен, но реальная DB health check не добавлена. `/health` может быть зеленым, даже если database connection недоступен.

Решение: добавить PostgreSQL health check и разделить liveness/readiness endpoints.

#### 6. OTLP endpoint зашит в коде

`http://localhost:4317` в контейнерном окружении обычно указывает на контейнер API, а не на внешний collector.

Решение: вынести endpoint в `OTEL_EXPORTER_OTLP_ENDPOINT` или typed options и добавить collector в Compose/инфраструктуру.

#### 7. Жестко заданный адрес получателя email

Регистрация и OAuth используют фиксированный email-адрес, а не email зарегистрированного пользователя. Это делает уведомления непереносимыми и может привести к отправке персональных сообщений не тому получателю.

Решение: использовать `user.Email`, проверять его наличие и сделать notification policy явной.

#### 8. Несогласованная валидация пароля

В `LoginDto` указана минимальная длина пароля 8 символов, но `AuthService` вручную проверяет только длину больше 3. Нужно выбрать одну бизнес-политику и применять ее одинаково на DTO и service level.

#### 9. Администраторский профиль возвращает роль User

`GetCurrentUserProfileAsync` создает `CurrentUserProfileDto` с `Role = "User"` независимо от реальной роли. Администратор может получить неверную роль в profile response.

Решение: возвращать `currentUser.Data.Role` и покрыть это тестом.

### Средний приоритет

#### 10. Фоновая служба слишком часто ходит в базу

`RemoveOldActions` и `UnblockUser` используют циклы с задержками 100 мс и 1 с. Это может создавать постоянную нагрузку на PostgreSQL.

Решение:

- вынести периодичность в конфигурацию;
- использовать один периодический цикл с разумным интервалом;
- удалять записи одной командой `ExecuteDeleteAsync`;
- обновлять просроченные блокировки одним SQL/EF запросом;
- корректно логировать количество обработанных строк.

#### 11. Нужна явная транзакционность для связанных операций

Логирование действия, изменение пользователя и сохранение refresh token выполняются несколькими последовательными `SaveChangesAsync`. При ошибке между шагами состояние может быть частично сохранено.

Для auth/refresh/admin mutations стоит рассмотреть явные транзакции и единый application service boundary.

#### 12. Миграции требуют ревизии

В истории есть много migrations с именами `fix`, `NewMigration`, `MigrsNew`, `BlockedUntill` и несколько миграций без содержательных операций. Это отражает активную разработку, но усложняет аудит схемы.

Перед production deployment стоит:

- проверить итоговый snapshot;
- удалить пустые/дублирующие migration в безопасной ветке;
- привести naming к единому стилю;
- проверить миграции на чистой базе и на базе с реальными данными;
- не делать автоматическое `MigrateAsync()` на production без осознанной стратегии deployment.

#### 13. Ошибка валидации пагинации

`PaginationParams` принимает `Page` без ограничения `Page >= 1`, а размер страницы захардкожен в service. Стоит добавить DataAnnotations/Fluent validation и ограничить максимально допустимую страницу/размер.

#### 14. Production Compose-файл пустой

`docker-compose.prod.yml` существует, но не содержит конфигурации. Production deployment пока описан только частично через development-oriented Compose.

#### 15. Нет `UseHttpsRedirection`

В production включен HSTS, но в pipeline не видно `UseHttpsRedirection`. Если TLS завершается на reverse proxy, это нужно явно документировать; если нет — следует настроить HTTPS redirect и secure callback URLs.

#### 16. Correlation ID не возвращается клиенту

Middleware генерирует ID и добавляет его только в logger scope. Для удобной поддержки API можно также возвращать его в response header, например `X-Correlation-ID`, и принимать входящий ID после валидации.

## 15. Что можно написать в резюме

### Короткая формулировка проекта

> Разработал ASP.NET Core Web API на .NET 10 для аутентификации и управления пользователями: JWT/refresh tokens с BCrypt, Google/GitHub OAuth, роли User/Admin, временная блокировка, audit history, PostgreSQL через EF Core, caching, rate limiting, background jobs, SMTP, Docker Compose, OpenTelemetry и Prometheus.

### Формулировки навыков

- C#, .NET 10, ASP.NET Core Web API;
- REST API, attribute routing, model validation, Swagger/OpenAPI;
- JWT Bearer, claims, role-based authorization, OAuth 2.0 providers;
- BCrypt password hashing и безопасное хранение refresh token hash;
- Entity Framework Core, PostgreSQL, Npgsql, Fluent API, migrations;
- LINQ, async/await, CancellationToken;
- DI, service interfaces, custom middleware и action filters;
- IMemoryCache и cache invalidation;
- BackgroundService и scoped dependencies;
- SMTP/MailKit, Google OAuth, GitHub OAuth;
- xUnit, Moq, EF Core InMemory, Testcontainers;
- Docker multi-stage builds, Docker Compose, health checks;
- OpenTelemetry, Prometheus и structured logging.

### Более сильная формулировка для портфолио

> Реализовал backend с разделением controller/service/data layers, role-based access control, JWT + refresh rotation, OAuth login, PostgreSQL persistence, audit trail, caching and rate limiting. Добавил автоматические миграции, Docker-инфраструктуру, integration tests с Testcontainers PostgreSQL и observability через OpenTelemetry/Prometheus.

## 16. Карта файлов проекта

| Область | Файлы |
|---|---|
| Точка входа | [`Program.cs`](Program.cs) |
| Auth endpoints | [`Controllers/AuthController.cs`](Controllers/AuthController.cs) |
| Admin endpoints | [`Controllers/AdminUsersController.cs`](Controllers/AdminUsersController.cs) |
| Current user endpoints | [`Controllers/CurrentUserController.cs`](Controllers/CurrentUserController.cs) |
| OAuth endpoints | [`Controllers/OAuthController.cs`](Controllers/OAuthController.cs) |
| Auth business logic | [`Services/AuthService.cs`](Services/AuthService.cs) |
| User business logic | [`Services/UserService.cs`](Services/UserService.cs) |
| JWT | [`Services/JwtService.cs`](Services/JwtService.cs) |
| Refresh token | [`Services/RefreshTokenService.cs`](Services/RefreshTokenService.cs) |
| OAuth orchestration | [`Services/OAuthService.cs`](Services/OAuthService.cs) |
| Google user provisioning | [`Services/GoogleUser.cs`](Services/GoogleUser.cs) |
| GitHub user provisioning | [`Services/GitHubUser.cs`](Services/GitHubUser.cs) |
| Password hashing | [`Services/BCryptPasswordHashService.cs`](Services/BCryptPasswordHashService.cs) |
| Audit history | [`Services/UserActionService.cs`](Services/UserActionService.cs) |
| Background jobs | [`Services/BackgroundLoggingService.cs`](Services/BackgroundLoggingService.cs) |
| Email | [`Services/emailService.cs`](Services/emailService.cs) |
| EF Core context | [`Data/AppDbContext.cs`](Data/AppDbContext.cs) |
| Entities | [`Entities/`](Entities/) |
| DTOs | [`DTOs/`](DTOs/) |
| Helpers | [`helpers/`](helpers/) |
| Middleware | [`Middlewares/`](Middlewares/) |
| Tests | [`MyApiBlya.Tests/`](MyApiBlya.Tests/) |
| Runtime configuration | [`docker-compose.yml`](docker-compose.yml), [`dockerfile`](dockerfile) |

## 17. Итоговая оценка

Проект показывает, что разработчик умеет собрать связную backend-систему на C#/.NET, а не только написать отдельный CRUD endpoint. Особенно хорошо отражены:

1. понимание ASP.NET Core pipeline;
2. DI и сервисные абстракции;
3. работа с PostgreSQL и EF Core;
4. authentication/authorization;
5. интеграции с OAuth и SMTP;
6. тестирование на нескольких уровнях;
7. Docker и runtime configuration;
8. logging, metrics и tracing.

Главный следующий шаг — довести security и reliability до production-уровня: убрать секреты из файлов и логов, исправить persistence refresh rotation, перестроить OAuth token handoff, добавить реальный DB readiness check и стабилизировать фоновые задачи/миграции. После этого проект будет заметно сильнее выглядеть как портфолио backend-разработчика и как основа для реального сервиса.

