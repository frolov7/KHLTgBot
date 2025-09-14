using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using TelegramBOT.Data;
using TelegramBOT.Handlers;
using TelegramBOT.Services;
using TelegramBOT.Utils;

// -----------------------------
// Создание builder
// -----------------------------
var builder = Host.CreateDefaultBuilder(args);

// -----------------------------
// Логирование через Serilog
// -----------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // лишние логи от Microsoft
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)    // лишние логи от System
    .MinimumLevel.Information()                                               // по умолчанию Info+
    .WriteTo.File(
        $"Log/log-{DateTime.Now:yyyyMMdd_HHmmss}.txt",  // лог в файл с датой
        rollingInterval: RollingInterval.Infinite,      // один файл на запуск
        retainedFileCountLimit: 5,                      // храним только 5 файлов
        shared: true                                    // доступен для параллельного чтения
    )
    .CreateLogger();

builder.UseSerilog();

// -----------------------------
// Конфигурация сервисов
// -----------------------------
builder.ConfigureServices((context, services) =>
{
    var configuration = context.Configuration;

    // -----------------------------
    // DbContext (работа с БД)
    // -----------------------------
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

    // -----------------------------
    // Telegram Bot Client
    // -----------------------------
    services.AddSingleton<ITelegramBotClient>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var token = config["Telegram:Token"];

        if (string.IsNullOrEmpty(token))
            throw new ArgumentNullException("Не задан токен Telegram Bot в appsettings.json");

        return new TelegramBotClient(token);
    });

    // -----------------------------
    // Обработчики приложения
    // -----------------------------
    services.AddScoped<CommandHandler>();
    services.AddScoped<MenuService>();

    // -----------------------------
    // Сервисы приложения
    // -----------------------------
    services.AddSingleton<MessageService>();              // сервис для отправки сообщений
    services.AddScoped<MatchService>();                   // сервис для матчей
    services.AddScoped<ScriptService>();                  // сервис для запуска Node-скриптов
    services.AddHostedService<BotBackgroundService>();    // запуск фонового слушателя бота

    // -----------------------------
    // Утилиты приложения
    // -----------------------------
    services.AddSingleton<MappingService>();
});

// -----------------------------
// Запуск приложения
// -----------------------------
await builder.RunConsoleAsync();
