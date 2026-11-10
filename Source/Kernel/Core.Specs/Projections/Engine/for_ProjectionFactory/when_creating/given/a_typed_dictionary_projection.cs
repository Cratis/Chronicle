// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.given;

public class a_typed_dictionary_projection : a_child_collection_projection
{
    protected static readonly EventType _amountRecorded = new("AmountRecorded", EventTypeGeneration.First);
    protected AppendedEvent _incoming;
    protected Key _key;
    protected Changeset<AppendedEvent, ExpandoObject> _changeset;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {
              "type": "object",
              "properties": {
                "counts": { "type": "object", "additionalProperties": { "type": "integer", "format": "int32" } },
                "amounts": { "type": "object", "additionalProperties": { "type": "number", "format": "double" } }
              }
            }
            """);
        _definition.Children.Clear();
        _incoming = new(
            EventContext.EmptyWithEventSourceId("stream") with { EventType = _amountRecorded },
            new { slot = "first", amount = 5 }.AsExpandoObject());
        _key = new(_incoming.Context.EventSourceId.Value, ArrayIndexers.NoIndexers);
        _changeset = new(new ObjectComparer(), _incoming, new ExpandoObject());
    }

    protected async Task<object?> Project(string propertyPath, string expression, string key = "first")
    {
        _definition.From[_amountRecorded] = new(new Dictionary<PropertyPath, string> { [new(propertyPath)] = expression }, "$eventSourceId", null);
        var projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        projection.OnNext(new ProjectionEventContext(_key, _incoming, _changeset, projection.GetOperationTypeFor(_amountRecorded), false));
        var dictionary = (IDictionary<string, object?>)((IDictionary<string, object?>)_changeset.CurrentState)[propertyPath[..propertyPath.IndexOf('.')]]!;
        return dictionary[key];
    }
}
