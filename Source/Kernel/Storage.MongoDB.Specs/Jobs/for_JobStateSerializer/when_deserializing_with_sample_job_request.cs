// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs.for_JobStateSerializer;

public class when_deserializing_with_sample_job_request : given.a_job_state_serializer
{
    JobState _original;
    JobState _result;

    void Establish()
    {
        _original = new JobState
        {
            Id = JobId.New(),
            Details = "Test Job",
            Type = typeof(SampleJobRequest),
            Status = JobStatus.Running,
            Created = DateTimeOffset.UtcNow,
            StatusChanges = [],
            Progress = new JobProgress(),
            Request = new SampleJobRequest("TestName", 42)
        };

        _jobTypes.GetRequestClrTypeFor(Arg.Is<JobType>(t => t.Value == nameof(SampleJobRequest)))
            .Returns(Result<Type, IJobTypes.GetRequestClrTypeForError>.Success(typeof(SampleJobRequest)));
    }

    void Because()
    {
        var bsonDocument = _original.ToBsonDocument();
        using var reader = new BsonDocumentReader(bsonDocument);
        var context = BsonDeserializationContext.CreateRoot(reader);
        _result = _serializer.Deserialize(context, default);
    }

    [Fact] void should_deserialize_job_id() => _result.Id.ShouldEqual(_original.Id);
    [Fact] void should_deserialize_job_details() => _result.Details.ShouldEqual(_original.Details);
    [Fact] void should_deserialize_job_type() => _result.Type.ShouldEqual(_original.Type);
    [Fact] void should_deserialize_job_status() => _result.Status.ShouldEqual(_original.Status);
    [Fact] void should_deserialize_request() => _result.Request.ShouldBeOfExactType<SampleJobRequest>();
    [Fact] void should_have_correct_request_name() => ((SampleJobRequest)_result.Request).Name.ShouldEqual("TestName");
    [Fact] void should_have_correct_request_count() => ((SampleJobRequest)_result.Request).Count.ShouldEqual(42);
}
