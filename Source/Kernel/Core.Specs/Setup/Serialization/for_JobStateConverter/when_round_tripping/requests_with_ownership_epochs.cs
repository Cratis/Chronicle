// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_JobStateConverter.when_round_tripping;

public class requests_with_ownership_epochs : given.a_converter_for_job_states
{
    ReplayObserverPartitionRequest _jsonPartitionResult;
    ReplayObserverPartitionRequest _orleansPartitionResult;
    ReplayObserverRequest _jsonObserverResult;
    ReplayObserverRequest _orleansObserverResult;

    void Establish()
    {
        _jobTypes.GetRequestClrTypeFor(new JobType("ReplayObserverPartitionRequest"))
            .Returns(Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(ReplayObserverPartitionRequest)));
        _jobTypes.GetRequestClrTypeFor(new JobType("ReplayObserverRequest"))
            .Returns(Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(ReplayObserverRequest)));
    }

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSerializer(builder => builder.AddJsonSerializer(JsonSerializedTypes.Includes, _options));
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<Serializer>();

        (_jsonPartitionResult, _orleansPartitionResult) = RoundTrip(serializer, new ReplayObserverPartitionRequest(_observerKey, ObserverType.Projection, "partition", 0UL, 100UL, []) { OwnershipEpoch = 42 });
        (_jsonObserverResult, _orleansObserverResult) = RoundTrip(serializer, new ReplayObserverRequest(_observerKey, ObserverType.Projection, []) { OwnershipEpoch = 43 });
    }

    [Fact] void should_restore_the_partition_request_epoch_from_json() => _jsonPartitionResult.OwnershipEpoch.ShouldEqual(42UL);
    [Fact] void should_preserve_the_partition_request_epoch_through_orleans() => _orleansPartitionResult.OwnershipEpoch.ShouldEqual(42UL);
    [Fact] void should_restore_the_observer_request_epoch_from_json() => _jsonObserverResult.OwnershipEpoch.ShouldEqual(43UL);
    [Fact] void should_preserve_the_observer_request_epoch_through_orleans() => _orleansObserverResult.OwnershipEpoch.ShouldEqual(43UL);

    (TRequest Json, TRequest Orleans) RoundTrip<TRequest>(Serializer serializer, TRequest request)
        where TRequest : class, IJobRequest
    {
        var state = new JobState { Id = JobId.New(), Type = new JobType(request.GetType().Name), Status = JobStatus.Running, Request = request };
        var restored = JsonSerializer.Deserialize<JobState>(JsonSerializer.Serialize(state, _options), _options)!;
        var throughOrleans = serializer.Deserialize<JobState>(serializer.SerializeToArray(restored));
        return ((TRequest)restored.Request, (TRequest)throughOrleans.Request);
    }
}
