using GrabAndGo.DataAccess; // For SqlExecutor
using GrabAndGo.DataAccess.Core;
using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.DataAccess.Repositories; // Adjust these based on your exact folder names
using GrabAndGo.Services.Implementations;
using GrabAndGo.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddSingleton(new SqlExecutor(connectionString!));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();

// Standard Boilerplate
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Define the "Bearer" security scheme.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token below.\n\nExample: 'eyJhbGciOiJIUzI1NiIs...'"
    });

    // Apply the Bearer scheme globally so secured endpoints require it
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
// Read JWT settings from appsettings.json or Environment Variables
var jwtKey = builder.Configuration["GRABANDGO_JWT_KEY"] ?? throw new InvalidOperationException("JWT Key is missing in configuration."); 
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GrabAndGoApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "GrabAndGoUsers";

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            // Optional: Removes the default 5-minute clock skew so tokens expire at the exact second
            ClockSkew = TimeSpan.Zero
        };
    });

// Make sure to add Authorization as well
builder.Services.AddAuthorization();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();