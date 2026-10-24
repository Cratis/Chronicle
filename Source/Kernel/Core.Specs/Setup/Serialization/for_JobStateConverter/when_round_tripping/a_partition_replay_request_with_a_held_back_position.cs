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
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_JobStateConverter.when_round_tripping;

public class a_partition_replay_request_with_a_held_back_position : given.a_converter_for_job_states
{
    ReplayObserverPartitionRequest _jsonResult;
    ReplayObserverPartitionRequest _orleansResult;

    void Establish()
    {
        _jobType = new JobType("ReplayObserverPartition");
        _jobTypes.GetRequestClrTypeFor(Arg.Any<JobType>())
            .Returns(Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(ReplayObserverPartitionRequest)));
    }

    void Because()
    {
        var request = new ReplayObserverPartitionRequest(_observerKey, ObserverType.Projection, "partition", 0UL, 100UL, []) { HeldBackFrom = 45UL };
        var state = new JobState { Id = JobId.New(), Type = _jobType, Status = JobStatus.Running, Request = request };
        var stored = JsonNode.Parse(JsonSerializer.Serialize(state, _options))!.AsObject();
        var restored = JsonSerializer.Deserialize<JobState>(stored.ToJsonString(), _options)!;
        _jsonResult = (ReplayObserverPartitionRequest)restored.Request;
        var services = new ServiceCollection();
        services.AddSerializer(builder => builder.AddJsonSerializer(JsonSerializedTypes.Includes, _options));
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<Serializer>();
        _orleansResult = (ReplayObserverPartitionRequest)serializer.Deserialize<JobState>(serializer.SerializeToArray(restored)).Request;
    }

    [Fact] void should_preserve_the_observer_identity() => _jsonResult.ObserverKey.ShouldEqual(_observerKey);
    [Fact] void should_preserve_the_partition() => _jsonResult.Key.Value.ToString().ShouldEqual("partition");
    [Fact] void should_restore_the_json_bound() => _jsonResult.HeldBackFrom.ShouldEqual((EventSequenceNumber)45UL);
    [Fact] void should_preserve_the_bound_through_orleans() => _orleansResult.HeldBackFrom.ShouldEqual((EventSequenceNumber)45UL);
}
