using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cratis.OpenTelemetry;

/// <summary>
/// Shared host setup for Cratis telemetry with opt-in OTLP export.
/// </summary>
public static class OpenTelemetryExtensions
{
	private sealed class TelemetryServiceProvider(IServiceProvider provider, IConfiguration configuration) : IKeyedServiceProvider, IServiceProvider, ISupportRequiredService
	{
		public object? GetService(Type serviceType)
		{
			if (!(serviceType == typeof(IConfiguration)))
			{
				return provider.GetService(serviceType);
			}
			return configuration;
		}

		public object GetRequiredService(Type serviceType)
		{
			if (!(serviceType == typeof(IConfiguration)))
			{
				return provider.GetRequiredService(serviceType);
			}
			return configuration;
		}

		public object? GetKeyedService(Type serviceType, object? serviceKey)
		{
			if (!(provider is IKeyedServiceProvider keyedServiceProvider))
			{
				throw new InvalidOperationException("This service provider doesn't support keyed services.");
			}
			return keyedServiceProvider.GetKeyedService(serviceType, serviceKey);
		}

		public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
		{
			return provider.GetRequiredKeyedService(serviceType, serviceKey);
		}
	}

	private static readonly string[] _signals = new string[3] { "TRACES", "METRICS", "LOGS" };

	/// <summary>
	/// Adds Cratis sources, meters, host instrumentation, scoped logs and resource defaults.
	/// </summary>
	/// <param name="builder">The application builder.</param>
	/// <param name="configure">Optional application defaults and extra instrumentation.</param>
	/// <returns>The application builder for continuation.</returns>
	public static IHostApplicationBuilder AddCratisOpenTelemetry(this IHostApplicationBuilder builder, Action<CratisOpenTelemetryOptions>? configure = null)
	{
		OpenTelemetryServicesExtensions.AddOpenTelemetry(builder.Services).WithCratis(builder.Configuration, configure);
		return builder;
	}

	/// <summary>
	/// Adds Cratis telemetry, resolving application configuration from the service provider or using standard environment variables.
	/// </summary>
	/// <param name="builder">The OpenTelemetry builder.</param>
	/// <param name="configure">Optional application defaults and extra instrumentation.</param>
	/// <returns>The OpenTelemetry builder for continuation.</returns>
	public static OpenTelemetryBuilder WithCratis(this OpenTelemetryBuilder builder, Action<CratisOpenTelemetryOptions>? configure = null)
	{
		return WithCratis(builder, (IServiceProvider provider) => provider.GetRequiredService<IConfiguration>(), configure);
	}

	/// <summary>
	/// Adds Cratis telemetry with standard OTEL configuration keys supplied by the application.
	/// </summary>
	/// <param name="builder">The OpenTelemetry builder.</param>
	/// <param name="configuration">Standard OTEL keys, such as OTEL_SERVICE_NAME and OTEL_EXPORTER_OTLP_ENDPOINT.</param>
	/// <param name="configure">Optional application defaults and extra instrumentation.</param>
	/// <returns>The OpenTelemetry builder for continuation.</returns>
	/// <exception cref="T:Cratis.OpenTelemetry.InvalidOpenTelemetryEndpoint">An enabled OTLP endpoint is invalid.</exception>
	public static OpenTelemetryBuilder WithCratis(this OpenTelemetryBuilder builder, IConfiguration configuration, Action<CratisOpenTelemetryOptions>? configure = null)
	{
		if (IsDisabled(configuration))
		{
			builder.WithTracing((Action<TracerProviderBuilder>)delegate
			{
			}).WithMetrics((Action<MeterProviderBuilder>)delegate
			{
			}).WithLogging((Action<LoggerProviderBuilder>)delegate
			{
			});
			ConfigureProviderFactory<TracerProvider>(builder.Services, (IServiceProvider _) => configuration);
			ConfigureProviderFactory<MeterProvider>(builder.Services, (IServiceProvider _) => configuration);
			ConfigureProviderFactory<LoggerProvider>(builder.Services, (IServiceProvider _) => configuration);
			return builder;
		}
		ValidateEndpoints(configuration);
		return WithCratis(builder, (IServiceProvider _) => configuration, configure);
	}

