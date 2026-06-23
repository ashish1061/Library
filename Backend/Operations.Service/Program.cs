using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared.Infrastructure.Repositories;
using Operations.Service.Services;

using Hangfire;
using Operations.Service.Filters;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/operations_service_log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Hangfire Configuration
var connectionString = builder.Configuration.GetConnectionString("LibraryDB");
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();

// Repositories
builder.Services.AddScoped<IIssueRepository, IssueRepository>();
builder.Services.AddScoped<Shared.Core.Interfaces.IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<Shared.Core.Interfaces.IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<Shared.Core.Interfaces.IEmailService, Shared.Infrastructure.Services.EmailService>();
builder.Services.AddScoped<OverdueReminderService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey == "REPLACE_VIA_ENVIRONMENT_VARIABLE_IN_PRODUCTION" || jwtKey == "A_very_long_super_secret_key_that_needs_to_be_at_least_32_bytes")
{
    throw new System.InvalidOperationException("JWT Secret Key must be properly configured in application configuration and cannot be empty or default placeholder.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "http://localhost",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "http://localhost",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors("AllowAngular");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Configure Nightly Overdue Reminder Job
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<OverdueReminderService>(
        "SendOverdueReminders",
        service => service.SendRemindersAsync(),
        Cron.Daily); // Runs every day at midnight
}

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

// Force restart

