# EquipmentLibrary V2 (ELA V2)

Кроссплатформенное десктопное приложение для учёта оборудования лаборатории.  
Переработанная версия предыдущей системы на **Avalonia UI** с **.NET 10** и **PostgreSQL**.

## Возможности

- **Авторизация** — вход по логину/паролю с проверкой через `crypt()` PostgreSQL
- **Автовход** — сессия сохраняется в зашифрованном cookie-файле на 7 дней
- **Ролевая модель** — Администратор, Метролог, Пользователь
- **Библиотека оборудования** — поиск, фильтр по дате и категории
- **Рабочая область** — быстрая регистрация оборудования, сводка по объектам учёта
- **Панель администратора** — управление пользователями (добавление, редактирование, удаление, поиск, фильтрация, копирование пароля)
- **Журнал измерений** — *(в разработке)*
- **Реестр испытательного оборудования** — *(в разработке)*
- **Тёмная тема** — единый стиль интерфейса
- **Логирование** — Serilog (консоль + файл `launcher-log-xx_xx_xxx.txt`)
- **Проверка подключения** — ping + `SELECT 1` перед операциями с БД

## Технологии

- **.NET 10** — целевая платформа
- **Avalonia UI 12.0.5** — кроссплатформенный UI-фреймворк
- **CommunityToolkit.Mvvm 8.4.2** — MVVM (source generators)
- **Npgsql 10.0.3** + **Dapper 2.1.79** — доступ к PostgreSQL
- **Serilog** — структурированное логирование
- **Newtonsoft.Json** — сериализация cookie
- **TextCopy** — работа с буфером обмена

## Структура проекта

```
EquipmentLibraryV2-Avalonia/
├── App.axaml / App.axaml.cs       # Точка входа приложения
├── Program.cs                     # Main
├── ViewLocator.cs                 # Резолвер View → ViewModel
├── Models/                        # Модели данных (User, UserRole, EquipmentType, CookieData)
├── Services/                      # Сервисы (AuthService, ConnectivityService)
├── Infrastructure/                # Конфигурация, пути, сессия
├── Messages/                      # Сообщения для WeakReferenceMessenger
├── ViewModels/                    # ViewModel'и страниц и компонентов
├── Views/                         # XAML-представления
├── Converters/                    # IValueConverter
├── Styles/                        # Стили и темы
└── Assets/                        # Иконки и ресурсы
```

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL (БД `ELA_V2`, локально на порту 5432)

## Запуск

```bash
git clone https://github.com/your-repo/EquipmentLibraryV2-Avalonia.git
cd EquipmentLibraryV2-Avalonia/EquipmentLibraryV2-Avalonia
dotnet run
```

## База данных

Скрипт инициализации БД — [`db.sql`](db.sql) в корне репозитория.  
Строка подключения настраивается в `Infrastructure/AppConfig.cs`.

## Разработка

Проект следует паттерну **MVVM** с использованием source-генераторов `CommunityToolkit.Mvvm`.  
Навигация реализована через `WeakReferenceMessenger`.  
Модальные окна построены как наложение слоёв (`OverlayContent`, `TopOverlayContent`) в `MainWindowViewModel`.

```bash
dotnet build
dotnet run
```
