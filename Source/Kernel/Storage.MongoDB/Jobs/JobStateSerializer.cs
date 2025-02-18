// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
namespace Cratis.Chronicle.Storage.MongoDB.Jobs;

/// <summary>
/// Represents a <see cref="SerializerBase{TValue}"/> for <see cref="JobState"/>.
/// </summary>
/// <param name="jobTypes">The <see cref="IJobTypes"/>.</param>
public class JobStateSerializer(IJobTypes jobTypes) : SerializerBase<JobState>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JobState value)
        => BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>().Serialize(context, args, value);

    /// <inheritdoc />
    public override JobState Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var (jobState, requestBookmark, endBookmark) = DeserializeJobStateExceptRequest(context);
        context.Reader.ReturnToBookmark(requestBookmark);
        var jobRequestClrType = jobTypes.GetRequestClrTypeFor(jobState.Type).Match(type => type, _ => throw new UnknownClrTypeForJobType(jobState.Type));
        ValueType.GetProperty(nameof(JobState.Request))!
            .SetValue(jobState, BsonSerializer.Deserialize(context.Reader, jobRequestClrType));

        context.Reader.ReturnToBookmark(endBookmark);
        context.Reader.ReadEndDocument();
        return jobState;
    }

    static (JobState JobState, BsonReaderBookmark JobRequestBookmark, BsonReaderBookmark EndBookmark) DeserializeJobStateExceptRequest(BsonDeserializationContext context)
    {
        var jobState = new JobState();
        BsonReaderBookmark requestBookmark = default!;
        context.Reader.ReadStartDocument();
        var bsonCLassMap = BsonClassMap.LookupClassMap(typeof(JobState)).AllMemberMaps;
        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var fieldName = context.Reader.ReadName();
            if (fieldName.ToPascalCase() == nameof(JobState.Request))
            {
                requestBookmark = context.Reader.GetBookmark();
                context.Reader.SkipValue();
                continue;
            }
            var memberMap = bsonCLassMap.FirstOrDefault(map => map.ElementName.Equals(fieldName));
            if (memberMap == null)
            {
                context.Reader.SkipValue();
                continue;
            }
            var jobStateProperty = typeof(JobState).GetProperty(memberMap.MemberName)!;
            jobStateProperty.SetValue(jobState, BsonSerializer.Deserialize(context.Reader, memberMap.MemberType));
        }
        var endBookmark = context.Reader.GetBookmark();
        return (jobState, requestBookmark, endBookmark);
    }
}