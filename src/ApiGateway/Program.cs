using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApiGatewayService;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddHttpClient<FlightsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:FlightService"] ?? "http://flight_service:8060");
});

builder.Services.AddHttpClient<BonusClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:BonusService"] ?? "http://bonus_service:8050");
});

builder.Services.AddHttpClient<TicketsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:TicketService"] ?? "http://ticket_service:8070");
});

builder.Services.AddSingleton<IBonusRefundQueue, BonusRefundQueue>();
builder.Services.AddHostedService<BonusRefundWorker>();


builder.Services.AddSingleton<ICircuitBreaker>(
    _ => new CircuitBreaker(
        failureThreshold: 3,
        openTimeout: TimeSpan.FromSeconds(15)
    )
);
builder.Services.AddScoped<FlightService>();
builder.Services.AddScoped<TicketsService>();
builder.Services.AddScoped<BonusService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "GatewayAPI";
    config.Title = "GatewayAPI v1";
    config.Version = "v1";
});

var app = builder.Build();


app.UseOpenApi();
app.UseSwaggerUi();


app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
