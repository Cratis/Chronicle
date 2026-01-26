// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs.for_JobStateSerializer;

public class when_deserializing_with_different_job_request : given.a_job_state_serializer
{
    JobState _original;
    JobState _result;

    void Establish()
    {
        _original = new JobState
        {
            Id = JobId.New(),
            Details = "Another Job",
            Type = typeof(AnotherJobRequest),
            Status = JobStatus.CompletedSuccessfully,
            Created = DateTimeOffset.UtcNow,
            StatusChanges = [],
            Progress = new JobProgress(),
            Request = new AnotherJobRequest("test-id-123", "Test Description")
        };

        _jobTypes.GetRequestClrTypeFor(Arg.Is<JobType>(t => t.Value == nameof(AnotherJobRequest)))
            .Returns(Result<Type, IJobTypes.GetRequestClrTypeForError>.Success(typeof(AnotherJobRequest)));
    }

    void Because()
    {
        var bsonDocument = _original.ToBsonDocument();
        using var reader = new BsonDocumentReader(bsonDocument);
        var context = BsonDeserializationContext.CreateRoot(reader);
        _result = _serializer.Deserialize(context, default);
    }

    [Fact] void should_deserialize_job_id() => _result.Id.ShouldEqual(_original.Id);
    [Fact] void should_deserialize_job_type() => _result.Type.ShouldEqual(_original.Type);
    [Fact] void should_deserialize_job_status() => _result.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_deserialize_request() => _result.Request.ShouldBeOfExactType<AnotherJobRequest>();
    [Fact] void should_have_correct_request_id() => ((AnotherJobRequest)_result.Request).Id.ShouldEqual("test-id-123");
    [Fact] void should_have_correct_request_description() => ((AnotherJobRequest)_result.Request).Description.ShouldEqual("Test Description");
}
