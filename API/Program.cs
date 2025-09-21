using System.Reflection;
using System.Text;
using API.BookingService;
using API.Data;
using API.Repositories;
using API.Services;
using API.Services.Mail;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
builder.Services.AddDbContext<AppDBContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")
             ?? Environment.GetEnvironmentVariable("DATABASE_URL")
             ?? throw new InvalidOperationException("DefaultConnection missing.");

    options.UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure());
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// --- Controllers / MVC ---
builder.Services.AddControllers();

// --- LDAP (fra fil 1) ---
builder.Services.Configure<LdapOptions>(builder.Configuration.GetSection("Ldap"));
builder.Services.AddSingleton<ILdapService, LdapService>(); // auth + grupper

// --- JWT ---
var jwtKey = builder.Configuration["Jwt:SecretKey"]
             ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "H2-2025-API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "H2-2025-Client";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // slå til i prod
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// --- CORS ---
// MÅSKE ÆNDRE TIL DEN SPECIFIKKE FRONTEND-URL I STEDET FOR ÅBEN FOR ALLE?
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevAll", p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel API",
        Version = "v1",
        Description = "API til hotel booking system (JWT, EF Core, CORS)."
    });

    // Indlæs XML-kommentarer for controllers/endpoints (fra fil 1)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    // Unikke schema-navne - undgå DTO-navne-konflikter (fra fil 1)
    c.CustomSchemaIds(t => t.FullName?.Replace("+", "."));

    // Bearer auth - Swagger (fra fil 1)
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Skriv 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } });
});

// --- DI: repos/services ---
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<JwtService>();

// --- MailService (fra fil 2) ---
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IMailService, MailService>();

var app = builder.Build();

// Only redirect in non-dev
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("DevAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Simple health endpoint
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
