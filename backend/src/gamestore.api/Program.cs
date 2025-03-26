using gamestore.api.Data;
using gamestore.api.Features.Baskets;
using gamestore.api.Features.Baskets.Authorization;
using gamestore.api.Features.Games;
using gamestore.api.Features.Genres;
using gamestore.api.Shared.Authorization;
using gamestore.api.Shared.ErrorHandling;
using gamestore.api.Shared.FileUpload;
using gamestore.api.Shared.Timing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("GameStore");
builder.Services.AddSqlite<GameStoreContext>(connString);

builder.Services.AddHttpLogging(options => { });

builder.Services.AddProblemDetails().AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor().AddSingleton<FileUploader>();

builder.AddGameStoreAuthentication();
builder.AddGameStoreAuthorization();
builder.Services.AddSingleton<IAuthorizationHandler, BasketAuthorizationHandler>();

var app = builder.Build();

app.UseStaticFiles();

app.UseAuthorization();

app.MapGames();
app.MapGenres();
app.MapBaskets();

app.UseHttpLogging();
app.UseMiddleware<RequestTimingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}
else
{
    app.UseExceptionHandler();
}

await app.InitializeDbAsync();

app.Run();
