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

public class a_partition_request_persisted_before_reader_scopes : given.a_converter_for_job_states
{
    CatchUpObserverPartitionRequest _requestToStore;
    CatchUpObserverPartitionRequest _result;
    JsonObject _storedRequest;

    void Establish()
    {
        _requestToStore = new(_observerKey, ObserverType.Reactor, "partition", EventSequenceNumber.First, []);
        _jobTypes.GetRequestClrTypeFor(Arg.Any<JobType>())
            .Returns(Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(CatchUpObserverPartitionRequest)));
    }

    void Because()
    {
        var state = new JobState { Id = JobId.New(), Type = _jobType, Status = JobStatus.Running, Request = _requestToStore };
        var stored = JsonNode.Parse(JsonSerializer.Serialize(state, _options))!.AsObject();
        _storedRequest = stored["request"]!.AsObject();
        _storedRequest.Remove("readerScope");
        _storedRequest.Remove("revision");
        _result = (CatchUpObserverPartitionRequest)JsonSerializer.Deserialize<JobState>(stored.ToJsonString(), _options)!.Request;
    }

    [Fact] void should_store_no_reader_scope() => _storedRequest.ContainsKey("readerScope").ShouldBeFalse();
    [Fact] void should_store_no_revision() => _storedRequest.ContainsKey("revision").ShouldBeFalse();
    [Fact] void should_preserve_the_observer_key() => _result.ObserverKey.ShouldEqual(_observerKey);
    [Fact] void should_preserve_the_partition() => _result.Key.Value.ToString().ShouldEqual(_requestToStore.Key.Value.ToString());
    [Fact] void should_default_the_inherited_reader_scope_to_none() => _result.ReaderScope.ShouldBeNull();
    [Fact] void should_default_the_inherited_revision_to_none() => _result.Revision.ShouldBeNull();
}
