// QuestPDF Community License — must be acknowledged once at startup before any PDF is rendered.


QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddSingleton(new SqlExecutor(connectionString!));
builder.Services.AddHostedService<MqttVisionWorker>();
builder.Services.AddHostedService<InvoiceWorker>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ISessionService, SessionService>();

builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();

builder.Services.AddScoped<IVisionSystemRepository, VisionSystemRepository>();
builder.Services.AddScoped<IVisionSystemService, VisionSystemService>();

builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddScoped<ICheckoutRepository, CheckoutRepository>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();

builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<IGateService, GateService>();
builder.Services.AddScoped<IGateRepository, GateRepository>();

builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();

builder.Services.AddScoped<ICartNotificationService, SignalRCartNotificationService>();
builder.Services.AddScoped<IGateNotificationService, GateNotificationService>();
builder.Services.AddScoped<IInvoiceNotificationService, InvoiceNotificationService>();
builder.Services.AddScoped<HelperMethods>();
builder.Services.AddScoped<ILogger, Logger<Program>>();
builder.Services.AddScoped<IGateQrTokenRefreshService, GateQrTokenRefreshService>();
builder.Services.AddScoped<IBroadcastSessionToDemo, BroadcastSessionToDemo>();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1048576; // 1 Megabytes, 6,990 distinct, unique items
});
builder.Services.AddHttpClient("VisionSystem", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["VisionSystem:BaseUrl"]!);
});
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
    options.AddSecurityDefinition("GateApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "Hardware Gate API Key"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
             new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "GateApiKey"
                    }
        },
        Array.Empty<string>()
        }
    });
    options.AddSecurityDefinition("VisionApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "Hardware Vision API Key"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
             new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "VisionApiKey"
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

            ClockSkew = TimeSpan.Zero
        };


        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://127.0.0.1:5500",  // Live Server default
                "http://localhost:5500",
                "https://192.168.1.7:5500",
                "http://192.168.1.7:5500"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
// Make sure to add Authorization as well
builder.Services.AddAuthorization();
var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();

//}
app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CartHub>("/hubs/cart");
app.MapHub<GateHub>("/hubs/gate");
app.MapHub<InvoiceHub>("/hubs/invoice");
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    environment = app.Environment.EnvironmentName
}));
app.Run();