using EventSourcing.Infrastructure;
using EventSourcing.Infrastructure.Domain;
using EventSourcing.Service.Features.Products;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.ConfigureMessaging(factoryConfigurator => { });
builder.ConfigureObservability();
builder.ConfigureEventStore();

builder.Services.AddScoped<IAggregateRepository<Product>, EventStoreAggregateRepository<Product>>();

builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.Run();