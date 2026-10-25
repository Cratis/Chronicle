// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Json;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing.given;

public class a_factory : Specification
{
    protected static readonly EventStoreName Store = "store";
    protected static readonly EventStoreNamespaceName Namespace = "tenant";
    protected IEventTypesStorage _schemas;
    protected IJsonSchemaMetadataManager _metadata;
    protected JsonSchema _schema;
    protected AppendedEvent _event;
    protected UniqueConstraintEventDefinition _definition;
    private protected UniqueConstraintRelease _factory;
    private protected ReleasedUniqueConstraintComponents _result;
    protected JsonObject? _released;
    protected JsonObject _releaseInput;
    protected Dictionary<string, ProtectedValueReleaseStatus> _statuses;
    protected string _identifier;
    protected string _original;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
                "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "number":{"type":"integer","format":"int32","compliance":[{"metadataType":"PII","details":""}]},
                "identifier":{"type":"string","format":"guid","security":[{"metadataType":"Encrypted","details":""}]},
                "day":{"type":"string","format":"date","security":[{"metadataType":"Encrypted","details":""}]},
                "profile":{"type":"object","properties":{"name":{"type":"string"}}},
                "names":{"type":"array","items":{"type":"string"}},"nameSuffix":{"type":"string"}}}
            """);
        _schemas = Substitute.For<IEventTypesStorage>();
        _schemas.GetFor("event", 2).Returns(new EventTypeSchema(new("event", 2), EventTypeOwner.Server, EventTypeSource.Code, _schema));
        _metadata = Substitute.For<IJsonSchemaMetadataManager>();
        _statuses = [];
        _metadata.ReleaseWithStatus(Store, Namespace, _schema, Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call =>
        {
            _identifier = call.ArgAt<string>(3);
            _releaseInput = call.Arg<JsonObject>();
            return new StatusedRelease(_released ?? _releaseInput.DeepClone().AsObject(), _statuses);
        });
        _factory = new(_schemas, _metadata, new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())));
        _definition = new("event", ["name"]);
        SetContent("""{"name":"ciphertext","number":"opaque integer","identifier":"opaque guid","day":"opaque date"}""");
    }

    protected void SetContent(string json)
    {
        var content = new ExpandoObjectConverter(new TypeFormats()).ToExpandoObject(JsonNode.Parse(json)!.AsObject(), new JsonSchema());
        _event = new(EventContext.From(Store, Namespace, new("event", 2), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 1UL, CorrelationId.NotSet), content);
        _original = JsonSerializer.SerializeToNode(content, Globals.JsonSerializerOptions)!.ToJsonString();
    }

    private protected Task<ReleasedUniqueConstraintComponents> Create() => _factory.Create(Store, Namespace, _definition, _event);

    protected string CurrentContent() => JsonSerializer.SerializeToNode(_event.Content, Globals.JsonSerializerOptions)!.ToJsonString();
}
