using Receiver.Api;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://localhost:5101");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddControllers();
builder.Services.AddScoped<ReceiveRequestHandler>();

var app = builder.Build();

app.MapControllers();
app.Run();
