// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_catchup;

public class and_the_window_is_replaced_before_the_first_checkpoint : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish() => _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
        Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [] }),
        Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [Guid.NewGuid()] }));

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_advance_the_durable_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_fail_the_step_instead_of_skipping_the_lost_writes() => _result.Match(result => result.TryGetError(out _), _ => true).ShouldBeTrue();
}
