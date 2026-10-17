// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Setup.Serialization.for_JobStateConverter.when_round_tripping;

/// <summary>
/// A job persisted by a release before requests carried a reader scope and a revision has neither in its stored
/// request. It has to load unchanged, with both absent, so that the readers fall back to what the request's event
/// type list has always meant.
/// </summary>
public class a_request_persisted_before_reader_scopes : given.a_converter_for_job_states
{
    JobState _result;
    JsonObject _storedRequest;

    void Because()
    {
        var state = new JobState
        {
            Id = JobId.New(),
            Type = _jobType,
            Status = JobStatus.Running,
            Request = _request with { ReaderScope = Concepts.Observation.ObserverReaderScope.Wildcard, Revision = 1 }
        };

        var stored = JsonNode.Parse(JsonSerializer.Serialize(state, _options))!.AsObject();
        _storedRequest = stored.Single(_ => _.Key.Equals("request", StringComparison.OrdinalIgnoreCase)).Value!.AsObject();
        foreach (var property in _storedRequest.Where(_ => _.Key.Equals("readerScope", StringComparison.OrdinalIgnoreCase) || _.Key.Equals("revision", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            _storedRequest.Remove(property.Key);
        }

        _result = JsonSerializer.Deserialize<JobState>(stored.ToJsonString(), _options)!;
    }

    [Fact] void should_have_stored_a_request_without_the_new_properties() => _storedRequest.Select(_ => _.Key).ShouldNotContain("readerScope");
    [Fact] void should_load_the_request() => _result.Request.ShouldBeOfExactType<CatchUpObserverRequest>();
    [Fact] void should_keep_the_observer_key() => ((CatchUpObserverRequest)_result.Request).ObserverKey.ShouldEqual(_observerKey);
    [Fact] void should_default_the_reader_scope_to_none() => ((CatchUpObserverRequest)_result.Request).ReaderScope.ShouldBeNull();
    [Fact] void should_default_the_revision_to_none() => ((CatchUpObserverRequest)_result.Request).Revision.ShouldBeNull();
}
