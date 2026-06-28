# MyApiBlya

ASP.NET Core Web API для аутентификации и управления пользователями. Проект использует PostgreSQL, Entity Framework Core, JWT-аутентификацию, refresh-токены и OAuth-вход через Google и GitHub.

## Возможности проекта

- JWT-аутентификация и защищенные endpoints
- Поддержка refresh-токенов
- OAuth-аутентификация через Google
- OAuth-аутентификация через GitHub
- Вход тестового администратора через переменные окружения
- Получение профиля, переименование, история действий, блокировка, разблокировка и удаление пользователей
- PostgreSQL база данных и миграции Entity Framework Core
- Swagger UI для тестирования API
- Docker Compose для запуска API и PostgreSQL
- xUnit тесты

## Технологический стек

| Область | Технологии |
| --- | --- |
| Backend | C#, ASP.NET Core Web API |
| База данных | PostgreSQL |
| ORM | Entity Framework Core |
| Аутентификация | JWT, refresh-токены, Google OAuth, GitHub OAuth |
| API-документация | Swagger / OpenAPI |
| Контейнеры | Docker Compose |
| Тесты | xUnit, Moq |

## Требования

- .NET SDK, совместимый с target framework проекта
- PostgreSQL, если проект запускается локально без Docker
- Docker и Docker Compose, если проект запускается в контейнерах
- EF Core CLI tools

Если `dotnet ef` не установлен:

```bash
dotnet tool install --global dotnet-ef
```

## Переменные окружения

Каждый разработчик должен использовать собственные значения переменных окружения. Не используйте чужие секреты.

Чтобы подготовить локальную конфигурацию:

1. Клонируйте репозиторий.
2. Скопируйте `.env.example` в `.env`.
3. Заполните `.env` своими значениями.
4. Запустите проект через Docker Compose или задайте такие же переменные в локальной shell/user-secrets для `dotnet run`.

Linux/macOS:

```bash
cp .env.example .env
```

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

`.env.example` можно коммитить в GitHub, потому что он содержит только плейсхолдеры. Настоящий `.env` файл нельзя коммитить.

Рекомендуемая запись в `.gitignore`:

```gitignore
.env
```

В ASP.NET Core вложенные значения конфигурации в переменных окружения записываются через двойное подчеркивание `__`.

| Ключ конфигурации | Переменная окружения |
| --- | --- |
| `Jwt:Key` | `Jwt__Key` |
| `Authentication:Google:ClientSecret` | `Authentication__Google__ClientSecret` |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |

Используемые переменные:

| Переменная | Назначение |
| --- | --- |
| `Jwt__Key` | Секретный ключ для подписи JWT-токенов |
| `Database` | Имя PostgreSQL базы данных для Docker Compose |
| `Username` | Имя пользователя PostgreSQL для Docker Compose |
| `Password` | Пароль PostgreSQL для Docker Compose |
| `ConnectionStrings__DefaultConnection` | Локальная строка подключения для EF Core / `dotnet run` |
| `Authentication__Google__ClientId` | Google OAuth client ID |
| `Authentication__Google__ClientSecret` | Google OAuth client secret |
| `Authentication__GitHub__ClientId` | GitHub OAuth client ID |
| `Authentication__GitHub__ClientSecret` | GitHub OAuth client secret |
| `ADMIN_LOGIN` | Логин тестового администратора |
| `ADMIN_PASSWORD` | Пароль тестового администратора |

## `.env.example`

```env
Database=myapi
Username=postgres
Password=your_database_password

ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=myapi;Username=postgres;Password=your_database_password

Jwt__Key=your-super-secret-jwt-key

Authentication__Google__ClientId=your-google-client-id
Authentication__Google__ClientSecret=your-google-client-secret

Authentication__GitHub__ClientId=your-github-client-id
Authentication__GitHub__ClientSecret=your-github-client-secret

ADMIN_LOGIN=admin
ADMIN_PASSWORD=your-admin-password
```

## Локальный запуск

Для локальной разработки PostgreSQL обычно доступен через `localhost`:

```env
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=myapi;Username=postgres;Password=your_database_password
```

Запуск проекта локально:

```bash
git clone <repo-url>
cd <project-folder>
dotnet restore
dotnet ef database update
dotnet run
```

После запуска API должен быть доступен по адресу:

```text
http://localhost:5063
```

## Запуск через Docker Compose

Docker Compose читает значения из `.env` и подставляет их в `docker-compose.yml` через `${VariableName}`.

Пример конфигурации сервисов:

