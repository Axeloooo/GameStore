using Azure.Identity;
using GameStore.Data;
using GameStore.Api.Features.Baskets;
using GameStore.Api.Features.Diagnostics;
using GameStore.Api.Features.Games;
using GameStore.Api.Features.Genres;
using GameStore.Api.Features.Orders;
using GameStore.Api.Features.Payments;
using GameStore.Api.Shared.Authorization;
using GameStore.Api.Shared.Cdn;
using GameStore.Api.Shared.Cors;
using GameStore.Api.Shared.ErrorHandling;
using GameStore.Api.Shared.FileUpload;
using GameStore.Api.Shared.Messaging;
using GameStore.Api.Shared.Outbox;
using GameStore.Api.Shared.Stripe;
using Microsoft.AspNetCore.HttpLogging;
using GameStore.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails()
                .AddExceptionHandler<GlobalExceptionHandler>();

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = builder.Configuration["AZURE_CLIENT_ID"]
});

builder.AddGameStoreNpgsql<GameStoreContext>("GameStoreDB", credential);

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod |
                            HttpLoggingFields.RequestPath |
                            HttpLoggingFields.ResponseStatusCode |
                            HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddFileUploader(credential);

builder.AddGameStoreAuthentication();
builder.AddGameStoreAuthorization();

builder.AddGameStoreCors();

builder.Services.AddSingleton<CdnUrlTransformer>();

builder.AddStripe();

builder.AddMessaging("serviceBus", credential);
builder.Services.AddHostedService<OutboxProcessor>();

if (builder.Environment.IsProduction())
{
    builder.Configuration.AddAzureKeyVaultSecrets(
        "keyvault",
        settings => settings.Credential = credential
    );
}

builder.AddBasketServices();
builder.AddOrderServices();

var app = builder.Build();

app.UseCors();

app.UseAuthorization();

app.MapGames();
app.MapGenres();
app.MapBaskets();
app.MapDiagnostics();
app.MapPayments();
app.MapOrders();

app.MapDefaultEndpoints();

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/health"),
    appBuilder => appBuilder.UseHttpLogging()
);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}
else
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

await app.MigrateDbAsync();

app.Run();