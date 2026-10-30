// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_the_flush_fails : given.a_buffered_projection_job_step
{
    int _flushes;

    void Establish() => _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(_ =>
        ++_flushes == 1 ? Task.FromResult(Windows(_window)) : Task.FromException<IReadOnlyDictionary<SiloAddress, Guid[]>>(new InvalidOperationException("flush failed")));

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_fail_the_step() => _result.Match(result => result.TryGetError(out _), _ => true).ShouldBeTrue();
    [Fact] void should_keep_the_prior_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.IsActualValue.ShouldBeFalse();
}
