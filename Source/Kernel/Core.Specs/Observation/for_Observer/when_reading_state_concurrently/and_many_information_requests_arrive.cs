// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_state_concurrently;

public class and_many_information_requests_arrive : given.a_live_observer_with_overlap_guarded_definition
{
    const int Requests = 16;
    Exception? _error;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        try
        {
            await Task.WhenAll(Enumerable.Range(0, Requests).Select(_ => _observer.GetInformation()));
        }
        catch (Exception ex)
        {
            _error = ex;
        }
    }

    [Fact] void should_answer_every_request() => _error.ShouldBeNull();
    [Fact] void should_never_read_the_definition_twice_at_once() => (_guardedDefinition.MaxReadsInFlight <= 1).ShouldBeTrue();
}
