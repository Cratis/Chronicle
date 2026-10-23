// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_reporting_handled_events_and_the_observer_never_takes_them : Specification
{
    IObserver _observer;
    Exception _exception;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _observer
            .ReportHandledEvents(Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(), Arg.Any<JobStepId>(), Arg.Any<EventSequenceNumber>())
            .Returns(_ => Task.FromException(new InvalidOperationException("storage is down")));
    }

    async Task Because() => _exception = await Catch.Exception(() => TransientTimeouts.ReportHandledEventsWithoutFailing(
        _observer,
        new(new Key("p", ArrayIndexers.NoIndexers), new Dictionary<EventTypeId, EventCount>(), JobStepId.New(), 42UL),
        TimeSpan.Zero,
        Substitute.For<ILogger>(),
        CancellationToken.None));

    [Fact] void should_not_fail_the_step() => _exception.ShouldBeNull();
    [Fact] void should_try_as_many_times_as_a_step_is_attempted() => _observer.ReceivedCalls().Count().ShouldEqual(TransientTimeouts.MaximumAttempts);
}
