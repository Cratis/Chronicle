// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_NoOpEventLog;

public class when_appending_a_named_tag_batch : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => new NoOpEventLog().AppendManyWithNamedTags(EventSourceId.New(), ["event"], [new("name", "value")]));

    [Fact] void should_report_that_the_event_log_is_unavailable() => _error.ShouldBeOfExactType<EventLogNotAvailableInKernelPipeline>();
}
