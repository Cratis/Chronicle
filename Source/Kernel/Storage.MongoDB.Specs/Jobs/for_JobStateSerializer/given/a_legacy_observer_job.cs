// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.MongoDB.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs.for_JobStateSerializer.given;

public class a_legacy_observer_job : Specification
{
    protected ObserverKey _observerKey = new("observer", "event-store", "namespace", EventSequenceId.Log);
    protected BsonDocument _storedRequest;

    protected TRequest RoundTrip<TRequest>(TRequest request)
        where TRequest : IJobRequest
    {
        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetRequestClrTypeFor(Arg.Any<JobType>())
            .Returns(Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(TRequest)));
        var serializer = new JobStateSerializer(jobTypes);
        var state = new JobState { Id = JobId.New(), Type = new("observer-job"), Status = JobStatus.Running, Request = request };
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
        {
            serializer.Serialize(BsonSerializationContext.CreateRoot(writer), new BsonSerializationArgs { NominalType = typeof(JobState) }, state);
        }

        _storedRequest = document["request"].AsBsonDocument;
        _storedRequest.Remove("readerScope");
        _storedRequest.Remove("revision");

        // Encode and decode actual BSON bytes, as the Mongo job storage does.
        var persisted = BsonSerializer.Deserialize<BsonDocument>(document.ToBson());
        using var reader = new BsonDocumentReader(persisted);
        var restored = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), new BsonDeserializationArgs { NominalType = typeof(JobState) });
        return (TRequest)restored.Request;
    }
}
