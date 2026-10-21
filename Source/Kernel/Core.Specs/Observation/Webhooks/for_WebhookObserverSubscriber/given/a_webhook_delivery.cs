// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.given;

public class a_webhook_delivery : Events.for_EventGenerationRelease.given.a_release_boundary
{
    protected TestKitSilo _silo;
    protected WebhookObserverSubscriber _subscriber;
    protected IWebhook _webhook;
    protected IWebhookDefinitionsStorage _webhookDefinitions;
    protected WebhookDefinition _webhookDefinition;
    protected AppendedEvent[] _events;
    protected ObserverSubscriberResult _deliveryResult;
    protected PayloadReceiver _receiver;
    protected Key _partition;
    HttpClient _httpClient;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _events = [_event with
        {
            Context = _event.Context with
            {
                EventStore = "store",
                Namespace = "namespace",
                EventSourceId = "person",
                SequenceNumber = 42,
                Occurred = DateTimeOffset.UnixEpoch
            }
        }];
        var original = _events[0];
        _events =
        [
            original,
            original with
            {
                Context = original.Context with { SequenceNumber = 43 },
                OriginalContent = "{\"name\":\"original-ciphertext\"}",
                RevisedGeneration = 1,
                Revisions = [new(1, CorrelationId.NotSet, [], Identity.NotSet, DateTimeOffset.UnixEpoch.AddDays(1), "{\"name\":\"ciphertext\"}")]
            },
            original with
            {
                Context = original.Context with { SequenceNumber = 44, AppendedGeneration = null },
                GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"ciphertext\"}" },
                GenerationalHashes = new Dictionary<int, EventHash> { [1] = "first-hash" }
            }
        ];
        _partition = new("person", ArrayIndexers.NoIndexers);
        _webhookDefinition = new("webhook", WebhookOwner.Client, "log", [_pin], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
        {
            GenerationDelivery = EventGenerationDelivery.Pinned
        };
        var stateStorage = Substitute.For<IStorage<WebhookDefinition>>();
        stateStorage.State = _webhookDefinition;
        _silo.Options.StorageFactory = _ => stateStorage;
        _webhookDefinitions = Substitute.For<IWebhookDefinitionsStorage>();
        _storage.GetEventStore("store").Webhooks.Returns(_webhookDefinitions);
        _webhookDefinitions.Get("webhook").Returns(_ => _webhookDefinition);
        _webhook = Substitute.For<IWebhook>();
        _webhook.GetDefinition().Returns(_ => _webhookDefinition);
        _silo.AddProbe(_ => _webhook);
        _eventTypes.GetFor(_pin.Id, _pin.Generation).Returns(_schemas[_pin]);
        _silo.AddService(_storage);
        _silo.AddService<IEventGenerationRelease>(_release);
        _silo.AddService(Substitute.For<IOAuthClient>());
        _silo.AddService(Substitute.For<IEncryption>());
        _receiver = new();
        _httpClient = new(_receiver) { BaseAddress = new("https://example.com/webhook") };
        var factory = Substitute.For<IWebhookHttpClientFactory>();
        factory.Create(Arg.Any<WebhookTarget>(), Arg.Any<string?>()).Returns(_httpClient);
        var services = new ServiceCollection();
        var builder = Substitute.For<ISiloBuilder>();
        builder.Services.Returns(services);
        builder.ConfigureSerialization();
        await using var provider = services.BuildServiceProvider();
        _silo.AddService<IWebhookMediator>(new WebhookMediator(factory, provider.GetRequiredService<JsonSerializerOptions>()));
        _subscriber = await _silo.CreateGrainAsync<WebhookObserverSubscriber>(new ObserverSubscriberKey("webhook", "store", "namespace", "log", "person", string.Empty).ToString());
    }

    protected static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Observation", "Webhooks", "for_WebhookObserverSubscriber", "Fixtures", $"{name}.json")).Trim();
    void Destroy() => _httpClient.Dispose();

    public sealed class PayloadReceiver : HttpMessageHandler
    {
        public List<string> Payloads { get; } = [];
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public TaskCompletionSource RequestReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task ResponseGate { get; set; } = Task.CompletedTask;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Payloads.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            RequestReceived.TrySetResult();
            await ResponseGate.WaitAsync(cancellationToken);
            return new(StatusCode);
        }
    }
}
