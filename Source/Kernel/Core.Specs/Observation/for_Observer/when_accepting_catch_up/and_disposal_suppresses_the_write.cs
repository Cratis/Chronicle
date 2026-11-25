// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_accepting_catch_up;

public class and_disposal_suppresses_the_write : given.an_observer_accepting_a_catch_up_handover
{
    Exception? _error;

    void Establish() => _observer.Dispose();

    async Task Because() => _error = await Catch.Exception(() => _observer.CaughtUp(JobId.NotSet, 12UL, 12UL));

    [Fact] void should_not_treat_a_skipped_write_as_acceptance() => _error!.Message.ShouldEqual("The catch-up handover could not be durably accepted.");
}
