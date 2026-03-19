// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.MongoDB.Serialization;
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
[BsonSerializerDisableAutoRegistration]
public class JobStateSerializer(IJobTypes jobTypes) : SerializerBase<JobState>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JobState value)
    {
        var actualType = value.GetType();
        var bsonClassMap = BsonClassMap.LookupClassMap(actualType);

        context.Writer.WriteStartDocument();

        foreach (var memberMap in bsonClassMap.AllMemberMaps)
        {
            // Skip the Request property - we'll handle it after
            if (memberMap.MemberName == nameof(JobState.Request))
            {
                continue;
            }

            context.Writer.WriteName(memberMap.ElementName);
            var memberValue = memberMap.Getter(value);
            BsonSerializer.Serialize(context.Writer, memberMap.MemberType, memberValue);
        }

        // Serialize the Request property
        if (value.Request is not null)
        {
            context.Writer.WriteName(nameof(JobState.Request).ToCamelCase());
            BsonSerializer.Serialize(context.Writer, value.Request.GetType(), value.Request);
        }

        context.Writer.WriteEndDocument();
    }

     /// <inheritdoc />
    public override JobState Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var actualType = args.NominalType ?? typeof(JobState);
        var (jobState, requestBookmark, endBookmark) = DeserializeJobStateExceptRequest(context, actualType);
        context.Reader.ReturnToBookmark(requestBookmark);
        var jobRequestClrType = jobTypes.GetRequestClrTypeForOrThrow(jobState.Type);
        actualType.GetProperty(nameof(JobState.Request))!
            .SetValue(jobState, BsonSerializer.Deserialize(context.Reader, jobRequestClrType));

        context.Reader.ReturnToBookmark(endBookmark);
        context.Reader.ReadEndDocument();
        return jobState;
    }

    static (JobState JobState, BsonReaderBookmark JobRequestBookmark, BsonReaderBookmark EndBookmark) DeserializeJobStateExceptRequest(BsonDeserializationContext context, Type actualType)
    {
        var jobState = (JobState)Activator.CreateInstance(actualType)!;
        BsonReaderBookmark requestBookmark = default!;
        context.Reader.ReadStartDocument();
        var bsonCLassMap = BsonClassMap.LookupClassMap(actualType).AllMemberMaps;
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
            var jobStateProperty = actualType.GetProperty(memberMap.MemberName)!;
            jobStateProperty.SetValue(jobState, BsonSerializer.Deserialize(context.Reader, memberMap.MemberType));
        }
        var endBookmark = context.Reader.GetBookmark();
        return (jobState, requestBookmark, endBookmark);
    }
}
