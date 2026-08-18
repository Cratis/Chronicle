// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Setup.Serialization.for_JobStateConverter.when_round_tripping;

/// <summary>
/// A job grain persists its own derived state type, never the base <see cref="Storage.Jobs.JobState"/>, and the
/// request is declared as the empty marker interface <see cref="IJobRequest"/>. The converter therefore has to own
/// every type deriving from <see cref="Storage.Jobs.JobState"/> - left to the default converter, a derived state
/// writes an empty request and cannot be read back at all.
/// </summary>
public class a_derived_job_state : given.a_converter_for_job_states
{
    JobStateWithLastHandledEvent _result;

    void Because()
    {
        var state = new JobStateWithLastHandledEvent
        {
            Id = JobId.New(),
            Type = _jobType,
            Status = JobStatus.Running,
            Request = _request,
            LastHandledEventSequenceNumber = new EventSequenceNumber(42)
        };

        _result = JsonSerializer.Deserialize<JobStateWithLastHandledEvent>(JsonSerializer.Serialize(state, _options), _options)!;
    }

    [Fact] void should_keep_the_derived_state_type() => _result.ShouldBeOfExactType<JobStateWithLastHandledEvent>();
    [Fact] void should_keep_the_request_type() => _result.Request.ShouldBeOfExactType<CatchUpObserverRequest>();
    [Fact] void should_keep_the_observer_key() => ((CatchUpObserverRequest)_result.Request).ObserverKey.ShouldEqual(_observerKey);
    [Fact] void should_keep_the_observer_type() => ((CatchUpObserverRequest)_result.Request).ObserverType.ShouldEqual(_request.ObserverType);
    [Fact] void should_keep_the_last_handled_event_sequence_number() => _result.LastHandledEventSequenceNumber.ShouldEqual(new EventSequenceNumber(42));
}
