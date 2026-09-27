// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_NoOpEventLog;

public class when_appending_a_routed_named_tag_batch : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => new NoOpEventLog().AppendManyWithNamedTags([new EventForEventSourceId(EventSourceId.New(), "event")], []));

    [Fact] void should_report_that_the_event_log_is_unavailable_even_for_empty_tags() => _error.ShouldBeOfExactType<EventLogNotAvailableInKernelPipeline>();
}
