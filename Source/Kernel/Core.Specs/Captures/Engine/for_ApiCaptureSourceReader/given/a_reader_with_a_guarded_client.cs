// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.ExternalServices;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ExternalServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.given;

public class a_reader_with_a_guarded_client : Specification
{
    protected EventStoreName _eventStore = "orders";
    protected SourceDefinition _source;
    protected ApiCaptureSourceReader _reader;
    protected IWebhookHostResolver _resolver;
    protected List<string> _warnings;
    protected recording_transport _transport;
    ServiceProvider _services;

    void Establish()
    {
        _source = new(SourceType.Api, Api: "OrdersApi", Route: "items");
        _warnings = [];
        _transport = new();
        _resolver = Substitute.For<IWebhookHostResolver>();
        _resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("93.184.216.34")]);
    }

    protected void Configure(Configuration.ExternalServices settings)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
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
        services.AddSingleton(_ => Options.Create(new ChronicleOptions { ExternalServices = settings }));
        services.AddSingleton(environment);
        services.AddSingleton(_resolver);
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
        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        var definitions = Substitute.For<IExternalServiceDefinitionsStorage>();
        storage.GetEventStore(_eventStore).Returns(store);
        store.ExternalServices.Returns(definitions);
        definitions.GetAll().Returns([new ExternalServiceDefinition("orders", "OrdersApi", new(ExternalServiceEndpointType.Http, Http: new("https://capture.example", new BearerTokenAuthorization("token"), new Dictionary<string, string> { ["X-Api-Key"] = "key" })))]);
        _reader = new(storage, ActivatorUtilities.CreateInstance<ExternalServiceHttpClientFactory>(_services));
    }

    void Destroy() => _services?.Dispose();

    public class recording_transport : HttpMessageHandler
    {
        public List<(Uri Target, string? Authorization, bool HasApiKey)> Requests { get; } = [];
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public Uri Location { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri!, request.Headers.Authorization?.ToString(), request.Headers.Contains("X-Api-Key")));
            var response = new HttpResponseMessage(StatusCode) { Content = new StringContent("[]") };
            response.Headers.Location = Location;
            return Task.FromResult(response);
        }
    }
}
