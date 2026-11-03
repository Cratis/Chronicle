// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.given;

public class a_dictionary_projection : a_child_collection_projection
{
    protected static readonly EventType _payloadReceived = new("PayloadReceived", EventTypeGeneration.First);
    protected AppendedEvent _incoming;
    protected Key _key;
    protected Changeset<AppendedEvent, ExpandoObject> _changeset;
    protected JsonSchema _schema;
    protected ExpandoObjectConverter _converter;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {
              "type": "object",
              "properties": {
                "payload": { "type": "object", "additionalProperties": {} },
                "details": {
                  "type": "object",
                  "properties": {
                    "payload": { "type": "object", "additionalProperties": {} }
                  }
                }
              }
            }
            """);
        _readModel.Schemas[ReadModelGeneration.First] = _schema;
        _definition.Children.Clear();
        _incoming = new(
            EventContext.EmptyWithEventSourceId("stream") with { EventType = _payloadReceived },
            new { data = new { value = "kept" }, nested = new { data = new { value = "kept" } } }.AsExpandoObject());
        _key = new(_incoming.Context.EventSourceId.Value, ArrayIndexers.NoIndexers);
        _changeset = new(new ObjectComparer(), _incoming, new ExpandoObject());
        _converter = new(new TypeFormats());
    }

    public record DictionaryReadModel(IDictionary<string, object> Payload);

    public record NestedDictionaryReadModel(DictionaryReadModel Details);
}
