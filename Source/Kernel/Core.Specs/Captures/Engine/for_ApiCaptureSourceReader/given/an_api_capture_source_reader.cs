// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.ExternalServices;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ExternalServices;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.given;

public class an_api_capture_source_reader : Specification
{
    protected EventStoreName _eventStore = "orders";
    protected ApiCaptureSourceReader _reader;
    protected HttpRequestMessage _request;
    protected IWebhookTargetPolicy _policy;
    protected SourceDefinition _source;
    HttpClient _client;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        var services = Substitute.For<IExternalServiceDefinitionsStorage>();
        storage.GetEventStore(_eventStore).Returns(store);
        store.ExternalServices.Returns(services);
        services.GetAll().Returns([new ExternalServiceDefinition("orders", "OrdersApi", new(ExternalServiceEndpointType.Http))]);
        _policy = Substitute.For<IWebhookTargetPolicy>();
        _policy.CheckAuthority(Arg.Any<Uri>(), Arg.Any<EventStoreName?>()).Returns(WebhookTargetValidation.Allowed);
        var handler = new WebhookValidationHandler(_policy, Substitute.For<ILogger<WebhookValidationHandler>>())
        {
            InnerHandler = new capture_handler(request => _request = request)
        };
        _client = new(handler) { BaseAddress = new Uri("https://orders.example/") };
        var factory = Substitute.For<IExternalServiceHttpClientFactory>();
        factory.Create(Arg.Any<ExternalServiceDefinition>()).Returns(_client);
        _reader = new(storage, factory);
        _source = new(SourceType.Api, Api: "OrdersApi", Route: "orders");
    }

    void Destroy() => _client.Dispose();

    class capture_handler(Action<HttpRequestMessage> capture) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            capture(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });
        }
    }
}
