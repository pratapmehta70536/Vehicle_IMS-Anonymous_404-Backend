using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Backend;
using Backend.Data;
using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Database Context ──────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── JWT Authentication ────────────────────────────────────────────────
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();

// ─── Dependency Injection: Services ────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPartService, PartService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ─── Controllers (with kebab-case route convention) ────────────────────
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(
        new SlugifyParameterTransformer()));
});

// ─── Swagger / OpenAPI ─────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vehicle IMS API",
        Version = "v1",
        Description = "Vehicle Parts Selling & Inventory Management System API"
    });

    // JWT Bearer token support in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token.\nExample: Bearer eyJhbGciOiJIUz..."
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

// ─── CORS ──────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ─── Middleware Pipeline ───────────────────────────────────────────────
app.UseCors("AllowFrontend");

// Swagger UI available at /index.html
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehicle IMS API v1");
    options.RoutePrefix = string.Empty; // Serve at root → /index.html
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── Database Migration & Admin Seeding ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();

    // Seed admin user if not exists
    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        context.Users.Add(new Backend.Models.User
        {
            FullName = "System Admin",
            Email = "applicationdevelopment404@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
            Role = "Admin",
            Phone = "0000000000",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    // Seed staff user if not exists
    if (!context.Users.Any(u => u.Role == "Staff"))
    {
        context.Users.Add(new Backend.Models.User
        {
            FullName = "Demo Staff",
            Email = "staff@demo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("staff"),
            Role = "Staff",
            Phone = "1111111111",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    // Seed customer user if not exists
    if (!context.Users.Any(u => u.Role == "Customer"))
    {
        context.Users.Add(new Backend.Models.User
        {
            FullName = "Demo Customer",
            Email = "customer@demo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("customer"),
            Role = "Customer",
            Phone = "2222222222",
            Address = "123 Demo St",
            IsActive = true
        });
        await context.SaveChangesAsync();
    }
}

app.Run();
