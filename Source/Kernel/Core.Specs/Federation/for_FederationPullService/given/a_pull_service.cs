// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationPullService.given;

public class a_pull_service : Federation.given.certificates
{
    protected const string Store = "producer";
    protected const string Namespace = "Default";

    protected IFederationCaller _caller;
    protected IFederationRegistry _registry;
    protected IFederationStatus _status;
    protected IEventSequenceStorage _outbox;
    protected IEventTypesStorage _eventTypes;
    protected IStorage _storage;
    protected IExpandoObjectConverter _converter;
    protected FederationMetrics _metrics;
    protected Federation.given.ManualTimeProvider _time;
    protected FederationRateLimiter _rateLimiter;
    protected FederationPullService _service;
    protected ChronicleOptions _options;
    protected List<FederationGrantState> _grants;
    protected List<AppendedEvent> _events;
    protected X509Certificate2? _presented;

    void Establish()
    {
        _grants = [];
        _events = [];
        _presented = _certificate;

        _caller = Substitute.For<IFederationCaller>();
        _caller.ClientCertificate.Returns(_ => _presented);

        _registry = Substitute.For<IFederationRegistry>();
        _registry.FindActiveGrants(Arg.Any<CertificatePin>()).Returns(callInfo =>
            Task.FromResult<IReadOnlyList<FederationGrantState>>([.. _grants.Where(grant => grant.ClientPin == callInfo.Arg<CertificatePin>().ToString())]));

        _status = Substitute.For<IFederationStatus>();

        _outbox = Substitute.For<IEventSequenceStorage>();
        _outbox.GetTailSequenceNumber().Returns(_ => Task.FromResult(_events.Count == 0 ? EventSequenceNumber.Unavailable : new EventSequenceNumber(_events.Max(e => e.Context.SequenceNumber.Value))));
        _outbox.GetRange(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId?>(),
            Arg.Any<IEnumerable<EventType>?>(),
            Arg.Any<IEnumerable<Concepts.Events.Tag>?>(),
            Arg.Any<CancellationToken>()).Returns(callInfo =>
            {
                var start = callInfo.ArgAt<EventSequenceNumber>(0);
                var end = callInfo.ArgAt<EventSequenceNumber>(1);
                var cursor = Substitute.For<IEventCursor>();
                var delivered = false;
                cursor.MoveNext().Returns(_ =>
                {
                    var more = !delivered;
                    delivered = true;
                    return Task.FromResult(more);
                });
                cursor.Current.Returns(_ => _events
                    .Where(e => e.Context.SequenceNumber.Value >= start.Value && e.Context.SequenceNumber.Value <= end.Value)
                    .OrderBy(e => e.Context.SequenceNumber.Value)
                    .ToArray());
                return Task.FromResult(cursor);
            });

        _eventTypes = Substitute.For<IEventTypesStorage>();

        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        namespaceStorage.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        eventStoreStorage.EventTypes.Returns(_eventTypes);
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);

        _converter = Substitute.For<IExpandoObjectConverter>();
        _converter.ToJsonObject(Arg.Any<System.Dynamic.ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(_ => new JsonObject { ["value"] = 1 });

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        _metrics = new(meterFactory);

        _time = new();
        _rateLimiter = new(_time);
        Configure(new FederationLimits());
    }

    protected static FederationGrantState Grant(
        string id,
        CertificatePin pin,
        string eventStore = Store,
        string[]? namespaces = null,
        bool allPublic = true,
        string[]? eventTypes = null,
        bool revoked = false) =>
        new(id, pin.ToString(), eventStore, namespaces ?? [Namespace], allPublic, eventTypes ?? [], revoked, nameof(FederationOrigin.Runtime));

    protected void Configure(FederationLimits limits, bool enabled = true)
    {
        _options = new() { Federation = new() { Enabled = enabled, Port = 35100, Limits = limits } };
        _service = new(
            _caller,
            _registry,
            _status,
            _storage,
            _converter,
            _rateLimiter,
            _metrics,
            Options.Create(_options),
            Substitute.For<ILogger<FederationPullService>>());
    }

    protected FederationGrantState GrantForCaller(string id = "link-a", bool allPublic = true, params string[] eventTypes) =>
        Grant(id, CertificatePin.From(_certificate), allPublic: allPublic, eventTypes: eventTypes);

    protected FederationGrantState GrantForOtherPeer(string id = "link-b") =>
        Grant(id, CertificatePin.From(_otherCertificate));

    protected void RegisterEventType(string id, EventTypeVisibility visibility, bool protectedData = false)
    {
        var schema = JsonSchema.FromJson(protectedData
            ? """{ "type": "object", "properties": { "name": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] } } }"""
            : """{ "type": "object", "properties": { "name": { "type": "string" } } }""");
        var eventType = new EventType(id, 1);
        _eventTypes.HasFor(eventType.Id, Arg.Any<EventTypeGeneration?>()).Returns(true);
        _eventTypes.GetFor(eventType.Id, Arg.Any<EventTypeGeneration?>()).Returns(new EventTypeSchema(eventType, EventTypeOwner.Client, EventTypeSource.Code, schema, visibility));
    }

    protected void Append(string eventTypeId, ulong sequenceNumber) =>
        _events.Add(AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType(eventTypeId, 1), sequenceNumber));

    protected Task<FederatedEventBatch> Pull(ulong from = 0, int limit = 100, string eventStore = Store, string @namespace = Namespace, params string[] eventTypes) =>
        _service.Pull(eventStore, @namespace, from, limit, eventTypes);
}
