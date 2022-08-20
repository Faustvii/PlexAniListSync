using System.Text.Json.Serialization;
using PlexAniListSync.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
.AddJsonOptions(opt => opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions();
// Setup Dependency Injection

builder.Services.AddPlex(builder.Configuration);
builder.Services.AddAnilist(builder.Configuration);
builder.Services.AddWebhooks(builder.Configuration);
builder.Services.AddExtractor();
builder.Services.AddHttpClients();
builder.Services.AddHostedServices();
builder.Services.AddMappingServices();
builder.Services.AddParsers();
builder.Services.AddDataCache();

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
