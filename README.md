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
