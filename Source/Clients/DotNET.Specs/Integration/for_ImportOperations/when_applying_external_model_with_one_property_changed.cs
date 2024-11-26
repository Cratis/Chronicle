// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ImportOperations;

public class when_applying_external_model_with_one_property_changed : given.one_property_changed_for<SomeEvent>
{
    SomeEvent _eventAppendedToEventLog;

    void Establish()
    {
        _eventLog
            .When(_ => _.AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>()))
            .Do(callInfo =>
            {
                var events = callInfo.Arg<IEnumerable<object>>();
                var @event = events.First();
                _eventAppendedToEventLog = (@event as SomeEvent)!;
            });
    }

    async Task Because() => await operations.Apply(incoming);

    [Fact] void should_add_causation() => causation_manager.Received(1).Add(ImportOperations<string, string>.CausationType, Arg.Any<IDictionary<string, string>>());
    [Fact] void should_have_adapter_id_in_causation() => _causationProperties[ImportOperations<string, string>.CausationAdapterIdProperty].ShouldEqual(_adapterId.ToString());
    [Fact] void should_have_adapter_type_in_causation() => _causationProperties[ImportOperations<string, string>.CausationAdapterTypeProperty].ShouldEqual(_adapter.GetType().AssemblyQualifiedName);
    [Fact] void should_have_key_in_causation() => _causationProperties[ImportOperations<string, string>.CausationKeyProperty].ShouldEqual(key);
    [Fact] void should_have_one_event_in_event_log() => _eventAppendedToEventLog.ShouldNotBeNull();
}
