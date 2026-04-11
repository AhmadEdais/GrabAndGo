using GrabAndGo.DataAccess; // For SqlExecutor
using GrabAndGo.DataAccess.Core;
using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.DataAccess.Repositories; // Adjust these based on your exact folder names
using GrabAndGo.Services.Implementations;
using GrabAndGo.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Get the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// 2. Register SqlExecutor as a Singleton
// This passes the connection string into the constructor we wrote earlier
builder.Services.AddSingleton(new SqlExecutor(connectionString!));

// 3. Register Repositories (DataAccess)
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 4. Register Services (Business Layer)
builder.Services.AddScoped<IUserService, UserService>();

// Standard Boilerplate
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();