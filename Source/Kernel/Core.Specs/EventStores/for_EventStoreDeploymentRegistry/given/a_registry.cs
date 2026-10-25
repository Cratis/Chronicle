// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventStores.for_EventStoreDeploymentRegistry.given;

public class a_registry : Specification
{
    protected static readonly EventStoreName _name = "the-store";
    protected EventStoreDeploymentRegistry _registry;
    protected IEventSequence _systemSequence;
    protected IEventSequenceStorage _sequenceStorage;
    protected IEventTypes _eventTypes;
    protected IEventSerializer _serializer;
    protected AppendedEvent _appended;

    protected virtual DeploymentState Stored => DeploymentState.Empty;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        var storage = Substitute.For<IStorage>();
        _sequenceStorage = Substitute.For<IEventSequenceStorage>();
        storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.System).Returns(_sequenceStorage);
        _serializer = Substitute.For<IEventSerializer>();
        _appended = AppendedEvent.Empty();
        _sequenceStorage.TryGetLastInstanceOfAny(_name.Value, Arg.Any<IEnumerable<EventTypeId>>()).Returns(Task.FromResult(Option<AppendedEvent>.None()));
        if (Stored != DeploymentState.Empty)
        {
            _sequenceStorage.TryGetLastInstanceOfAny(_name.Value, Arg.Any<IEnumerable<EventTypeId>>()).Returns(_appended);
            _serializer.Deserialize(_appended).Returns(new EventStoreDeploymentRegistered(_name, Stored.Generation, Stored.Fingerprint));
        }
        _eventTypes = Substitute.For<IEventTypes>();
        _systemSequence = Substitute.For<IEventSequence>();
        _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default)
            .Returns(AppendResult.Success(CorrelationId.NotSet, EventSequenceNumber.First));
        silo.AddService(storage);
        silo.AddService(_eventTypes);
        silo.AddService(_serializer);
        silo.AddProbe<IEventSequence>(_ => _systemSequence);
        _registry = await silo.CreateGrainAsync<EventStoreDeploymentRegistry>(_name.Value);
    }
}
