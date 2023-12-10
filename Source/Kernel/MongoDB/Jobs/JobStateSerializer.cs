// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.MongoDB;
using Aksio.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace Aksio.Cratis.Kernel.MongoDB.Jobs;

/// <summary>
/// Represents a <see cref="IBsonSerializer{T}"/>  for <see cref="JobState"/>.
/// </summary>
public class JobStateSerializer : SerializerBase<JobState>, IBsonDocumentSerializer, IBsonIdProvider
{
    const string Type = "_type";
    const string RequestType = "_requestType";

    static readonly ConceptSerializer<JobId> _jobIdSerializer = new();

    readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStateSerializer"/> class.
    /// </summary>
    /// <param name="options"><see cref="JsonSerializerOptions"/> to use.</param>
    public JobStateSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JobState value)
    {
        var request = value.Request;
        value.Request = null!;

        var document = (value.ToBsonValue() as BsonDocument)!;
        document.Remove("_id");
        document
            .Set("_type", value.GetType().AssemblyQualifiedName);

        if (request is not null)
        {
            var requestProperty = nameof(JobState.Request).ToCamelCase();
            document
                .Set(requestProperty, request.ToBsonDocument(request.GetType()))
                .Set(RequestType, request.GetType().AssemblyQualifiedName);
        }
        var serializer = BsonSerializer.LookupSerializer<BsonDocument>();
        serializer.Serialize(context, document);

        value.Request = request!;
    }

    /// <inheritdoc/>
    public override JobState Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        var type = System.Type.GetType(document[Type].AsString) ?? typeof(JobState);
        Type? requestType = null;
        BsonDocument? requestAsDocument = null;

        if (document.Contains(RequestType))
        {
            requestType = System.Type.GetType(document[RequestType].AsString);
            var requestProperty = nameof(JobState.Request).ToCamelCase();
            requestAsDocument = document[requestProperty].AsBsonDocument;
            document.Remove(requestProperty);
        }

        if (document.Contains("_id"))
        {
            document["id"] = document["_id"];
            document.Remove("id");
        }
        var jobStateObjectRepresentation = BsonTypeMapper.MapToDotNetValue(document);
        var jobStateAsJson = JsonSerializer.Serialize(jobStateObjectRepresentation, _options);
        var jobState = (JsonSerializer.Deserialize(jobStateAsJson, type, _options) as JobState)!;

        if (requestType is not null && requestAsDocument is not null)
        {
            var jobStateRequestObjectRepresentation = BsonTypeMapper.MapToDotNetValue(requestAsDocument);
            var jobStateRequestAsJson = JsonSerializer.Serialize(jobStateRequestObjectRepresentation, _options);
            jobState.Request = JsonSerializer.Deserialize(jobStateRequestAsJson, requestType, _options)!;
        }

        return jobState;
    }

    /// <inheritdoc/>
    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
    {
        serializationInfo = new BsonSerializationInfo(
            memberName,
            _jobIdSerializer,
            typeof(BsonValue));

        return true;
    }

    /// <inheritdoc/>
    public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
    {
        id = ((JobState)document).Id;
        idNominalType = typeof(JobId);
        idGenerator = BsonBinaryDataGuidGenerator.GetInstance(GuidRepresentation.Standard);
        return true;
    }

    /// <inheritdoc/>
    public void SetDocumentId(object document, object id)
    {
        ((JobState)document).Id = (JobId)id;
    }
}
