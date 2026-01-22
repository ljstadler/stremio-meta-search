using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

using static Manifest;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<Config>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin();
        }
    );
});

builder.Services.AddOutputCache();

builder.Services.AddOptions<OutputCacheOptions>()
    .Configure<IOptions<Config>>((options, config) =>
    {
        options.AddBasePolicy(p => p.Expire(TimeSpan.FromSeconds(config.Value.CacheTtl)));
    });

builder.Services.AddSingleton<TVDBConfig>();
builder.Services.AddHostedService<TVDBConfigRefreshWorker>();

var app = builder.Build();

var config = app.Services.GetRequiredService<IOptions<Config>>().Value;

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation($"Manifest URL: {config.BaseUrl}/manifest.json{(!string.IsNullOrEmpty(config.AddonPassword) ? $"?auth={config.AddonPassword}" : "")}");
});

app.UseCors();

app.Use(async (context, next) =>
{
    if (!string.IsNullOrEmpty(config.AddonPassword) && config.AddonPassword != context.Request.Query["auth"])
    {
        context.Response.StatusCode = 401;
        return;
    }
    await next(context);
});

app.UseOutputCache();

app.MapGet("/manifest.json", () => Results.Content(manifest, "application/json"));

MovieEndpoints.Map(app);

SeriesEndpoints.Map(app);

app.Run();