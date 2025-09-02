using System.Reflection;
using Api.Endpoints;
using Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddCors();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Yet Another Garmin Connect Client API", Version = "v1" });
    // Enable XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();
var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

app.MapGet("/", () => new
{
    name = "yet-another-garmin-connect-client-api",
    projectUrl = "https://github.com/lswiderski/yet-another-garmin-connect-client",
    uploadBodyCompositiontEndpoint = "/upload",
    uploadBloodPressureEndpoint = "/uploadbloodpressure"
});

app.MapGet("/health", () => Results.Ok("Healthy"));
app.MapGet("/ping", () => Results.Ok("pong"));

app.MapUploadEndpoints();
app.MapBloodPressureEndpoints();


if (!appSettings.General.DisableSwagger)
{
    // Enable Swagger middleware
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Yet Another Garmin Connect Client API v1");
    });
}

app.Run();
