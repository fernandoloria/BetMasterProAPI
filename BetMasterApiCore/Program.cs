using Azure.Core;
using Microsoft.Extensions.Configuration;
using BetMasterApiCore.DbTier;
using BetMasterApiCore.Hubs;
using BetMasterApiCore.Models;
using BetMasterApiCore.Utilities;
using BetMasterApiCore.Stream;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT settings
var key = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<DbConnectionHelper>();
builder.Services.AddSignalR();
builder.Services.AddTransient<Base64Service>();
builder.Services.AddSingleton<JwtService>();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Solo para pruebas locales
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin))
                return false;

            // Permitir cualquier dominio Replit
            if (origin.Contains("replit.app"))
                return true;

            if (origin.Contains("repl.co"))
                return true;

            // Dominios permitidos manualmente
            var allowedOrigins = new[]
            {
                "http://localhost",
                "https://liveapi.api.cr",
                "https://api.betmasterpro.net",
                "https://live.api.cr",
                "https://live.betmasterpro.net"
            };

            return allowedOrigins.Any(origin.StartsWith);
        })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// LOG A ARCHIVO PARA DEBUG
app.Use(async (context, next) =>
{
    try
    {
        var logFolder = Path.Combine(AppContext.BaseDirectory, "logs");
        if (!Directory.Exists(logFolder))
            Directory.CreateDirectory(logFolder);

        var logFile = Path.Combine(logFolder, "requests.log");

        var origin = context.Request.Headers["Origin"].ToString();
        var auth = context.Request.Headers["Authorization"].ToString();
        var path = context.Request.Path;

        var logMessage =
            $"--- REQUEST {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ---\n" +
            $"Origin: {origin}\n" +
            $"Auth: {auth}\n" +
            $"Path: {path}\n" +
            $"----------------------------------------------\n";

        await File.AppendAllTextAsync(logFile, logMessage);
    }
    catch
    {
        // No tirar error si el log falla
    }

    await next();
});


// Swagger solo en dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ORDEN CORRECTO:
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR
app.MapHub<Messages>("cnn").AllowAnonymous();

app.Run();
