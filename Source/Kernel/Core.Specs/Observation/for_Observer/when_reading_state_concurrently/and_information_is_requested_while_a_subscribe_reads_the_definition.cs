// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_state_concurrently;

public class and_information_is_requested_while_a_subscribe_reads_the_definition : given.a_live_observer_with_overlap_guarded_definition
{
    const int Requests = 16;
    Exception? _error;

    async Task Because()
    {
        _guardedDefinition.HoldNextRead = true;
        var subscribing = _observer.SubscribeToAllEvents<NullObserverSubscriber>(ObserverType.Projection, SiloAddress.Zero);
        await WaitFor(_guardedDefinition.ReadStarted);
        try
        {
            await Task.WhenAll(Enumerable.Range(0, Requests).Select(_ => _observer.GetInformation()));
        }
        catch (Exception ex)
        {
            _error = ex;
        }

        _guardedDefinition.ReleaseRead.TrySetResult();
        await subscribing;
    }

    [Fact] void should_answer_every_request() => _error.ShouldBeNull();
    [Fact] void should_never_read_the_definition_twice_at_once() => _guardedDefinition.MaxReadsInFlight.ShouldEqual(1);
}
