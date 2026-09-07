using Hi_Trade.BLL.DI;
using Hi_Trade.DAL;
using Hi_Trade.DAL.DI;
using Hi_Trade.Endpoints;
using Hi_Trade.Models.Common;
using Hi_Trade.Models.Validators;
using Hi_Trade.Services;
using Hi_Trade.Services.DI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

// Add DbContext with SQL Server
builder.Services.AddDbContext<TechScannerContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContextFactory<TechScannerContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped);

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure JWT Options & Authentication
builder.Services.Configure<JWTOptions>(builder.Configuration.GetSection("JWTOptions"));
var jwtSecret = builder.Configuration["JWTOptions:Secret"] ?? "TechScannerTN_SuperSecretKey_2026_SecureAuthentication_DevKey_AtLeast32BytesLong";
var jwtIssuer = builder.Configuration["JWTOptions:Issuer"] ?? "TechScannerTN";
var jwtAudience = builder.Configuration["JWTOptions:Audience"] ?? "TechScannerTN.Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// Register Layer Services via DI Extensions
builder.Services
    .AddDALServices()
    .AddBLLServices()
    .RegisterValidators()
    .AddServices();

// Scrapers
builder.Services.AddHttpClient("Mytek", client =>
{
    client.BaseAddress = new Uri("https://www.mytek.tn/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
});
builder.Services.AddHttpClient("TunisiaNet", client =>
{
    client.BaseAddress = new Uri("https://www.tunisianet.com.tn/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
});
builder.Services.AddHttpClient("Spacenet", client =>
{
    client.BaseAddress = new Uri("https://spacenet.tn/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
});
builder.Services.AddScoped<MytekScraperService>();
builder.Services.AddScoped<IMytekScraperService>(serviceProvider =>
    serviceProvider.GetRequiredService<MytekScraperService>());
builder.Services.AddScoped<IWebScraper, MytekScraperService>();
builder.Services.AddScoped<IWebScraper, TunisiaNetScraper>();
builder.Services.AddScoped<IWebScraper, SpacenetScraperService>();

// Register hosted service for web scraping
builder.Services.AddHostedService<WebScraperHostedService>();

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Swagger with Bearer Support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TechScannerTN API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token directly in the text input below (without 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Ensure database is migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TechScannerContext>();
    context.Database.Migrate();
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapInternetProviderEndpoints();

app.Run();

