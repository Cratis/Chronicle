// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Setup.Serialization.for_JobStateConverter.when_round_tripping;

public class requests_persisted_before_ownership_epochs : given.a_converter_for_job_states
{
    CatchUpObserverPartitionRequest _partitionResult;
    CatchUpObserverRequest _observerResult;

    void Establish()
    {
        _jobTypes.GetRequestClrTypeFor(new JobType("CatchUpObserverPartitionRequest"))
            .Returns(Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(CatchUpObserverPartitionRequest)));
        _jobTypes.GetRequestClrTypeFor(new JobType("CatchUpObserverRequest"))
            .Returns(Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(CatchUpObserverRequest)));
    }

    void Because()
    {
        _partitionResult = (CatchUpObserverPartitionRequest)RestoredWithoutEpoch(new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, "partition", EventSequenceNumber.First, []) { OwnershipEpoch = 5 });
        _observerResult = (CatchUpObserverRequest)RestoredWithoutEpoch(new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, []) { OwnershipEpoch = 5 });
    }

    [Fact] void should_default_the_partition_request_epoch_to_none() => _partitionResult.OwnershipEpoch.ShouldBeNull();
    [Fact] void should_default_the_observer_request_epoch_to_none() => _observerResult.OwnershipEpoch.ShouldBeNull();

    IJobRequest RestoredWithoutEpoch(IJobRequest request)
    {
        var state = new JobState { Id = JobId.New(), Type = new JobType(request.GetType().Name), Status = JobStatus.Running, Request = request };
        var stored = JsonNode.Parse(JsonSerializer.Serialize(state, _options))!.AsObject();
        stored["request"]!.AsObject().Remove("ownershipEpoch");
        return JsonSerializer.Deserialize<JobState>(stored.ToJsonString(), _options)!.Request;
    }
}
