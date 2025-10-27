# Документация проекта TelegramBOT

## Общая информация

Документация проекта включает описание архитектуры, соглашений по разработке и набор диаграмм, отражающих внутреннее устройство и пользовательские сценарии работы Telegram-бота.

---

## Архитектура

- [Architecture Guide](architecture_guide.md) — структура приложения, слои и взаимодействие между ними.
- [Configuration and Conventions](configuration_and_conventions.md) — соглашения по именованию, структуре кода и настройке окружения.

---

## Диаграммы

### 1. Use-Case диаграммы (`docs/diagrams/usecase/`)

- ![Use-Case диаграмма](diagrams/usecase/usecase_diagram.png)

### 2. BPMN процессы (`docs/diagrams/bpmn/`)

- ![Основной процесс BPMN](diagrams/bpmn/bpmn_main_process.png)

### 3. ER-диаграмма сущностей (`docs/diagrams/er/`)

- ![ER-диаграмма сущностей](diagrams/er/er_entities.png)

### 4. Диаграмма базы данных (`docs/diagrams/database/`)

- ![Диаграмма структуры БД](diagrams/database/db_structure.png)

### 5. Компонентная архитектура (`docs/diagrams/components/`)

- ![Компонентная диаграмма](diagrams/components/components_overview.png)

### 6. Интерфейс пользователя (UI) (`docs/diagrams/ui/`)

---

## Генерация документации

Проект использует **DocFX** для автоматической генерации HTML-документации.  
Чтобы сгенерировать сайт:

```bash
docfx docfx.json
```
