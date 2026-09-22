using System.Text;
using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Repositories;
using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);



// Adding the Controllers
builder.Services.AddControllers();
// added CORS policy to allow requests from the frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy.WithOrigins(
            "http://localhost:5173",
            "https://kind-tree-05024de00.7.azurestaticapps.net"
        )
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Lets Swagger UI send "Authorization: Bearer <token>" so the protected
    // endpoints can be tried out after logging in through /api/auth/login.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by /api/auth/login.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --- authentication ---------------------------------------------------------
// The signing key is a secret: it belongs in App Service application settings
// (Jwt__Key) or user-secrets, never in appsettings.json in source control.
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
{
    if (builder.Environment.IsDevelopment())
    {
        // Local-only fallback so the project still runs straight after a clone.
        jwt.Key = "altrium-local-development-signing-key-change-me";
    }
    else
    {
        // Refusing to start is deliberate: falling back to a default key outside
        // Development would mean anyone who reads this file can forge a token.
        throw new InvalidOperationException(
            $"Jwt:Key is missing or shorter than 32 characters, and the environment is " +
            $"'{builder.Environment.EnvironmentName}' rather than Development.\n" +
            "Fix it in one of these ways:\n" +
            "  - running locally?  start the project itself so launchSettings.json applies:\n" +
            "      cd Altrium_Project_Backend\\Altrium_Project_Backend\n" +
            "      dotnet run --launch-profile http\n" +
            "  - running the built exe or publishing locally?  set the environment variable:\n" +
            "      set ASPNETCORE_ENVIRONMENT=Development     (or)   set Jwt__Key=<32+ characters>\n" +
            "  - deploying to Azure?  App Service > Configuration > Application settings > Jwt__Key");
    }
}
builder.Services.AddSingleton(jwt);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),   // default is 5 minutes of free expiry
        };
    });

// Closed by default: an action with no [Authorize] still requires a valid token, so
// forgetting the attribute locks an endpoint down instead of leaving it open.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// regitering the database connection factory
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
// registering security services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
// registering repositories
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();
builder.Services.AddScoped<IDealRepository, DealRepository>();
builder.Services.AddScoped<IEngagementRepository, EngagementRepository>();
builder.Services.AddScoped<IFollowUpRepository, FollowUpRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IArchiveRepository, ArchiveRepository>();



var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseCors("dev");

// Order matters: authentication establishes who the caller is, and only then can
// authorization decide what they may do. Swapping these two lines breaks both.
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

app.MapControllers();

app.Run();