```yml
services:
  api:
    build: .
    ports:
      - "5063:5063"
    environment:
      ASPNETCORE_HTTP_PORTS: 5063
      ASPNETCORE_ENVIRONMENT: Development
      Jwt__Key: ${Jwt__Key}
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=${Database};Username=${Username};Password=${Password}"
      Authentication__Google__ClientId: ${Authentication__Google__ClientId}
      Authentication__Google__ClientSecret: ${Authentication__Google__ClientSecret}
      Authentication__GitHub__ClientId: ${Authentication__GitHub__ClientId}
      Authentication__GitHub__ClientSecret: ${Authentication__GitHub__ClientSecret}
      ADMIN_LOGIN: ${ADMIN_LOGIN}
      ADMIN_PASSWORD: ${ADMIN_PASSWORD}

  db:
    image: postgres:17
    environment:
      POSTGRES_DB: ${Database}
      POSTGRES_USER: ${Username}
      POSTGRES_PASSWORD: ${Password}
    ports:
      - "5432:5432"
```

Разница между строками подключения:

| Среда запуска | Строка подключения |
| --- | --- |
| Локальная машина | `Host=localhost;Port=5432;Database=myapi;Username=postgres;Password=your_database_password` |
| Docker Compose | `Host=db;Port=5432;Database=myapi;Username=postgres;Password=your_database_password` |

В Docker Compose используется `Host=db`, потому что `db` - это имя PostgreSQL-сервиса внутри Docker-сети. С локальной машины используйте `localhost`.

Команды Docker Compose:

```bash
docker compose up -d
docker compose logs -f
docker compose down
```

## Swagger

После запуска приложения откройте Swagger UI в браузере:

```text
http://localhost:5063/swagger
```

Swagger можно использовать, чтобы просматривать доступные endpoints, отправлять запросы и тестировать API.

## OAuth endpoints

Google OAuth login:

```text
http://localhost:5063/api/users/login/google
```

GitHub OAuth login:

```text
http://localhost:5063/api/users/login/github
```

Когда пользователь открывает эти ссылки, его перенаправляет на страницу авторизации Google или GitHub. После успешной авторизации backend получает данные пользователя, создает JWT и refresh-токен, а затем перенаправляет пользователя на endpoint завершения авторизации:

```text
http://localhost:5063/api/users/auth-complete
```

## Миграции EF Core

Создать новую миграцию:

```bash
dotnet ef migrations add MigrationName
```

Применить миграции к базе данных:

```bash
dotnet ef database update
```

Показать список миграций:

```bash
dotnet ef migrations list
```

Удалить последнюю миграцию, если она еще не была применена:

```bash
dotnet ef migrations remove
```

## Тесты

Запустить все тесты:

```bash
dotnet test
```

## Основные API endpoints

| Метод | Endpoint | Auth | Описание |
| --- | --- | --- | --- |
| `POST` | `/api/users/login` | Public | Вход по логину и паролю |
| `POST` | `/api/users/refresh` | Public | Обновление JWT через refresh-токен |
| `POST` | `/api/users/logout` | JWT | Выход и отзыв refresh-токена |
| `POST` | `/api/users/admin` | Public | Вход администратора через настроенные admin credentials |
| `GET` | `/api/users/me` | JWT | Получить профиль текущего пользователя |
| `PUT` | `/api/users/rename` | JWT | Переименовать текущего пользователя |
| `GET` | `/api/users/history` | JWT | Получить историю действий текущего пользователя |
| `GET` | `/api/users/{id}` | Admin JWT | Получить пользователя по ID |
| `GET` | `/api/users/all` | Admin JWT | Получить всех пользователей |
| `DELETE` | `/api/users/deleteUser` | Admin JWT | Удалить пользователя |
| `PUT` | `/api/users/blockUser` | Admin JWT | Заблокировать пользователя |
| `PUT` | `/api/users/UnblockUser` | Admin JWT | Разблокировать пользователя |
| `GET` | `/api/users/login/google` | Public | Начать Google OAuth login |
| `GET` | `/api/users/login/github` | Public | Начать GitHub OAuth login |

## Структура проекта

```text
.
+-- controllers/                 # API controllers
+-- middleWares/                 # Custom middleware
+-- servises/                    # Services, models, EF Core DbContext
+-- Migrations/                  # EF Core migrations
+-- MyApiBlya.Tests/             # xUnit tests
+-- Properties/launchSettings.json
+-- Program.cs                   # Application setup and middleware pipeline
+-- docker-compose.yml           # API and PostgreSQL containers
+-- dockerfile                   # API Docker image
+-- appsettings.json
`-- appsettings.Development.json
```

## Заметки по безопасности

- Не храните реальные секреты в `README.md`.
- Не коммитьте `.env`.
- Используйте `.env.example` только как шаблон.
- Для Google/GitHub OAuth каждый разработчик должен создать собственные ClientId и ClientSecret.
- JWT secret должен быть длинным и случайным.
- Пароли и секреты должны храниться только в переменных окружения, user-secrets или secret manager.

## Автор

Maintainer проекта: `<sarkicyn>`
