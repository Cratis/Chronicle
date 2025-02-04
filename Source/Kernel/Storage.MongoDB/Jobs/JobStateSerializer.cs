// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Strings;
using Microsoft.Extensions.DependencyInjection;
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
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JobState value)
    {
        var requestType = value.Request.GetType();
        var requestSerializer = BsonSerializer.SerializerRegistry.GetSerializer(requestType);
    }
    /// <inheritdoc />
    public override JobState Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var jobRequestElementName = nameof(JobState.Request).ToCamelCase();
        var rawBsonDocument = context.Reader.ReadRawBsonDocument();
        using var rawDocument = new RawBsonDocument(rawBsonDocument);
        rawDocument.Remove(jobRequestElementName);
        var bsonDocument = rawDocument.ToBsonDocument<BsonDocument>();
        var jobTypeString = bsonDocument.GetValue(nameof(JobState.Type).ToCamelCase()).AsString;
        var jobRequestType = jobTypes.GetRequestClrTypeFor(new(jobTypeString)).AsT0;
        var request = (IJobRequest)BsonSerializer.Deserialize(
            bsonDocument.GetElement(jobRequestElementName).ToBsonDocument(),
            jobRequestType);
        // bsonDocument.Remove(jobRequestElementName);
        var jobState = BsonSerializer.Deserialize<JobState>(bsonDocument);
        jobState.Request = request;
        return jobState;
    }
}

public class JobRequestSerializer<TJobRequest>(IJobTypes jobTypes) : SerializerBase<TJobRequest>
    where TJobRequest : IJobRequest
{
    /// <inheritdoc />
    public override TJobRequest Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var rawBsonDocument = context.Reader.ReadRawBsonDocument();
        using var rawDocument = new RawBsonDocument(rawBsonDocument);
        var bsonDocument = rawDocument.ToBsonDocument<BsonDocument>();
        return BsonSerializer.Deserialize<TJobRequest>(bsonDocument);
    }
}

/// <summary>
/// Represents a <see cref="IBsonSerializationProvider"/> for concepts.
/// </summary>
public class JobRequestSerializationProvider(IServiceProvider serviceProvider) : IBsonSerializationProvider
{
    /// <summary>
    /// Creates an instance of a serializer of the concept of the given type param T.
    /// </summary>
    /// <typeparam name="T">The Concept type.</typeparam>
    /// <returns><see cref="ConceptSerializer{T}"/> for the specific type.</returns>
    public static JobRequestSerializer<T> CreateSerializer<T>(IServiceProvider serviceProvider)
        where T : class, IJobRequest
        => ActivatorUtilities.CreateInstance<JobRequestSerializer<T>>(serviceProvider);

    /// <inheritdoc/>
    public IBsonSerializer GetSerializer(Type type)
    {
        if (type.IsAssignableTo(typeof(IJobRequest)))
        {
            var createSerializerMethod = GetType().GetMethod(nameof(CreateSerializer))!.MakeGenericMethod(type);
            return (createSerializerMethod.Invoke(null, [serviceProvider]) as IBsonSerializer)!;
        }

        return null!;
    }
}
