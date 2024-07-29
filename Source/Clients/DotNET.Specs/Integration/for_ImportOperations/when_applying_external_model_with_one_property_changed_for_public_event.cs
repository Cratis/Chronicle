// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_ImportOperations;

public class when_applying_external_model_with_one_property_changed_for_public_event : given.one_property_changed_for<SomePublicEvent>
{
    SomePublicEvent event_appended_to_event_log;

    void Establish()
    {
        event_log
            .Setup(_ => _.AppendMany(IsAny<EventSourceId>(), IsAny<IEnumerable<object>>()))
            .Callback((EventSourceId _, IEnumerable<object> events) =>
            {
                var @event = events.First();
                event_appended_to_event_log = (@event as SomePublicEvent)!;
            });
    }

    async Task Because() => await operations.Apply(incoming);

    [Fact] void should_add_causation() => causation_manager.Verify(_ => _.Add(ImportOperations<string, string>.CausationType, IsAny<IDictionary<string, string>>()), Once);
    [Fact] void should_have_adapter_id_in_causation() => causation_properties[ImportOperations<string, string>.CausationAdapterIdProperty].ShouldEqual(adapter_id.ToString());
    [Fact] void should_have_adapter_type_in_causation() => causation_properties[ImportOperations<string, string>.CausationAdapterTypeProperty].ShouldEqual(adapter.Object.GetType().AssemblyQualifiedName);
    [Fact] void should_have_key_in_causation() => causation_properties[ImportOperations<string, string>.CausationKeyProperty].ShouldEqual(key);
    [Fact] void should_have_one_event_in_event_log() => event_appended_to_event_log.ShouldNotBeNull();
}
