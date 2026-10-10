// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosedStreamScope.when_checking_coverage;

public class and_values_differ : Specification
{
    bool _covers;

    void Because() => _covers = new ClosedStreamScope(EventSourceId: "source-a").Covers(new(EventSourceId: "source-b", EventStreamId: "month"));

    [Fact] void should_not_cover_another_source() => _covers.ShouldBeFalse();
}
