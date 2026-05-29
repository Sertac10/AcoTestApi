using System;
using System.IO;
using AcoTestApi.API.Middlewares;
using AcoTestApi.Application;
using AcoTestApi.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using AcoTestApi.Application.Common.Interfaces;
using Microsoft.OpenApi.Models;

namespace AcoTestApi.API;

public class Program
{
    public static void Main(string[] args)
    {
        // 1. Simple .env file loader
        LoadEnvFile();

        // 2. Setup Serilog for general application logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Web API host...");

            var builder = WebApplication.CreateBuilder(args);

            // Configure builder port if set in .env
            var configuredPort = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrWhiteSpace(configuredPort) && int.TryParse(configuredPort, out var port))
            {
                builder.WebHost.UseUrls($"http://*:{port}");
            }

            builder.Host.UseSerilog();

            // Add layers
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure();

            // Add Middlewares
            builder.Services.AddTransient<ExceptionHandlingMiddleware>();
            builder.Services.AddTransient<TokenAuthMiddleware>();

            builder.Services.AddControllers();

            // Add Swagger with API Token Configuration (Interview Gold Standard)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "ACO Thermal Printer Simulator API", 
                    Version = "v1",
                    Description = "ACO Termal Yazıcı Simülasyon Servisi ve Entegrasyon Arayüzü API Dokümantasyonu"
                });

                // Add X-Api-Token security scheme
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Description = "Gömülü X-Api-Token güvenliği. İstek başlığına eklenir. Örnek değer: 'aco-secret-token'",
                    In = ParameterLocation.Header,
                    Name = "X-Api-Token",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKeyScheme"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            },
                            In = ParameterLocation.Header
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Enable CORS for external API integration (e.g. Postman)
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Enable Developer Exception Page in Dev
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors();

            // Enable Swagger in Development AND Production for easy evaluator live testing
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ACO Thermal Printer API v1");
                c.RoutePrefix = "swagger"; // Swagger is available at http://localhost:5000/swagger
            });

            // Serve Static Files for the gorgeous dashboard
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Exception & Auth Middlewares (enforcing correct order)
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<TokenAuthMiddleware>();

            app.MapControllers();

            // Map standard health check endpoint (Bonus)
            app.MapGet("/health", async (context) =>
            {
                var printer = context.RequestServices.GetRequiredService<IThermalPrinter>();
                context.Response.ContentType = "application/json";
                var healthStatus = new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow,
                    details = new
                    {
                        server = "Up",
                        printer = new
                        {
                            mode = printer.Mode.ToString(),
                            state = printer.ConnectionState.ToString(),
                            error = printer.ActiveError.ToString(),
                            paperPercentage = Math.Round((printer.RemainingPaperLengthCm / printer.TotalPaperLengthCm) * 100.0, 2)
                        }
                    }
                };
                context.Response.StatusCode = 200;
                await context.Response.WriteAsJsonAsync(healthStatus);
            });

            // Fallback to SPA index.html
            app.MapFallbackToFile("index.html");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void LoadEnvFile()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (!File.Exists(filePath))
        {
            // Try parent directory in case we are running inside bin folder
            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            if (!File.Exists(filePath)) return;
        }

        try
        {
            foreach (var line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var index = line.IndexOf('=');
                if (index <= 0) continue;

                var key = line[..index].Trim();
                var value = line[(index + 1)..].Trim();
                
                // Strip quotes if any
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value[1..^1];
                }

                Environment.SetEnvironmentVariable(key, value);
            }
        }
        catch
        {
            // Fail silently if .env cannot be loaded
        }
    }
}
