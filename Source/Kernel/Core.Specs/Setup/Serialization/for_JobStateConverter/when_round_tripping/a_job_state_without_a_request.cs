// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Setup.Serialization.for_JobStateConverter.when_round_tripping;

/// <summary>
/// A state persisted before its request was assigned carries no request, so there is nothing to resolve a request
/// CLR type for. The MongoDB serializer leaves the request unset in that case rather than failing, and the JSON
/// converter has to behave the same way.
/// </summary>
public class a_job_state_without_a_request : given.a_converter_for_job_states
{
    JobState _result;

    void Because()
    {
        var state = new JobState
        {
            Id = JobId.New(),
            Type = _jobType,
            Status = JobStatus.Running
        };

        _result = JsonSerializer.Deserialize<JobState>(JsonSerializer.Serialize(state, _options), _options)!;
    }

    [Fact] void should_keep_the_job_type() => _result.Type.ShouldEqual(_jobType);
    [Fact] void should_keep_the_status() => _result.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_leave_the_request_unset() => _result.Request.ShouldBeNull();
}
