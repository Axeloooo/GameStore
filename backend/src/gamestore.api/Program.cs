using gamestore.api.Data;
using gamestore.api.Features.Games;
using gamestore.api.Features.Genres;
using gamestore.api.Shared.ErrorHandling;
using gamestore.api.Shared.FileUpload;
using gamestore.api.Shared.Timing;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("GameStore");
builder.Services.AddSqlite<GameStoreContext>(connString);

builder.Services.AddHttpLogging(options => { });

builder.Services.AddProblemDetails().AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor().AddSingleton<FileUploader>();

builder
    .Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
    });

var app = builder.Build();

app.UseStaticFiles();

app.MapGames();
app.MapGenres();

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
