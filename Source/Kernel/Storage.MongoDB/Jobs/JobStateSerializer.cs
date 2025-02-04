// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
namespace Cratis.Chronicle.Storage.MongoDB.Jobs;

/// <summary>
/// Represents a <see cref="SerializerBase{TValue}"/> for <see cref="JobState"/>.
/// </summary>
/// <param name="jobTypes">The <see cref="IJobTypes"/>.</param>
public class JobStateSerializer(IJobTypes jobTypes) : SerializerBase<JobState>
{
    /// <inheritdoc />
    public override JobState Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var rawBsonDocument = context.Reader.ReadRawBsonDocument();
        using var rawDocument = new RawBsonDocument(rawBsonDocument);
        var bsonDocument = rawDocument.ToBsonDocument<BsonDocument>();
        var jobTypeString = bsonDocument.GetValue(nameof(JobState.Type).ToCamelCase()).AsString;
        var jobRequestType = jobTypes.GetRequestClrTypeFor(new(jobTypeString)).AsT0;
        var jobRequestElementName = nameof(JobState.Request).ToCamelCase();
        var request = (IJobRequest)BsonSerializer.Deserialize(
            bsonDocument.GetElement(jobRequestElementName).ToBsonDocument(),
            jobRequestType);
        bsonDocument.Remove(jobRequestElementName);
        var jobState = BsonSerializer.Deserialize<JobState>(bsonDocument);
        jobState.Request = request;
        return jobState;
    }
}