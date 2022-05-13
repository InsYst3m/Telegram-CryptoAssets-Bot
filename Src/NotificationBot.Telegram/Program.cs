using NotificationBot.DataAccess.Extensions;
using NotificationBot.DataAccess.Services;
using NotificationBot.Telegram.Configuration;
using NotificationBot.Telegram.Infrastructure;
using NotificationBot.Telegram.Infrastructure.Generators;
using NotificationBot.Telegram.Infrastructure.HostedServices;
using NotificationBot.Telegram.Infrastructure.HostedServices.Interfaces;
using NotificationBot.Telegram.Infrastructure.Services;
using NotificationBot.Telegram.Infrastructure.Services.Interfaces;
using NotificationBot.Telegram.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
#region Configure Services

IServiceCollection services = builder.Services;

services.Configure<BotSettings>(configuration.GetSection(nameof(BotSettings)),
            options => options.BindNonPublicProperties = true);

services.Configure<NotificationsSettings>(configuration.GetSection(nameof(NotificationsSettings)),
    options => options.BindNonPublicProperties = true);

services.AddDataAccessLayer(configuration.GetConnectionString("DefaultConnection"));

services.AddSingleton<ITelegramBotClientFactory, TelegramBotClientFactory>();
services.AddSingleton<IDataAccessService, DataAccessService>();
services.AddSingleton<IMessageGenerator, MessageGenerator>();
services.AddSingleton<INotificationService, NotificationService>();
services
    .AddCryptoAssetsGraphServiceClient()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://insyst3m-002-site1.btempurl.com/graphql"));

services.AddSingleton<IBotService, BotService>();
services.AddSingleton<IDiagnosticService>(x => x.GetRequiredService<IBotService>());

services.AddSingleton<ITelegramBotHostedService, TelegramBotHostedService>();
services.AddSingleton<IDiagnosticService>(x => x.GetRequiredService<ITelegramBotHostedService>());

services.AddHostedService(x => x.GetRequiredService<ITelegramBotHostedService>());

#endregion

// Configure the HTTP request pipeline.
#region Configure Pipeline

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGet("/diagnostics", () =>
{
    List<IDiagnosticService> diagnosticServices = app.Services.GetServices<IDiagnosticService>().ToList();

    Dictionary<string, string> result = new();

    foreach (IDiagnosticService diagnosticService in diagnosticServices)
    {
        Dictionary<string, string> diagnosticsInfo = diagnosticService.GetDiagnosticsInfo();
        
        foreach (KeyValuePair<string, string> keyValuePair in diagnosticsInfo)
        {
            result.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }

    return result;
});

#endregion

app.Run();
