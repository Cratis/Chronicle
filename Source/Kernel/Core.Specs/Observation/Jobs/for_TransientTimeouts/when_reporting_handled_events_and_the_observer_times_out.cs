// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_reporting_handled_events_and_the_observer_times_out : Specification
{
    IObserver _observer;
    Exception _exception;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _observer.ReportHandledEvents(Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>()).Returns(_ => Task.FromException(new TimeoutException("busy")));
    }

    async Task Because() => _exception = await Catch.Exception(() => TransientTimeouts.ReportHandledEventsWithoutFailing(
        _observer,
        new Key("p", ArrayIndexers.NoIndexers),
        new Dictionary<EventTypeId, EventCount>(),
        Substitute.For<ILogger>()));

    [Fact] void should_not_fail_the_step() => _exception.ShouldBeNull();
}
