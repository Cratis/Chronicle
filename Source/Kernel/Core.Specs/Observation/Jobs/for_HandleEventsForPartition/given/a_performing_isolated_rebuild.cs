// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.given;

public class a_performing_isolated_rebuild : a_performing_job_step
{
    protected IProjectionPartitionRebuilder _rebuilder;
    protected IEventSequenceStorage _eventSequence;
    protected Catch<JobStepResult> _result;

    void Establish()
    {
        _rebuilder = Substitute.For<IProjectionPartitionRebuilder>();
        _rebuilder
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => ObserverSubscriberResult.Ok(call.Arg<IEnumerable<AppendedEvent>>().Last().Context.SequenceNumber));
        _jobStep.SetupIsolatedRebuildForTesting(_rebuilder);
        _performState.IsolatedRebuild = true;
        _performState.EndEventSequenceNumber = 100UL;
        _eventSequence = _storage.GetEventStore("event-store").GetNamespace("event-store-namespace").GetEventSequence(EventSequenceId.Log);
    }

    protected JobStepResult StepResult
    {
        get
        {
            _result.TryGetResult(out var stepResult).ShouldBeTrue();
            return stepResult;
        }
    }

    protected HandleEventsForPartitionResult? HandledResult()
    {
        StepResult.TryGetFullResult<HandleEventsForPartitionResult>(out var result, out _, new JsonSerializerOptions());
        return result;
    }
}
