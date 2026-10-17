// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_the_system_event_types_are_not_registered_yet : given.an_ensure_command
{
    bool _systemEventTypesRegistered;
    bool _registeredWhenAnnounced;

    void Establish()
    {
        _eventTypes.DiscoverAndRegister(EventStoreName.System).Returns(_ =>
        {
            _systemEventTypesRegistered = true;
            return Task.CompletedTask;
        });
        _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default)
            .Returns(_ =>
            {
                _registeredWhenAnnounced = _systemEventTypesRegistered;
                return _systemEventTypesRegistered
                    ? AppendResult.Success(CorrelationId.NotSet, EventSequenceNumber.First)
                    : AppendResult.Failed(CorrelationId.NotSet, [new AppendError("EventStoreAdded is missing from event store 'System'")]);
            });
    }

    Task Because() => Ensure();

    [Fact] void should_register_the_system_event_types_before_announcing_the_store() => _registeredWhenAnnounced.ShouldBeTrue();
    [Fact] void should_mark_the_store_initialized() => _initialized.ShouldBeTrue();
}
