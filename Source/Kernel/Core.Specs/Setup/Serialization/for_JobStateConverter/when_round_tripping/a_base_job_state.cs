// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Setup.Serialization.for_JobStateConverter.when_round_tripping;

public class a_base_job_state : given.a_converter_for_job_states
{
    JobState _result;

    void Because()
    {
        var state = new JobState
        {
            Id = JobId.New(),
            Type = _jobType,
            Status = JobStatus.Running,
            Request = _request
        };

        _result = JsonSerializer.Deserialize<JobState>(JsonSerializer.Serialize(state, _options), _options)!;
    }

    [Fact] void should_keep_the_state_type() => _result.ShouldBeOfExactType<JobState>();
    [Fact] void should_keep_the_request_type() => _result.Request.ShouldBeOfExactType<CatchUpObserverRequest>();
    [Fact] void should_keep_the_observer_key() => ((CatchUpObserverRequest)_result.Request).ObserverKey.ShouldEqual(_observerKey);
}
