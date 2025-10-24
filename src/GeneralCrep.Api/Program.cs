using GeneralCrep.Application.Interfaces;
using GeneralCrep.Application.Services;
using GeneralCrep.Infrastructure.External;
using GeneralCrep.Infrastructure.Processors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//============================
// Inyección de dependencias
//============================

// Gmail
builder.Services.AddSingleton<GmailApiClient>();
// File Processors
builder.Services.AddSingleton<FileProcessorFactory>();
// Servicios de aplicación
builder.Services.AddScoped<IGmailService, GmailService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowKnownOrigins", policy =>
    {
        policy.WithOrigins(
            "https://generalcrepapi-auazgxdebgducqfa.brazilsouth-01.azurewebsites.net",
            "https://localhost:7134",
            "http://localhost:7134"
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowKnownOrigins");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
