// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.given;

public class a_webhook_registrar : Specification
{
    protected WebhookRegistrar _registrar;
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IWebhookDefinitionComparer _webhookDefinitionComparer;
    protected IEncryption _encryption;
    protected IOAuthClient _oauthClient;
    protected IWebhookMediator _webhookMediator;
    protected IEventSerializer _eventSerializer;
    protected List<object> _serialized;
    protected IEventSequenceStorage _log;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([]);
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>()).Returns(namespaces);
        _storage = Substitute.For<IStorage>();
        _webhookDefinitionComparer = Substitute.For<IWebhookDefinitionComparer>();
        _encryption = Substitute.For<IEncryption>();
        _oauthClient = Substitute.For<IOAuthClient>();
        _webhookMediator = Substitute.For<IWebhookMediator>();
        _serialized = [];
        _eventSerializer = Substitute.For<IEventSerializer>();
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(call =>
        {
            _serialized.Add(call.Arg<object>());
            return new JsonObject();
        });
        _log = Substitute.For<IEventSequenceStorage>();
        ALogWithLastLifecycleEvent(Option<AppendedEvent>.None());
        _storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(Arg.Any<EventStoreNamespaceName>()).GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_log);
        _registrar = CreateRegistrar();
    }

    /// <summary>
    /// Stubs the system event sequence log: each read of the lifecycle of a webhook returns the next result, and the last one repeats.
    /// </summary>
    /// <param name="reads">The results of the reads, in order.</param>
    protected void ALogWithLastLifecycleEvent(params Option<AppendedEvent>[] reads) =>
        _log.TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(Task.FromResult(reads[0]), reads.Skip(1).Select(Task.FromResult).ToArray());

    protected static Option<AppendedEvent> ALifecycleEvent<TEvent>(EventSequenceNumber sequenceNumber)
        where TEvent : class =>
        new Option<AppendedEvent>(new AppendedEvent(
            EventContext.Empty with { EventType = typeof(TEvent).GetEventType(), SequenceNumber = sequenceNumber },
            new System.Dynamic.ExpandoObject()));

    protected WebhookRegistrar CreateRegistrar(IWebhookTargetPolicy? policy = null) =>
        new(_grainFactory, _webhookDefinitionComparer, _encryption, _oauthClient, _webhookMediator, Options.Create(new ChronicleOptions()), _storage, _eventSerializer, NullLogger<WebhookRegistrar>.Instance, policy);

    protected IEventSequence AnEventSequenceThatAppendsSuccessfully()
    {
        var eventSequence = Substitute.For<IEventSequence>();
        eventSequence.Append(
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventType>(),
            Arg.Any<JsonObject>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Subject?>()).Returns(_ => AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        eventSequence.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>()).Returns(_ => AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        return eventSequence;
    }
}
