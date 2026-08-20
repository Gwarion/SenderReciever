using Receiver.Api;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://localhost:5101");
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddCors(options =>
{
    options.AddPolicy("SenderUi", policy =>
    {
        policy
            .WithOrigins("http://localhost:5102")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddControllers();
builder.Services.AddScoped<ReceiveRequestHandler>();

var app = builder.Build();

app.UseCors("SenderUi");
app.MapControllers();
app.Run();
