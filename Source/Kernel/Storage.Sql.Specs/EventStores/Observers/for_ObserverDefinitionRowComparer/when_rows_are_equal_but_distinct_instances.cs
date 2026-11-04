// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionRowComparer;

public class when_rows_are_equal_but_distinct_instances : Specification
{
    [Fact] void should_compare_all_values() => ObserverDefinitionRowComparer.Instance.Equals(Create(), Create()).ShouldBeTrue();
    [Fact] void should_have_equal_hash_codes() => ObserverDefinitionRowComparer.Instance.GetHashCode(Create()).ShouldEqual(ObserverDefinitionRowComparer.Instance.GetHashCode(Create()));
    [Fact] void should_compare_nulls() => ObserverDefinitionRowComparer.Instance.Equals(null, null).ShouldBeTrue();
    [Fact] void should_reject_one_null() => ObserverDefinitionRowComparer.Instance.Equals(Create(), null).ShouldBeFalse();

    internal static ObserverDefinition Create() => new()
    {
        Id = "observer",
        EventTypes = [new("first", 1, "key"), new("second", 2, null)],
        EventSequenceId = "sequence",
        Type = Concepts.Observation.ObserverType.Reactor,
        Owner = Concepts.Observation.ObserverOwner.Client,
        IsReplayable = true,
        GenerationDelivery = Concepts.Observation.EventGenerationDelivery.Compatibility
    };
}
