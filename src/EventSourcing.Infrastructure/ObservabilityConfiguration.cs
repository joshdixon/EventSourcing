using Elastic.Apm.SerilogEnricher;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcing.Infrastructure;

file class ObservabilitySettings
{
    public string Environment { get; set; } = "dev";

    public string? ElasticsearchAddress { get; set; }
    public string? ElasticApmAddress { get; set; }
    public string? ElasticApmBearerToken { get; set; }
    public string? JaegerAddress { get; set; }

    public LogEventLevel MinimumLogLevel { get; set; } = LogEventLevel.Information;
}

public static class ObservabilityConfiguration
{
    public static void ConfigureObservability(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ObservabilitySettings>(builder.Configuration.GetSection(nameof(ObservabilitySettings)));

        builder.Host.UseSerilog(ConfigureLogging);

        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        ObservabilitySettings? observabilitySettings = builder.Configuration
          .GetSection(nameof(ObservabilitySettings))
          .Get<ObservabilitySettings>();

        ResourceBuilder resourceBuilder = ResourceBuilder
          .CreateDefault()
          .AddAttributes(new Dictionary<string, object>()
          {
              ["service.name"] = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Unknown",
              ["deployment.environment"] = observabilitySettings?.Environment ?? "dev",
              ["service.version"] = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "Unknown",
          })
          .AddService(ServiceActivitySource.ActivitySource.Name);

        void ConfigureOtlpExporter(OtlpExporterOptions options)
        {
            options.Endpoint = new Uri(observabilitySettings.ElasticApmAddress);
            options.Headers = $"Authorization=Bearer {observabilitySettings.ElasticApmBearerToken}";
            options.Protocol = OtlpExportProtocol.Grpc;
        }

        builder.Services.AddOpenTelemetryTracing((TracerProviderBuilder otlpBuilder) =>
        {
            otlpBuilder
              .SetSampler(new AlwaysOnSampler())
              .SetResourceBuilder(resourceBuilder)
              .AddSource(ServiceActivitySource.ActivitySource.Name)
              .AddAspNetCoreInstrumentation()
              .AddEntityFrameworkCoreInstrumentation()
              .AddHttpClientInstrumentation()
              .AddMassTransitInstrumentation()
              .AddSource(nameof(MassTransit));

            if (observabilitySettings?.ElasticApmAddress != null)
                otlpBuilder.AddOtlpExporter(ConfigureOtlpExporter);

            if (observabilitySettings?.JaegerAddress != null)
                otlpBuilder.AddJaegerExporter(configure =>
                {
                    configure.AgentHost = observabilitySettings.JaegerAddress;
                    configure.AgentPort = 6831;
                });
        });

        builder.Services.AddOpenTelemetryMetrics((MeterProviderBuilder otlpBuilder) =>
        {
            otlpBuilder
              .SetResourceBuilder(resourceBuilder)
              .AddRuntimeInstrumentation()
              .AddProcessInstrumentation()
              .AddAspNetCoreInstrumentation()
              .AddHttpClientInstrumentation()
              .AddMeter(ServiceMeters.Meter.Name)
              .AddMeter(nameof(MassTransit));

            if (observabilitySettings?.ElasticApmAddress != null)
                otlpBuilder.AddOtlpExporter(ConfigureOtlpExporter);
        });
    }

    public static Serilog.Core.LoggingLevelSwitch LogLevel { get; set; } = new Serilog.Core.LoggingLevelSwitch() { MinimumLevel = Serilog.Events.LogEventLevel.Information };

    private static void ConfigureLogging(HostBuilderContext hostContext, LoggerConfiguration loggerConfiguration)
    {
        Uri? elasticsearchAddress = null;

        ObservabilitySettings logSettings = hostContext.Configuration.GetSection(nameof(ObservabilitySettings)).Get<ObservabilitySettings>();

        if (!string.IsNullOrWhiteSpace(logSettings?.ElasticsearchAddress))
        {
            elasticsearchAddress = new Uri(logSettings.ElasticsearchAddress);
        }

        if (logSettings != null)
        {
            LogLevel.MinimumLevel = logSettings.MinimumLogLevel;
        }

        loggerConfiguration
          .Enrich.WithProperty("service.name", Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Unknown")
          .Enrich.WithProperty("deployment.environment", logSettings?.Environment ?? "dev")
          .Enrich.WithProperty("service.version", Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "Unknown")
          .Enrich.FromLogContext()
          .Enrich.WithSpan()
          // .Enrich.With<OpenTelemetryEnricher>()
          .Enrich.WithElasticApmCorrelationInfo()
          .MinimumLevel.ControlledBy(LogLevel)
          .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning) // Db command executions are Information level, therefore only include warnings
          .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning) // Context init is Information level
          .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
          .Filter.ByExcluding(Matching.WithProperty("RequestPath", "/health"))
          .Filter.ByExcluding(logEvent => logEvent.Exception is TaskCanceledException)
          .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug);

        // Setup elasticsearch logging if configured
        Console.WriteLine("Elasticsearch address: " + logSettings?.ElasticsearchAddress);
        if (elasticsearchAddress != null)
        {
            var sinkOptions = new ElasticsearchSinkOptions(elasticsearchAddress)
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                IndexFormat = "eventsourcing-{0:yyyy.MM}",
                BatchAction = ElasticOpType.Create,
                TypeName = null,
            };
            loggerConfiguration.WriteTo.Elasticsearch(sinkOptions);

            Log.Information("Sending Logs to Elasticsearch");
            Console.WriteLine("Sending Logs to Elasticsearch");
        }
    }
}
