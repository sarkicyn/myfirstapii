using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MyApiBlya.Services;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using AspNet.Security.OAuth.GitHub;
using System.Text.Json.Serialization;




var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers()
    // .AddJsonOptions(options =>
    // {
    //     options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    // })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var error = context.ModelState.Values
                .SelectMany(value => value.Errors)
                .FirstOrDefault();

            return new BadRequestObjectResult(new
            {
                message = error?.ErrorMessage ?? "Некорректные данные запроса."
            });
        };
    });
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste JWT token here without the Bearer prefix."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<HashPassword, HashPassik>();
builder.Services.AddScoped<IAddAction, Adding>();
builder.Services.AddScoped<IJwtCreate, JwtService>();
builder.Services.AddHostedService<LoggService>();
builder.Services.AddScoped<IRefreshing,Refresh>();
builder.Services.AddScoped<IGoogl,Google>(); 
builder.Services.AddScoped<IGitHubing,GithubService>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});


builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services

     .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddCookie("Sexcheme")
.AddGoogle(options =>
    {
        options.SignInScheme = "Sexcheme";
        options.ClientId = builder.Configuration[$"Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Google ClientId не настроен.");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google ClientSecret не настроен.");
            options.CallbackPath = "/google-path";
    })
    .AddGitHub(options =>
    {
        options.SignInScheme = "Sexcheme";
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]
            ?? throw new InvalidOperationException("GitHub ClientId не настроен.");
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]
            ?? throw new InvalidOperationException("GitHub ClientSecret не настроен.");
            options.CallbackPath = "/github-path";
    })
    .AddJwtBearer(options =>
    {
        var secretKey = builder.Configuration["JWT_KEY"]
            ?? builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)),

            ValidateIssuer = false,
            ValidateAudience = true,
            ValidAudience = "MyClients",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.Equals("/api/users/refresh", StringComparison.OrdinalIgnoreCase))
                {
                    context.NoResult();
                    return Task.CompletedTask;
                }

                var authorization = context.Request.Headers.Authorization.ToString();
                if (!string.IsNullOrWhiteSpace(authorization) &&
                    !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authorization.Trim();
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = async context =>
            {
                context.NoResult();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "JWT-токен недействителен или истек."
                });
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (context.Response.HasStarted)
                {
                    await Task.CompletedTask;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Необходим действительный JWT-токен."
                });
            },
            OnForbidden = async context =>
            {
                  
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "У вас нет прав для доступа к этому ресурсу."
                });     
            },
            
        };
    });
builder.Services.AddCors(options =>  //добавляем разрешенные адреса,с которых можно делать запрос к моему api 
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
       "http://localhost:5063"
               
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    
    errorApp.Run(async context =>
    {
        if (context.Response.HasStarted)
        {
            await Task.CompletedTask;
        }
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");

        logger.LogError(exception, "Необработанная ошибка при обработке запроса.");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Произошла непредвиденная ошибка сервера."
        });
    });
});

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.HasStarted)
    {
        return;
    }
    var message = response.StatusCode  switch
    {
        StatusCodes.Status400BadRequest => "Некорректный запрос.",
        StatusCodes.Status401Unauthorized => "Требуется авторизация.",
        StatusCodes.Status403Forbidden => "Доступ запрещен.",
        StatusCodes.Status404NotFound => "Ресурс не найден.",
        StatusCodes.Status405MethodNotAllowed => "HTTP-метод не разрешен для этого маршрута.",
        _ => "Запрос не выполнен."
    };

    await response.WriteAsJsonAsync(new { message });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
}
app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<PathRec>();
app.UseMiddleware<dataMiddleWare>();

app.MapControllers();

app.Run();
