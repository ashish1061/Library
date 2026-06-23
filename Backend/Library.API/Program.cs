using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddTransient<System.Data.IDbConnection>((sp) => new SqlConnection(connectionString));

// Register Repositories
builder.Services.AddScoped<Library.API.Repositories.Interfaces.IAuthRepository, Library.API.Repositories.AuthRepository>();
builder.Services.AddScoped<Library.API.Repositories.Interfaces.IBookRepository, Library.API.Repositories.BookRepository>();
builder.Services.AddScoped<Library.API.Repositories.Interfaces.IEmployeeRepository, Library.API.Repositories.EmployeeRepository>();
builder.Services.AddScoped<Library.API.Repositories.Interfaces.IIssueRepository, Library.API.Repositories.IssueRepository>();
builder.Services.AddScoped<Library.API.Repositories.Interfaces.IMagazineRepository, Library.API.Repositories.MagazineRepository>();
builder.Services.AddScoped<Library.API.Repositories.Interfaces.IAnalyticsRepository, Library.API.Repositories.AnalyticsRepository>();

// Register Services
builder.Services.AddScoped<Library.API.Services.Interfaces.IAuthService, Library.API.Services.AuthService>();
builder.Services.AddScoped<Library.API.Services.Interfaces.IBookService, Library.API.Services.BookService>();
builder.Services.AddScoped<Library.API.Services.Interfaces.IEmployeeService, Library.API.Services.EmployeeService>();
builder.Services.AddScoped<Library.API.Services.Interfaces.IIssueService, Library.API.Services.IssueService>();
builder.Services.AddScoped<Library.API.Services.Interfaces.IMagazineService, Library.API.Services.MagazineService>();
builder.Services.AddScoped<Library.API.Services.Interfaces.IAnalyticsService, Library.API.Services.AnalyticsService>();
builder.Services.AddScoped<Library.API.Services.Interfaces.IReportService, Library.API.Services.ReportService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS for Angular frontend
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

