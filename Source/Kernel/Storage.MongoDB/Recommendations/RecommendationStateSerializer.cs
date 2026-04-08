// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage.MongoDB.Serialization;
using Cratis.Chronicle.Storage.Recommendations;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Recommendations;

/// <summary>
/// Represents a <see cref="SerializerBase{TValue}"/> for <see cref="RecommendationState"/>.
/// </summary>
[BsonSerializerDisableAutoRegistration]
public class RecommendationStateSerializer : SerializerBase<RecommendationState>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, RecommendationState value)
    {
        var bsonClassMap = BsonClassMap.LookupClassMap(typeof(RecommendationState));

        context.Writer.WriteStartDocument();

        foreach (var memberMap in bsonClassMap.AllMemberMaps)
        {
            if (memberMap.MemberName == nameof(RecommendationState.Request))
            {
                continue;
            }

            context.Writer.WriteName(memberMap.ElementName);
            var memberValue = memberMap.Getter(value);
            BsonSerializer.Serialize(context.Writer, memberMap.MemberType, memberValue);
        }

        if (value.Request is not null)
        {
            context.Writer.WriteName(nameof(RecommendationState.Request).ToCamelCase());
            BsonSerializer.Serialize(context.Writer, value.Request.GetType(), value.Request);
        }

        context.Writer.WriteEndDocument();
    }

    /// <inheritdoc/>
    public override RecommendationState Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var (state, requestBookmark, endBookmark) = DeserializeStateExceptRequest(context);

        if (requestBookmark is not null && state.Type != RecommendationType.NotSet)
        {
            context.Reader.ReturnToBookmark(requestBookmark);
            var requestClrType = GetRequestClrType(state.Type);
            if (requestClrType is not null)
            {
                state.Request = (IRecommendationRequest)BsonSerializer.Deserialize(context.Reader, requestClrType);
            }
            else
            {
                context.Reader.SkipValue();
            }
        }

        if (endBookmark is not null)
        {
            context.Reader.ReturnToBookmark(endBookmark);
        }

        context.Reader.ReadEndDocument();
        return state;
    }

    static Type? GetRequestClrType(RecommendationType recommendationType)
    {
        try
        {
            Type recommendationInterfaceType = recommendationType;
            var genericRecommendationInterface = recommendationInterfaceType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRecommendation<>));

            return genericRecommendationInterface?.GetGenericArguments()[0];
        }
        catch
        {
            return null;
        }
    }

    static (RecommendationState State, BsonReaderBookmark? RequestBookmark, BsonReaderBookmark? EndBookmark) DeserializeStateExceptRequest(BsonDeserializationContext context)
    {
        var state = new RecommendationState();
        BsonReaderBookmark? requestBookmark = null;
        context.Reader.ReadStartDocument();
        var bsonClassMap = BsonClassMap.LookupClassMap(typeof(RecommendationState)).AllMemberMaps;

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var fieldName = context.Reader.ReadName();
            if (fieldName.ToPascalCase() == nameof(RecommendationState.Request))
            {
                requestBookmark = context.Reader.GetBookmark();
                context.Reader.SkipValue();
                continue;
            }

            var memberMap = bsonClassMap.FirstOrDefault(map => map.ElementName.Equals(fieldName));
            if (memberMap == null)
            {
                context.Reader.SkipValue();
                continue;
            }

            var property = typeof(RecommendationState).GetProperty(memberMap.MemberName)!;
            property.SetValue(state, BsonSerializer.Deserialize(context.Reader, memberMap.MemberType));
        }

        var endBookmark = context.Reader.GetBookmark();
        return (state, requestBookmark, endBookmark);
    }
}
