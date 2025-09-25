using Api.Middlewares;
using Api.StartupExtensions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning() 
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information) 
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Error)
    .MinimumLevel.Override("System", LogEventLevel.Error)
    .MinimumLevel.Override("Api", LogEventLevel.Debug)
    .MinimumLevel.Override("Infrastructure", LogEventLevel.Debug)
    .MinimumLevel.Override("Application", LogEventLevel.Debug) 
    .MinimumLevel.Override("Domain", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
            path: "../logs/log-.log",
            rollingInterval: RollingInterval.Day,
            rollOnFileSizeLimit: true,
            fileSizeLimitBytes: 10_000_000,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.ConfigureServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var appContext = services.GetRequiredService<AppDbContext>();
        var authContext = services.GetRequiredService<AuthDbContext>();
        
        logger.LogInformation("Running database migrations...");
        appContext.Database.Migrate();
        authContext.Database.Migrate();
        logger.LogInformation("Migrations completed successfully.");
        
        // Check if database needs seeding
        if (!appContext.LawDocuments.Any()) // Assuming you have a LawDocuments DbSet
        {
            logger.LogInformation("Database is empty, calling /fill-database endpoint...");
            await SeedDatabaseAsync(services);
        }
        else
        {
            logger.LogInformation("Database already contains data, skipping seeding.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization.");
        throw;
    }
}

// Seeding method
async Task SeedDatabaseAsync(IServiceProvider services)
{
    var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    using var httpClient = httpClientFactory.CreateClient();
    
    try
    {
        // Call your fill-database endpoint
        var response = await httpClient.PostAsync("http://localhost/api/fill-database", null);
        
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Database seeding completed successfully.");
        }
        else
        {
            logger.LogWarning("Database seeding failed with status code: {StatusCode}", response.StatusCode);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred while seeding database.");
        // Don't throw here - let the app start even if seeding fails
    }
}

app.UseMiddleware<LogEndpointsMiddleware>();

app.UseHsts();
app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("newPolicy");
app.MapHealthChecks("/health");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
