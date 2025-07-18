using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FluentValidation;
using Serilog;
using Minio;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using BE_SaleHunter.Infrastructure.Repositories;
using BE_SaleHunter.Application.Services;
using BE_SaleHunter.Application.Mappings;
using BE_SaleHunter.Application.Validators;
using BE_SaleHunter.Infrastructure.Logging;
using BE_SaleHunter.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.With<RequestContextEnricher>()
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("APPLICATION STARTUP - Configuring services...");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor(); // For enrichers
Log.Information("SERVICE REGISTRATION - Controllers and HttpContextAccessor registered");

// Add API Explorer for Swagger
builder.Services.AddEndpointsApiExplorer();
Log.Information("SERVICE REGISTRATION - API Explorer registered");

// Configure Swagger with JWT authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SaleHunter API",
        Version = "v1",
        Description = "Backend API for SaleHunter mobile application"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            []
        }
    });
});

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Log.Information("SERVICE REGISTRATION - Configuring Entity Framework with connection string: {ConnectionString}", 
    connectionString?.Substring(0, Math.Min(50, connectionString.Length)) + "...");

builder.Services.AddDbContext<SaleHunterDbContext>(options =>
    options.UseSqlServer(connectionString));
Log.Information("SERVICE REGISTRATION - Entity Framework with SQl server registered");

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key configuration is required");
var key = Encoding.ASCII.GetBytes(jwtKey);
Log.Information("SERVICE REGISTRATION - JWT Key configured (length: {KeyLength})", jwtKey.Length);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };    });
Log.Information("SERVICE REGISTRATION - JWT Authentication configured");

// Configure Authorization
builder.Services.AddAuthorization(option =>
{
    option.AddPolicy("Owner", policyBuilder => 
        policyBuilder.RequireAssertion(
              context => context.User.HasClaim(claim => claim.Type == "Role") &&
              context.User.FindFirst(claim => claim.Type == "Role").Value == "Owner"));
    option.AddPolicy("Admin", policyBuilder =>
        policyBuilder.RequireAssertion(
              context => context.User.HasClaim(claim => claim.Type == "Role") &&
              context.User.FindFirst(claim => claim.Type == "Role").Value == "Admin"));
});
Log.Information("SERVICE REGISTRATION - Authorization configured");

// Configure CORS for mobile app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
Log.Information("SERVICE REGISTRATION - CORS policy 'AllowMobileApp' configured");

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));
Log.Information("SERVICE REGISTRATION - AutoMapper configured with MappingProfile");

// Configure FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CreateProductValidator).Assembly);
Log.Information("SERVICE REGISTRATION - FluentValidation configured");

// Configure Identity for password hashing
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
Log.Information("SERVICE REGISTRATION - Password hasher configured");

// Configure MinIO
var minioEndpoint = builder.Configuration["MinIO:Endpoint"];
var minioUseSsl = builder.Configuration["MinIO:UseSSL"];

builder.Services.AddSingleton<IMinioClient>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    return new MinioClient()
        .WithEndpoint(config["MinIO:Endpoint"])
        .WithCredentials(config["MinIO:AccessKey"], config["MinIO:SecretKey"])
        .WithSSL(bool.Parse(config["MinIO:UseSSL"] ?? "false"))
        .Build();
});
Log.Information("SERVICE REGISTRATION - MinIO configured (Endpoint: {Endpoint}, UseSSL: {UseSSL})", 
    minioEndpoint, minioUseSsl);

// Configure HttpClient for location services
builder.Services.AddHttpClient<ILocationService, OpenStreetMapLocationService>();
Log.Information("SERVICE REGISTRATION - HttpClient for location services configured");

Log.Information("SERVICE REGISTRATION - Registering Repository Layer...");
// Register Repository Layer
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductRatingRepository, ProductRatingRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
Log.Information("SERVICE REGISTRATION - Repository Layer registered (UnitOfWork, UserRepository, StoreRepository, ProductRepository, ProductRatingRepository, GenericRepository)");

Log.Information("SERVICE REGISTRATION - Registering Service Layer...");
// Register Service Layer
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IImageStorageService, MinioImageStorageService>();
builder.Services.AddScoped<ILocationService, OpenStreetMapLocationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
Log.Information("SERVICE REGISTRATION - Service Layer registered (AuthService, UserService, StoreService, ProductService, ImageStorageService, LocationService)");
builder.Services.AddRazorPages();
Log.Information("APPLICATION STARTUP - Building application...");
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SaleHunterDbContext>();
        context.Database.EnsureCreated();
        Log.Information("Database schema checked and ensured.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred creating the DB.");
    }
}

// Initialize service locator for enrichers
ServiceLocator.SetServiceProvider(app.Services);

Log.Information("APPLICATION STARTUP - Application built successfully");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SaleHunter API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Use custom request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// Use Serilog for request logging (additional structured logging)
app.UseSerilogRequestLogging();

// Use global exception handler
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowMobileApp");
app.MapRazorPages();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting SaleHunter API");
    app.Run();
    Log.Information("SaleHunter API stopped gracefully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}