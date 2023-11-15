// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRoot.when_applying;

public class an_invalid_event : given.a_stateless_aggregate_root
{
    Exception result;

    void Because() => result = Catch.Exception(() => aggregate_root.Apply(new object()));

    [Fact] void should_throw_missing_event_type_attribute() => result.ShouldBeOfExactType<MissingEventTypeAttribute>();
}
