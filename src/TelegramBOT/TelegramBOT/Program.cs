using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using TelegramBOT.Application.Calendar;
using TelegramBOT.Application.MatchEvents;
using TelegramBOT.Application.MatchStats;
using TelegramBOT.Application.Predictions;
using TelegramBOT.Application.Results;
using TelegramBOT.Application.Standings;
using TelegramBOT.Application.Teams;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Infrastructure.Calendar;
using TelegramBOT.Infrastructure.Data;
using TelegramBOT.Infrastructure.MatchStats;
using TelegramBOT.Infrastructure.Predictions;
using TelegramBOT.Infrastructure.Results;
using TelegramBOT.Infrastructure.Scripts;
using TelegramBOT.Infrastructure.Standings;
using TelegramBOT.Infrastructure.Teams;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.Handlers;
using TelegramBOT.Presentation.Handlers.Calendar;
using TelegramBOT.Presentation.Handlers.MatchEvents;
using TelegramBOT.Presentation.Handlers.MatchStats;
using TelegramBOT.Presentation.Handlers.Navigation;
using TelegramBOT.Presentation.Handlers.Predictions;
using TelegramBOT.Presentation.Handlers.Results;
using TelegramBOT.Presentation.Handlers.System;
using TelegramBOT.Presentation.Handlers.Teams;
using TelegramBOT.Presentation.UI;

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
    services.AddScoped<MatchEventsHandler>();
    services.AddScoped<MatchStatsHandler>();
    services.AddScoped<TeamsHandler>();
    services.AddScoped<PredictionHandler>();
    services.AddScoped<UpdateHandler>();
    services.AddScoped<StandingsHandler>();

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
    services.AddScoped<MatchEventsService>();
    services.AddScoped<MatchStatsService>();
    services.AddScoped<TeamsService>();
    services.AddScoped<PredictionService>();
    services.AddScoped<StandingsService>();

    // -----------------------------
    // Repositories (доступ к данным)
    // -----------------------------
    services.AddScoped<ICalendarRepository, CalendarRepository>();
    services.AddScoped<IPredictionRepository, PredictionRepository>();
    services.AddScoped<IResultsRepository, ResultsRepository>();
    services.AddScoped<ITeamsRepository, TeamsRepository>();
    services.AddScoped<IMatchStatsServiceRepository, MatchStatsServiceRepository>();
    services.AddScoped<IStandingsRepository, StandingsRepository>();
    // -----------------------------
    // Утилиты
    // -----------------------------
    services.AddSingleton<MappingService>();
});

// -----------------------------
// Запуск приложения
// -----------------------------
await builder.RunConsoleAsync();
