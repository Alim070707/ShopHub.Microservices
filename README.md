# ShopHub Microservices

Реализация КТ-4: микросервисы CatalogService, OrderService, NotificationService.

Отчёт по проектированию (КТ-3): [docs/KT-3.docx](./docs/KT-3.docx)

## Архитектура

- CatalogService (:5002) — CRUD товаров, PostgreSQL + Redis
- OrderService (:5004) — создание заказов, HTTP → Catalog, RabbitMQ
- NotificationService (:5006) — consumer OrderCreated, отправка email

## Статус

В разработке.