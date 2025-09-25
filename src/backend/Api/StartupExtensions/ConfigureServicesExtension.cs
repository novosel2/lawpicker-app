using Application.Interfaces.IClients;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;
using Infrastructure.Clients;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Api.Filters;
using Infrastructure.Services;

namespace Api.StartupExtensions;

public static class ConfigureServicesExtension
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<HandleExceptionsFilter>();
        services.AddControllers(options => 
        {
            options.Filters.AddService<HandleExceptionsFilter>();
        });

        services.AddLogging();

        services.AddCors(options => 
        {
            options.AddPolicy("newPolicy", builder => 
            {
                builder
                .AllowAnyHeader()
                .WithOrigins("http://localhost:5173", "http://localhost:3000")
                .AllowAnyMethod()
                .AllowCredentials();
            });
        });

        services.AddHealthChecks()
            .AddAzureBlobStorage(
                    connectionString: configuration.GetConnectionString("AzureStorage")!,
                    name: "azure-blob-storage")
            .AddRedis(
                    configuration.GetConnectionString("Redis")!,
                    name: "redis");


        // Add Swagger configuration with JWT Bearer authentication
        services.AddSwaggerGen(option =>
        {
            option.SwaggerDoc("v1", new OpenApiInfo { Title = "To-Do API", Version = "v1" });
            option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            option.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type=ReferenceType.SecurityScheme,
                            Id="Bearer"
                        }
                    },
                    new string[]{}
                }
            });
        });

        services.AddIdentity<AppUser, AppRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";
        })
        .AddEntityFrameworkStores<AuthDbContext>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme =
            options.DefaultChallengeScheme =
            options.DefaultSignInScheme =
            options.DefaultSignOutScheme =
            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(userId, out var id))
                    {
                        context.Fail("Invalid user ID in token.");
                        return;
                    }

                    var db = context.HttpContext.RequestServices.GetRequiredService<AuthDbContext>();
                    var user = await db.Users.FindAsync(id);

                    if (user == null)
                    {
                        context.Fail("User no longer exists.");
                    }

                    return;
                }
            };
        });

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AuthConnection")));
        services.AddDbContext<AppDbContext>(options => 
                options.UseNpgsql(configuration.GetConnectionString("AppConnection")));

        services.AddHttpClient<ILawDocumentClient, LawDocumentClient>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            client.DefaultRequestHeaders.Add("Accept", "application/pdf");
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.ConnectionClose = false; // Keep connections alive
        });

        services.AddScoped<ILawDocumentRepository, LawDocumentRepository>();

        services.AddScoped<ILawDocumentService, LawDocumentService>();
        services.AddScoped<ILawDocumentStorageService, LawDocumentStorageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;;
    }
}
