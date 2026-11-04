// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration;

[Collection(nameof(telemetry_environment))]
public class when_a_span_with_grain_ids_is_exported : given.a_telemetry_configuration
{
    readonly CapturingHandler _handler = new();

    void Establish()
    {
        _configuration["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = "http://traces.example:4318/v1/traces";
        _configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf";
        _services.Configure<OtlpExporterOptions>(options =>
        {
            options.HttpClientFactory = () => new HttpClient(_handler);
            options.ExportProcessorType = ExportProcessorType.Simple;
        });
        _services.AddChronicleOpenTelemetry(_configuration);
        ResolveProviders();
    }

    void Because()
    {
        using var source = new ActivitySource(ChronicleActivity.SourceName);
        using (var activity = source.StartActivity("grain-call"))
        {
            activity?.SetTag("orleans.rpc.target_id", "grain/target-1");
            activity?.SetTag("orleans.rpc.source_id", "grain/source-1");
            activity?.SetTag("kept.tag", "kept-value");
        }

        _traces.ForceFlush();
    }

    [Fact] void should_export_the_span() => _handler.Payload.ShouldContain("kept-value");
    [Fact] void should_not_export_the_target_grain_id() => _handler.Payload.ShouldNotContain("orleans.rpc.target_id");
    [Fact] void should_not_export_the_source_grain_id() => _handler.Payload.ShouldNotContain("orleans.rpc.source_id");

    sealed class CapturingHandler : HttpMessageHandler
    {
        readonly StringBuilder _payload = new();

        public string Payload
        {
            get
            {
                lock (_payload)
                {
                    return _payload.ToString();
                }
            }
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => Capture(request);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(Capture(request));

        HttpResponseMessage Capture(HttpRequestMessage request)
        {
            using var body = new MemoryStream();
            request.Content?.CopyTo(body, null, CancellationToken.None);
            lock (_payload)
            {
                _payload.Append(Encoding.Latin1.GetString(body.ToArray()));
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
        }
    }
}
