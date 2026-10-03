# ShopHub Microservices

Микросервисный проект ShopHub — реализация КТ-4.

📎 **Отчёт по проектированию (КТ-3):** [docs/KT-3.docx](./docs/KT-3.docx)

## Архитектура

| Сервис | Порт | Назначение | Хранилище |
|---|---|---|---|
| **CatalogService** | 5065 | CRUD товаров, кэш, availability | PostgreSQL + Redis |
| **OrderService** | 5212 | Создание заказов, HTTP → Catalog, RabbitMQ | PostgreSQL |
| **NotificationService** | 5037 | Обработка OrderCreated, email | PostgreSQL |

**Инфраструктура:**

- RabbitMQ (5672 / UI 15672) — асинхронные события
- Redis (6379) — кэш каталога
- PostgreSQL × 3 — по БД на сервис (Database per Service)

### Схема взаимодействия

```
Client ──HTTP──▶ OrderService :5212
                    │
                    │ HTTP GET (Polly: retry + circuit breaker)
                    ▼
                CatalogService :5065 ──▶ Redis (кэш)
                    │
                    │ Publish OrderCreatedEvent
                    ▼
                RabbitMQ (exchange shop.order.created)
                    │
                    │ Consume
                    ▼
                NotificationService :5037 ──▶ PostgreSQL (журнал)
                    │
                    │ SMTP (в dev — FakeEmailService)
                    ▼
                  Email
```

## Технологии

- ASP.NET Core 10 Minimal API
- Entity Framework Core + PostgreSQL 16
- StackExchange.Redis (кэш)
- MassTransit 8.5.7 + RabbitMQ
- Polly (Retry + Circuit Breaker)
- MailKit (SMTP)
- Docker + Docker Compose

## Быстрый старт

### Требования

- Docker Desktop (с включённой виртуализацией)

### Запуск через Docker Compose

```bash
git clone https://github.com/Alim070707/ShopHub.Microservices.git
cd ShopHub.Microservices
cp .env.example .env
docker compose up --build
```

**Первый запуск — 3–5 минут** (сборка образов). Последующие — 30 секунд.

### Проверка работоспособности

```bash
# Health checks
curl http://localhost:5065/health
curl http://localhost:5212/health
curl http://localhost:5037/health

# Swagger UI
# http://localhost:5065/swagger  — CatalogService
# http://localhost:5212/swagger  — OrderService

# RabbitMQ Management UI
# http://localhost:15672  (admin / пароль из .env)
```

### End-to-end тест

```bash
curl -X POST http://localhost:5212/api/orders \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 00000000-0000-0000-0000-000000000001" \
  -H "X-User-Email: test@example.com" \
  -d '{"items":[{"productId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","quantity":1}],"shippingAddress":{"street":"Test","city":"Moscow","zipCode":"101000"}}'

# Проверить логи NotificationService
docker compose logs notification-service | grep "FAKE EMAIL"
```

## Структура проекта

```
ShopHub.Microservices/
├── CatalogService/        — каталог товаров
├── OrderService/          — заказы + HTTP-интеграция
├── NotificationService/   — email-уведомления
├── Contracts/             — общие события (OrderCreatedEvent)
├── docs/KT-3.docx         — отчёт по проектированию
├── docker-compose.yml
├── docker-compose.override.yml
├── .env.example
└── README.md
```

## Особенности реализации

- **Database per Service** — у каждого сервиса своя БД, без SQL-внешних ключей
- **Snapshot в заказе** — `OrderItem` хранит `ProductName` и `PriceAtPurchase` на момент покупки
- **Идемпотентность** — `NotificationLog.EventId` уникален, повторная доставка не отправит письмо дважды
- **Retry + Circuit Breaker** — OrderService устойчив к падениям CatalogService (Polly)
- **Health Checks** — `/health` у каждого сервиса
- **Redis-кэш** — кэш карточки товара с TTL 5 минут, инвалидация при обновлении

## Остановка

```bash
docker compose down       # остановить, сохранить данные БД
docker compose down -v    # остановить + удалить volumes (сброс данных)
```