// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Setup.Serialization.for_JobStateConverter.when_round_tripping;

public class a_request_with_a_reader_scope : given.a_converter_for_job_states
{
    static readonly EventType _eventType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    JobState _result;

    void Because()
    {
        var state = new JobState
        {
            Id = JobId.New(),
            Type = _jobType,
            Status = JobStatus.Running,
            Request = _request with { ReaderScope = ObserverReaderScope.Explicit([_eventType]) }
        };

        _result = JsonSerializer.Deserialize<JobState>(JsonSerializer.Serialize(state, _options), _options)!;
    }

    [Fact] void should_keep_the_scope_explicit() => ((CatchUpObserverRequest)_result.Request).ReaderScope!.IsWildcard.ShouldBeFalse();
    [Fact] void should_keep_the_scope_event_types() => ((CatchUpObserverRequest)_result.Request).ReaderScope!.EventTypes.ShouldContainOnly([_eventType]);
}
