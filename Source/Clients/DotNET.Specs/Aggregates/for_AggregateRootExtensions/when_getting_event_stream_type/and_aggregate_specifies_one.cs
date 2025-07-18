// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootExtensions.when_getting_event_stream_type;

public class and_aggregate_specifies_one : Specification
{
    EventStreamType _result;
    AggregateRootWithEventStreamType _aggregate;

    void Establish() => _aggregate = new();

    void Because() => _result = _aggregate.GetEventStreamType();

    [Fact] public void should_return_the_specified_event_stream_type() => _result.Value.ShouldEqual(AggregateRootWithEventStreamType.EventStreamType);
}