	private static OpenTelemetryBuilder WithCratis(OpenTelemetryBuilder builder, Func<IServiceProvider, IConfiguration> resolveConfiguration, Action<CratisOpenTelemetryOptions>? configure)
	{
		CratisOpenTelemetryOptions options = new CratisOpenTelemetryOptions();
		configure?.Invoke(options);
		ConfigureResourceDefaults(builder.Services, options);
		builder.WithLogging((Action<LoggerProviderBuilder>)delegate
		{
		}, (Action<OpenTelemetryLoggerOptions>)delegate(OpenTelemetryLoggerOptions logging)
		{
			logging.IncludeScopes = true;
			logging.IncludeFormattedMessage = true;
		}).WithTracing((Action<TracerProviderBuilder>)delegate
		{
		}).WithMetrics((Action<MeterProviderBuilder>)delegate
		{
		});
		ConfigureProviderFactory<TracerProvider>(builder.Services, resolveConfiguration);
		ConfigureProviderFactory<MeterProvider>(builder.Services, resolveConfiguration);
		ConfigureProviderFactory<LoggerProvider>(builder.Services, resolveConfiguration);
		builder.Services.AddOptions<OtlpExporterOptions>();
		builder.Services.AddOptions<PeriodicExportingMetricReaderOptions>().Configure(delegate(PeriodicExportingMetricReaderOptions reader, IServiceProvider provider)
		{
			ConfigureReader(reader, resolveConfiguration(provider));
		});
		OpenTelemetryDependencyInjectionTracingServiceCollectionExtensions.ConfigureOpenTelemetryTracerProvider(builder.Services, (Action<IServiceProvider, TracerProviderBuilder>)delegate(IServiceProvider provider, TracerProviderBuilder tracing)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Expected O, but got Unknown
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Expected O, but got Unknown
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Expected O, but got Unknown
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Expected O, but got Unknown
			IConfiguration configuration = resolveConfiguration(provider);
			if (IsDisabled(configuration))
			{
				TracerProviderBuilderExtensions.SetSampler(tracing, (Sampler)new AlwaysOffSampler());
			}
			else
			{
				ValidateEndpoints(configuration);
				TracerProviderBuilderExtensions.ConfigureResource(tracing, (Action<ResourceBuilder>)delegate(ResourceBuilder resource)
				{
					ConfigureResource(resource, configuration);
				});
				tracing.AddCratisInstrumentation();
				if (ShouldExport(configuration, "TRACES"))
				{
					OtlpExporterOptions val = ExporterOptions(provider, configuration, "TRACES", "traces");
					BatchExportProcessorOptions<Activity> batchExportProcessorOptions = val.BatchExportProcessorOptions;
					TracerProviderBuilderExtensions.AddProcessor(tracing, (BaseProcessor<Activity>)(((int)val.ExportProcessorType == 0) ? new SimpleActivityExportProcessor((BaseExporter<Activity>)new OtlpTraceExporter(val)) : new BatchActivityExportProcessor((BaseExporter<Activity>)new OtlpTraceExporter(val), batchExportProcessorOptions.MaxQueueSize, batchExportProcessorOptions.ScheduledDelayMilliseconds, batchExportProcessorOptions.ExporterTimeoutMilliseconds, batchExportProcessorOptions.MaxExportBatchSize)));
				}
			}
		});
		OpenTelemetryDependencyInjectionMetricsServiceCollectionExtensions.ConfigureOpenTelemetryMeterProvider(builder.Services, (Action<IServiceProvider, MeterProviderBuilder>)delegate(IServiceProvider provider, MeterProviderBuilder metrics)
		{
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Expected O, but got Unknown
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Expected O, but got Unknown
			IConfiguration configuration = resolveConfiguration(provider);
			if (!IsDisabled(configuration))
			{
				ValidateEndpoints(configuration);
				MeterProviderBuilderExtensions.ConfigureResource(metrics, (Action<ResourceBuilder>)delegate(ResourceBuilder resource)
				{
					ConfigureResource(resource, configuration);
				});
				metrics.AddCratisInstrumentation();
				if (ShouldExport(configuration, "METRICS"))
				{
					PeriodicExportingMetricReaderOptions value = provider.GetRequiredService<IOptions<PeriodicExportingMetricReaderOptions>>().Value;
					MeterProviderBuilderExtensions.AddReader(metrics, (MetricReader)new PeriodicExportingMetricReader((BaseExporter<Metric>)new OtlpMetricExporter(ExporterOptions(provider, configuration, "METRICS", "metrics")), value.ExportIntervalMilliseconds ?? 60000, value.ExportTimeoutMilliseconds ?? 30000));
				}
			}
		});
		ConfigureLogging(builder, resolveConfiguration, options);
		builder.WithTracing((Action<TracerProviderBuilder>)delegate(TracerProviderBuilder tracing)
		{
			HttpClientInstrumentationTracerProviderBuilderExtensions.AddHttpClientInstrumentation(AspNetCoreInstrumentationTracerProviderBuilderExtensions.AddAspNetCoreInstrumentation(tracing, (Action<AspNetCoreTraceInstrumentationOptions>)delegate(AspNetCoreTraceInstrumentationOptions instrumentation)
			{
				instrumentation.Filter = HealthRequestFilter.ShouldTrace;
			}));
			options.ConfigureTracing?.Invoke(tracing);
		}).WithMetrics((Action<MeterProviderBuilder>)delegate(MeterProviderBuilder metrics)
		{
			MeterProviderBuilderExtensions.AddRuntimeInstrumentation(HttpClientInstrumentationMeterProviderBuilderExtensions.AddHttpClientInstrumentation(AspNetCoreInstrumentationMeterProviderBuilderExtensions.AddAspNetCoreInstrumentation(metrics)));
			options.ConfigureMetrics?.Invoke(metrics);
		});
		OpenTelemetryDependencyInjectionTracingServiceCollectionExtensions.ConfigureOpenTelemetryTracerProvider(builder.Services, (Action<IServiceProvider, TracerProviderBuilder>)delegate(IServiceProvider provider, TracerProviderBuilder tracing)
		{
			ConfigureTracePolicy(tracing, resolveConfiguration(provider));
		});
		return builder;
	}

	private static void ConfigureLogging(OpenTelemetryBuilder builder, Func<IServiceProvider, IConfiguration> resolveConfiguration, CratisOpenTelemetryOptions options)
	{
		builder.Services.AddOptions<LogRecordExportProcessorOptions>().Configure(delegate(LogRecordExportProcessorOptions processor, IServiceProvider provider)
		{
			IConfiguration configuration = resolveConfiguration(provider);
			OpenTelemetryLogBatch.Configure(processor.BatchExportProcessorOptions, (string key) => Read(configuration, key));
		});
		OpenTelemetryDependencyInjectionLoggingServiceCollectionExtensions.ConfigureOpenTelemetryLoggerProvider(builder.Services, (Action<IServiceProvider, LoggerProviderBuilder>)delegate(IServiceProvider provider, LoggerProviderBuilder logging)
		{
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Expected O, but got Unknown
			IConfiguration configuration = resolveConfiguration(provider);
			if (!IsDisabled(configuration))
			{
				ValidateEndpoints(configuration);
				LoggerProviderBuilderExtensions.ConfigureResource(logging, (Action<ResourceBuilder>)delegate(ResourceBuilder resource)
				{
					ConfigureResource(resource, configuration);
				});
				if (!options.IncludeExceptionDetails)
				{
					LoggerProviderBuilderExtensions.AddProcessor(logging, (BaseProcessor<LogRecord>)(object)new ExceptionTypeLogProcessor());
				}
				if (ShouldExport(configuration, "LOGS"))
				{
					BatchExportLogRecordProcessorOptions batchExportProcessorOptions = provider.GetRequiredService<IOptions<LogRecordExportProcessorOptions>>().Value.BatchExportProcessorOptions;
					LoggerProviderBuilderExtensions.AddProcessor(logging, (BaseProcessor<LogRecord>)(object)OpenTelemetryLogBatch.Create(new OtlpLogExporter(ExporterOptions(provider, configuration, "LOGS", "logs")), batchExportProcessorOptions));
				}
			}
		});
	}

	private static void ConfigureTracePolicy(TracerProviderBuilder tracing, IConfiguration configuration)
	{
		if (!IsDisabled(configuration))
		{
			ConfigureSampler(tracing, configuration);
			OpenTelemetryPropagation.Configure(Read(configuration, "OTEL_PROPAGATORS"));
		}
	}

	private static void ConfigureProviderFactory<TProvider>(IServiceCollection services, Func<IServiceProvider, IConfiguration> resolveConfiguration)
	{
		ServiceDescriptor serviceDescriptor = services.LastOrDefault((ServiceDescriptor service) => service.ServiceType == typeof(TProvider));
		Func<IServiceProvider, object> factory = serviceDescriptor?.ImplementationFactory;
		if (factory != null)
		{
			services[services.IndexOf(serviceDescriptor)] = new ServiceDescriptor(typeof(TProvider), delegate(IServiceProvider provider)
			{
				IConfigurationRoot configuration = new ConfigurationBuilder().AddConfiguration(provider.GetRequiredService<IConfiguration>()).AddInMemoryCollection(new Dictionary<string, string> { ["OTEL_SDK_DISABLED"] = (IsDisabled(resolveConfiguration(provider)) ? "true" : "false") }).Build();
				return factory(new TelemetryServiceProvider(provider, configuration));
			}, serviceDescriptor.Lifetime);
		}
	}

	private static OtlpExporterOptions ExporterOptions(IServiceProvider provider, IConfiguration configuration, string signal, string path)
	{
		OtlpExporterOptions obj = provider.GetRequiredService<IOptionsFactory<OtlpExporterOptions>>().Create(Options.DefaultName);
		ConfigureExporter(obj, configuration, signal, path);
		return obj;
	}

	private static bool IsDisabled(IConfiguration configuration)
	{
		return string.Equals(Read(configuration, "OTEL_SDK_DISABLED"), "true", StringComparison.OrdinalIgnoreCase);
	}

	private static void ConfigureReader(PeriodicExportingMetricReaderOptions reader, IConfiguration configuration)
	{
		if (int.TryParse(Read(configuration, "OTEL_METRIC_EXPORT_INTERVAL"), CultureInfo.InvariantCulture, out var result) && result > 0)
		{
			reader.ExportIntervalMilliseconds = result;
		}
		if (int.TryParse(Read(configuration, "OTEL_METRIC_EXPORT_TIMEOUT"), CultureInfo.InvariantCulture, out var result2) && result2 >= 0)
		{
			reader.ExportTimeoutMilliseconds = result2;
		}
	}

	private static void ConfigureSampler(TracerProviderBuilder tracing, IConfiguration configuration)
	{
		Sampler val = OpenTelemetrySampler.Create(Read(configuration, "OTEL_TRACES_SAMPLER"), Read(configuration, "OTEL_TRACES_SAMPLER_ARG"));
		if (val != null)
		{
			TracerProviderBuilderExtensions.SetSampler(tracing, val);
		}
	}

	private static string? Read(IConfiguration configuration, string key)
	{
		string environmentVariable = Environment.GetEnvironmentVariable(key);
		string text = (string.IsNullOrWhiteSpace(environmentVariable) ? configuration[key] : environmentVariable);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return null;
	}

	private static void ConfigureResource(ResourceBuilder resource, IConfiguration configuration)
	{
		string text = Read(configuration, "OTEL_RESOURCE_ATTRIBUTES");
		if (!string.IsNullOrWhiteSpace(text))
		{
			ResourceBuilderExtensions.AddAttributes(resource, from attribute in text.Split(',')
				select attribute.Split('=', 2) into parts
				where parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0])
				select new KeyValuePair<string, object>(parts[0].Trim(), Uri.UnescapeDataString(parts[1].Trim())));
		}
		string value = Read(configuration, "OTEL_SERVICE_NAME");
		if (!string.IsNullOrWhiteSpace(value))
		{
			ResourceBuilderExtensions.AddAttributes(resource, (IEnumerable<KeyValuePair<string, object>>)new <>z__ReadOnlySingleElementList<KeyValuePair<string, object>>(new KeyValuePair<string, object>("service.name", value)));
		}
	}

	private static bool ShouldExport(IConfiguration configuration, string signal)
	{
		if (!string.IsNullOrWhiteSpace(Read(configuration, "OTEL_EXPORTER_OTLP_" + signal + "_ENDPOINT") ?? Read(configuration, "OTEL_EXPORTER_OTLP_ENDPOINT")))
		{
			return Read(configuration, "OTEL_" + signal + "_EXPORTER")?.Split(',').Any((string exporter) => exporter.Trim().Equals("otlp", StringComparison.OrdinalIgnoreCase)) ?? true;
		}
		return false;
	}

	private static void ValidateEndpoints(IConfiguration configuration)
	{
		foreach (string item in _signals.Where((string signal) => ShouldExport(configuration, signal)))
		{
			string text = "OTEL_EXPORTER_OTLP_" + item + "_ENDPOINT";
			string key = ((Read(configuration, text) == null) ? "OTEL_EXPORTER_OTLP_ENDPOINT" : text);
			Endpoint(Read(configuration, key), key);
		}
	}

	private static OtlpExportProtocol Protocol(IConfiguration configuration, string? signal)
	{
		if (string.Equals((((signal == null) ? null : Read(configuration, "OTEL_EXPORTER_OTLP_" + signal + "_PROTOCOL")) ?? Read(configuration, "OTEL_EXPORTER_OTLP_PROTOCOL"))?.Trim(), "http/protobuf", StringComparison.Ordinal))
		{
			return (OtlpExportProtocol)1;
		}
		return (OtlpExportProtocol)0;
	}

	private static void ConfigureExporter(OtlpExporterOptions exporter, IConfiguration configuration, string signal, string path)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		string text = Read(configuration, "OTEL_EXPORTER_OTLP_" + signal + "_ENDPOINT");
		string value = text ?? Read(configuration, "OTEL_EXPORTER_OTLP_ENDPOINT");
		exporter.Protocol = Protocol(configuration, signal);
		Uri uri = Endpoint(value, (text == null) ? "OTEL_EXPORTER_OTLP_ENDPOINT" : ("OTEL_EXPORTER_OTLP_" + signal + "_ENDPOINT"));
		exporter.Endpoint = (((int)exporter.Protocol == 1 && text == null) ? new UriBuilder(uri)
		{
			Path = uri.AbsolutePath.TrimEnd('/') + "/v1/" + path
		}.Uri : uri);
		exporter.Headers = Read(configuration, "OTEL_EXPORTER_OTLP_" + signal + "_HEADERS") ?? Read(configuration, "OTEL_EXPORTER_OTLP_HEADERS");
		OpenTelemetryExporterSettings.Apply(exporter, Read(configuration, "OTEL_EXPORTER_OTLP_" + signal + "_TIMEOUT") ?? Read(configuration, "OTEL_EXPORTER_OTLP_TIMEOUT"), Read(configuration, "OTEL_EXPORTER_OTLP_" + signal + "_COMPRESSION") ?? Read(configuration, "OTEL_EXPORTER_OTLP_COMPRESSION"));
	}

	private static Uri Endpoint(string value, string key)
	{
		Uri result;
		bool flag = !Uri.TryCreate(value, UriKind.Absolute, out result);
		if (!flag)
		{
			string scheme = result.Scheme;
			bool flag2 = ((scheme == "http" || scheme == "https") ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			throw new InvalidOpenTelemetryEndpoint(key);
		}
		return result;
	}

	private static void ConfigureResourceDefaults(IServiceCollection services, CratisOpenTelemetryOptions options)
	{
		int count = services.Count;
		OpenTelemetryDependencyInjectionTracingServiceCollectionExtensions.ConfigureOpenTelemetryTracerProvider(services, (Action<IServiceProvider, TracerProviderBuilder>)delegate(IServiceProvider _, TracerProviderBuilder tracing)
		{
			TracerProviderBuilderExtensions.ConfigureResource(tracing, (Action<ResourceBuilder>)AddDefaults);
		});
		OpenTelemetryDependencyInjectionMetricsServiceCollectionExtensions.ConfigureOpenTelemetryMeterProvider(services, (Action<IServiceProvider, MeterProviderBuilder>)delegate(IServiceProvider _, MeterProviderBuilder metrics)
		{
			MeterProviderBuilderExtensions.ConfigureResource(metrics, (Action<ResourceBuilder>)AddDefaults);
		});
		OpenTelemetryDependencyInjectionLoggingServiceCollectionExtensions.ConfigureOpenTelemetryLoggerProvider(services, (Action<IServiceProvider, LoggerProviderBuilder>)delegate(IServiceProvider _, LoggerProviderBuilder logging)
		{
			LoggerProviderBuilderExtensions.ConfigureResource(logging, (Action<ResourceBuilder>)AddDefaults);
		});
		ServiceDescriptor[] array = services.Skip(count).Reverse().ToArray();
		foreach (ServiceDescriptor item in array)
		{
			services.Remove(item);
			services.Insert(0, item);
		}
		void AddDefaults(ResourceBuilder resource)
		{
			ResourceBuilderExtensions.AddService(resource, options.ServiceName, (string)null, options.ServiceVersion, true, (string)null);
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
