using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using TelegramBOT.Data;
using TelegramBOT.Data.Repositories;
using TelegramBOT.Handlers;
using TelegramBOT.Handlers.Calendar;
using TelegramBOT.Handlers.Navigation;
using TelegramBOT.Handlers.Predictions;
using TelegramBOT.Handlers.Results;
using TelegramBOT.Handlers.Stats;
using TelegramBOT.Handlers.System;
using TelegramBOT.Handlers.Teams;
using TelegramBOT.Services.Calendar;
using TelegramBOT.Services.Core;
using TelegramBOT.Services.Predictions;
using TelegramBOT.Services.Results;
using TelegramBOT.Services.Stats;
using TelegramBOT.Services.Teams;
using TelegramBOT.Services.Utils;
using TelegramBOT.UI;

// -----------------------------
// Создание builder
// -----------------------------
var builder = Host.CreateDefaultBuilder(args);

// -----------------------------
// Логирование через Serilog
// -----------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Information()
    .WriteTo.File(
        $"Log/log-{DateTime.Now:yyyyMMdd_HHmmss}.txt",
        rollingInterval: RollingInterval.Infinite,
        retainedFileCountLimit: 5,
        shared: true
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
    // DbContext
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
    // Handlers (обработчики Telegram)
    // -----------------------------
    services.AddScoped<CalendarHandler>();
    services.AddScoped<CommandHandler>();
    services.AddScoped<NavigationHandler>();
    services.AddScoped<ResultsHandler>();
    services.AddScoped<MatchStatsHandler>();
    services.AddScoped<TeamsHandler>();
    services.AddScoped<PredictionHandler>();
    services.AddScoped<UpdateHandler>();

    // -----------------------------
    // Core Services (Telegram инфраструктура)
    // -----------------------------
    services.AddSingleton<MessageService>();
    services.AddScoped<MenuService>();
    services.AddScoped<ScriptService>();
    services.AddHostedService<BotBackgroundService>();

    // -----------------------------
    // Business Services (бизнес-логика)
    // -----------------------------
    services.AddScoped<CalendarService>();
    services.AddScoped<ResultsService>();
    services.AddScoped<MatchStatsService>();
    services.AddScoped<TeamsService>();
    services.AddScoped<PredictionService>();

    // -----------------------------
    // Repositories (доступ к данным)
    // -----------------------------
    services.AddScoped<ICalendarRepository, CalendarRepository>();
    services.AddScoped<IPredictionRepository, PredictionRepository>();
    services.AddScoped<IResultsRepository, ResultsRepository>();
    services.AddScoped<ITeamsRepository, TeamsRepository>();
    services.AddScoped<IMatchStatsServiceRepository, MatchStatsServiceRepository>();

    // -----------------------------
    // Утилиты
    // -----------------------------
    services.AddSingleton<MappingService>();
});

// -----------------------------
// Запуск приложения
// -----------------------------
await builder.RunConsoleAsync();
