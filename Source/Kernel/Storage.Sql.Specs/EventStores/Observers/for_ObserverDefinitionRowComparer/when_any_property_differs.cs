// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionRowComparer;

public class when_any_property_differs : Specification
{
    [Fact] void should_compare_id() => Different(row => row.Id = "other");
    [Fact] void should_compare_event_types_content() => Different(row => row.EventTypes = [new("other", 1, "key")]);
    [Fact] void should_compare_event_types_order() => Different(row => row.EventTypes = row.EventTypes.Reverse().ToArray());
    [Fact] void should_compare_event_sequence_id() => Different(row => row.EventSequenceId = "other");
    [Fact] void should_compare_type() => Different(row => row.Type = ObserverType.Unknown);
    [Fact] void should_compare_owner() => Different(row => row.Owner = ObserverOwner.Kernel);
    [Fact] void should_compare_is_replayable() => Different(row => row.IsReplayable = false);
    [Fact] void should_compare_generation_delivery() => Different(row => row.GenerationDelivery = EventGenerationDelivery.Pinned);

    static void Different(Action<ObserverDefinition> change)
    {
        var first = when_rows_are_equal_but_distinct_instances.Create();
        var second = when_rows_are_equal_but_distinct_instances.Create();
        change(second);
        ObserverDefinitionRowComparer.Instance.Equals(first, second).ShouldBeFalse();
    }
}
