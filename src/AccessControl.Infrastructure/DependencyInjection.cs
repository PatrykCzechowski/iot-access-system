using System.Text;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Devices.Abstractions;
using AccessControl.Infrastructure.Auth;
using AccessControl.Infrastructure.Devices;
using AccessControl.Infrastructure.Devices.Adapters;
using AccessControl.Infrastructure.Devices.Discovery;
using AccessControl.Infrastructure.Identity;
using AccessControl.Infrastructure.Mqtt;
using AccessControl.Infrastructure.Mqtt.Handlers;
using AccessControl.Infrastructure.Persistence;
using AccessControl.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace AccessControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = BuildConnectionString(configuration);

        services.AddSingleton<NpgsqlDataSource>(_ =>
            new NpgsqlDataSourceBuilder(connectionString)
                .EnableDynamicJson()
                .Build());

        services.AddDbContext<AccessControlDbContext>((sp, options) =>
        {
            var ds = sp.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(ds, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AccessControlDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });

            options.UseSnakeCaseNamingConvention();
        });

        services.AddHealthChecks()
            .AddDbContextCheck<AccessControlDbContext>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddTransient<AdminSeeder>();
        services.AddScoped<DevDataSeeder>();

        services.AddSingleton<IDeviceAdapter, CardReaderDeviceAdapter>();
        services.AddSingleton<IDeviceAdapter, KeypadReaderDeviceAdapter>();
        services.AddSingleton<IDeviceAdapter, CardAndKeypadReaderDeviceAdapter>();
        services.AddSingleton<IDeviceAdapter, DisplayExecutorDeviceAdapter>();
        services.AddSingleton<IDeviceAdapter, LockPinExecutorDeviceAdapter>();
        services.AddSingleton<IDeviceAdapterResolver, DeviceAdapterResolver>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IAccessCardRepository, AccessCardRepository>();
        services.AddScoped<IAccessZoneRepository, AccessZoneRepository>();
        services.AddScoped<IAccessLogRepository, AccessLogRepository>();
        services.Configure<DeviceDiscoveryOptions>(
            configuration.GetSection(DeviceDiscoveryOptions.SectionName));
        services.AddSingleton<IDeviceDiscoveryService, DeviceDiscoveryService>();

        // MQTT
        services.Configure<MqttOptions>(configuration.GetSection(MqttOptions.SectionName));
        services.AddSingleton<MqttClientService>();
        services.AddSingleton<IMqttService>(sp => sp.GetRequiredService<MqttClientService>());
        services.AddHostedService(sp => sp.GetRequiredService<MqttClientService>());
        services.AddHostedService<MqttBrokerMdnsAdvertiser>();
        services.AddScoped<ICardAccessService, CardAccessService>();
        services.AddScoped<ICardEnrollmentService, CardEnrollmentService>();

        services.AddSingleton<HeartbeatThrottler>();
        services.AddScoped<IMqttMessageHandler, AnnounceMqttHandler>();
        services.AddScoped<IMqttMessageHandler, HeartbeatMqttHandler>();
        services.AddScoped<IMqttMessageHandler, CardScannedMqttHandler>();
        services.AddScoped<IMqttMessageHandler, CardEnrolledMqttHandler>();
        services.AddScoped<IMqttMessageHandler, ConfigAckMqttHandler>();
        services.AddScoped<IMqttMessageHandler, LockStatusMqttHandler>();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AccessControlDbContext>()
            .AddDefaultTokenProviders();

        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must be at least 256 bits (32 bytes). Configure 'Jwt:Key' via user secrets or environment variable Jwt__Key.");
        }

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Builds the connection string from configuration or environment variables.
    /// Resolution priority:
    ///   1. ConnectionStrings:DefaultConnection (appsettings / env: ConnectionStrings__DefaultConnection)
    ///   2. Individual environment variables: DB_HOST, DB_PORT, POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD
    /// </summary>
    private static string BuildConnectionString(IConfiguration configuration)
    {
        var fullConnectionString = configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(fullConnectionString))
        {
            return fullConnectionString;
        }

        var host     = configuration["DB_HOST"]           ?? "localhost";
        var port     = configuration["DB_PORT"]           ?? "5432";
        var database = configuration["POSTGRES_DB"]       ?? throw new InvalidOperationException("Database configuration missing: POSTGRES_DB");
        var user     = configuration["POSTGRES_USER"]     ?? throw new InvalidOperationException("Database configuration missing: POSTGRES_USER");
        var password = configuration["POSTGRES_PASSWORD"] ?? throw new InvalidOperationException("Database configuration missing: POSTGRES_PASSWORD");

        if (!int.TryParse(port, out var portNumber) || portNumber is < 1 or > 65535)
        {
            throw new InvalidOperationException($"Invalid DB_PORT value: '{port}'");
        }

        return new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = portNumber,
            Database = database,
            Username = user,
            Password = password,
            Pooling = true,
            MinPoolSize = 2,
            MaxPoolSize = 100
        }.ConnectionString;
    }
}
