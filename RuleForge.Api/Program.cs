using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RuleForge.Api.Extensions;
using RuleForge.Api.Options;
using RuleForge.Api.Services;
using RuleForge.Application.Evaluate;
using RuleForge.Application.Rules.Validation;
using RuleForge.Infrastructure.Evaluate;
using RuleForge.Infrastructure.Persistence;
using RuleForge.Infrastructure.Rules;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddControllers();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateRuleRequestValidator>();

    builder.Services.AddProblemDetails();
    builder.Services.AddSwaggerWithBearer();

    builder.Services.AddDbContext<RuleForgeDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

    var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
    var jwt = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
    if (string.IsNullOrEmpty(jwt.Key))
        throw new InvalidOperationException("Jwt:Key is not configured.");
    if (jwt.Key.Length < 32)
        throw new InvalidOperationException("Jwt:Key must be at least 32 characters long for HMAC-SHA256.");
    builder.Services.AddSingleton(jwt);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddMemoryCache();

    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<RuleForge.Application.Rules.IRuleService, RuleService>();
    builder.Services.AddScoped<IEvaluationService, EvaluationService>();

    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("fixed", limiterOptions =>
        {
            limiterOptions.PermitLimit = 100;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 10;
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature?.Error;

            if (exception is not null)
            {
                Log.Error(exception, "Unhandled exception occurred. Path: {Path}", context.Request.Path);
            }

            var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "An unexpected error occurred while processing the request."
            };

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem
            });
        });
    });

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors();

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
        throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
