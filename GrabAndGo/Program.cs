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

builder.Services.AddScoped<ICartNotificationService, SignalRCartNotificationService>();
builder.Services.AddScoped<IGateNotificationService, GateNotificationService>();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1048576; // 1 Megabytes, 6,990 distinct, unique items
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

        // SignalR can't send custom headers during the WebSocket handshake (browser limitation),
        // so the Flutter app passes the JWT as ?access_token=... on the /hubs/* connection URL.
        // We forward that query token into the bearer pipeline only for hub paths.
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
app.MapHub<CartHub>("/hubs/cart");
app.Run();