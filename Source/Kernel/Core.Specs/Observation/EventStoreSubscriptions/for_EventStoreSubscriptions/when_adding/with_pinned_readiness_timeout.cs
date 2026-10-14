// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_pinned_readiness_timeout : given.a_pinned_registration
{
    void Establish() => _manager.WaitUntilSubscribed(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<TimeSpan>()).Returns(Task.FromException(new TimeoutException("Pinned observer unavailable")));
    async Task Because() => _error = await Catch.Exception(() => _service.Add(_request));
    [Fact] void should_not_silently_report_pinned_registration_as_ready() => _error.ShouldBeOfExactType<TimeoutException>();
}
