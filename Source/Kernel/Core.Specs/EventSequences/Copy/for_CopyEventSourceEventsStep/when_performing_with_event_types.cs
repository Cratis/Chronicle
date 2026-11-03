// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.Copy.for_CopyEventSourceEventsStep;

public class when_performing_with_event_types : given.the_job_step
{
    protected override IEnumerable<EventTypeId> RequestedEventTypes => [new("copied-event")];

    async Task Because() => await Perform();

    [Fact] void should_succeed() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_narrow_by_the_requested_event_types() => _requestedEventTypes!.Select(_ => _.Id).ShouldContainOnly(new EventTypeId("copied-event"));
}
