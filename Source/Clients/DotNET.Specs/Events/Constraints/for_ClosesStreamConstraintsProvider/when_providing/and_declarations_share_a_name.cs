// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintsProvider.when_providing;

public class and_declarations_share_a_name : given.a_closing_provider
{
    ClosesStreamConstraintDefinition _result;

    void Establish()
    {
        Register<Closed>();
        Register<Cancelled>();
        Register<Reopened>();
        _artifacts.ClosesStreamEventTypes.Returns([typeof(Closed), typeof(Cancelled)]);
    }

    void Because() => _result = (ClosesStreamConstraintDefinition)_provider.Provide().Single();

    [Fact] void should_merge_closing_types() => _result.EventTypeIds.Select(type => type.Value).ShouldContainOnly(nameof(Closed), nameof(Cancelled));
    [Fact] void should_keep_the_reopening_type() => _result.ReopenedBy.Single().Value.ShouldEqual(nameof(Reopened));
    [Fact] void should_include_the_property_sourced_dimension() => _result.Dimensions.ShouldEqual(ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId);
    [Fact] void should_keep_the_serialized_property() => _result.EventStreamIdFrom.ShouldEqual("Period");

    [ClosesStream(Name = "shared", Dimensions = ClosedStreamDimensions.EventSourceId, EventStreamIdFrom = "Period", ReopenedBy = [typeof(Reopened)])]
    record Closed(string Period);

    [ClosesStream(Name = "shared", Dimensions = ClosedStreamDimensions.EventSourceId, EventStreamIdFrom = "Period")]
    record Cancelled(string Period);

    record Reopened(string Period);
}
