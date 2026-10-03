// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.EventSources.for_EventSourceResolution;

public class when_resolving_with_a_contradicting_event_source_type : given_registered_event_sources
{
    AppendResult _result;

    async Task Because()
    {
        var resolved = await EventSourceResolution.Resolve(_eventSources, new EventSourceName("ShoppingCart"), "Order", EventStreamType.All, CorrelationId.New());
        _result = resolved.AsT1;
    }

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_mismatch() => _result.Errors.Single().ToString().ShouldContain(EventSourceResolution.EventSourceTypeDoesNotMatchEventSource);
}
