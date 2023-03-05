using EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventSourcing.Infrastructure;

public static class EventStoreConfiguration
{
    private class EventStoreSettings
    {
        public string ConnectionString { get; set; } = "esdb://eventstoredb:2113?tls=false";
    }

    public static void ConfigureEventStore(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<EventStoreSettings>(builder.Configuration.GetSection(nameof(EventStoreSettings)));

        builder.Services.AddSingleton(provider =>
        {
            EventStoreSettings eventStoreSettings = provider.GetRequiredService<IOptions<EventStoreSettings>>().Value;

            var clientSettings = EventStoreClientSettings
                .Create(eventStoreSettings.ConnectionString);

            return new EventStoreClient(clientSettings);
        });
    }
}