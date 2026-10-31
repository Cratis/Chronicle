// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceHttpClientFactory.given;

public class a_guarded_capture_client : Specification
{
    protected ChronicleOptions _options;
    protected ServiceProvider _services;
    protected List<string> _warnings;
    protected recording_transport _transport;
    protected ExternalServiceDefinition _definition;

    protected ExternalServiceHttpClientFactory Factory => ActivatorUtilities.CreateInstance<ExternalServiceHttpClientFactory>(_services);

    void Establish()
    {
        _warnings = [];
        _options = new() { ExternalServices = new() { Validation = WebhookValidationMode.Warn } };
        _transport = new();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var resolver = Substitute.For<IWebhookHostResolver>();
        resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("93.184.216.34")]);
        var logger = Substitute.For<ILogger<WebhookTargetPolicy>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        logger.When(value => value.Log(Arg.Any<LogLevel>(), Arg.Any<EventId>(), Arg.Any<LoggerMessageState>(), Arg.Any<Exception?>(), Arg.Any<Func<LoggerMessageState, Exception?, string>>()))
            .Do(call =>
            {
                if (call.ArgAt<LogLevel>(0) == LogLevel.Warning)
                {
                    _warnings.Add(call.ArgAt<Func<LoggerMessageState, Exception?, string>>(4)(call.ArgAt<LoggerMessageState>(2), call.ArgAt<Exception?>(3)));
                }
            });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_ => Options.Create(_options));
        services.AddSingleton(environment);
        services.AddSingleton(resolver);
        services.AddSingleton(logger);
        var builder = Substitute.For<ISiloBuilder>();
        builder.Services.Returns(services);
        builder.AddExternalServiceHttpClient();
        services.AddHttpClient(ExternalServiceHttpClientFactory.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => _transport)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
            {
                var pool = handlers.OfType<WebhookConnectionPoolHandler>().Single();
                handlers.Remove(pool);
                pool.Dispose();
            });
        _services = services.BuildServiceProvider();
        _definition = new("source", "Source", new(ExternalServiceEndpointType.Http, Http: new("https://capture.example", new Concepts.Security.BearerTokenAuthorization("token"), new Dictionary<string, string>())));
    }

    void Destroy() => _services.Dispose();

    public class recording_transport : HttpMessageHandler
    {
        public List<Uri> Targets { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Targets.Add(request.RequestUri);
            var response = request.RequestUri.AbsolutePath.Contains(".well-known", StringComparison.Ordinal)
                ? JsonContent.Create(new { token_endpoint = "https://tokens.example/token" })
                : JsonContent.Create(new { access_token = "access-token", expires_in = 3600 });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = response });
        }
    }
}
