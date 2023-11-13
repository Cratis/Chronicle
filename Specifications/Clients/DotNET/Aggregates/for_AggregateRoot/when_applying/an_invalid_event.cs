// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRoot.when_applying;

public class an_invalid_event : Specification
{
    AggregateRoot aggregate_root;
    Exception result;

    void Establish() => aggregate_root = new();

    async void Because() => result = await Catch.Exception(() => aggregate_root.Apply(new object()));

    [Fact] void should_throw_missing_event_type_attribute() => result.ShouldBeOfExactType<MissingEventTypeAttribute>();
}
