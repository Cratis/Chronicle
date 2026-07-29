// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs.for_JobStateSerializer;

/// <summary>
/// JobState.Request is declared as `default!`, so a state persisted before its request is assigned has no request
/// to write - and Serialize only writes the field when the request is there. Reading such a document back has to
/// work: the state carries its own status and progress, which is the whole reason it was written.
/// </summary>
public class when_round_tripping_a_job_state_without_a_request : given.a_job_state_serializer
{
    JobState _original;
    JobState _result;
    Exception _exception;

    void Establish() => _original = new JobState
    {
        Id = JobId.New(),
        Details = "Test Job",
        Type = typeof(SampleJobRequest),
        Status = JobStatus.Running,
        Created = DateTimeOffset.UtcNow,
        StatusChanges = [],
        Progress = new JobProgress()
    };

    void Because() => _exception = Catch.Exception(() =>
    {
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
        {
            _serializer.Serialize(BsonSerializationContext.CreateRoot(writer), default, _original);
        }

        using var reader = new BsonDocumentReader(document);
        _result = _serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), default);
    });

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_deserialize_job_id() => _result.Id.ShouldEqual(_original.Id);
    [Fact] void should_deserialize_job_status() => _result.Status.ShouldEqual(_original.Status);
    [Fact] void should_leave_the_request_unset() => _result.Request.ShouldBeNull();
}
