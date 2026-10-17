// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Guards the atomic control replacement against an older membership revision.
/// </summary>
internal static class ObserverTypeSetControlUpdate
{
    /// <summary>
    /// Adds a conditional whole-control replacement to an observer state update.
    /// </summary>
    /// <param name="update">The other observer-owned fields to save.</param>
    /// <param name="control">The incoming control.</param>
    /// <returns>A single update pipeline preserving a newer stored control.</returns>
    /// <exception cref="UnsupportedObserverStateUpdateOperator">The update contains an operator other than $set.</exception>
    internal static UpdateDefinition<ObserverState> Guard(UpdateDefinition<ObserverState> update, ObserverTypeSetControl control)
    {
        var registry = BsonSerializer.SerializerRegistry;
        var args = new RenderArgs<ObserverState>(registry.GetSerializer<ObserverState>(), registry);
        var field = new ExpressionFieldDefinition<ObserverState, ObserverTypeSetControl?>(state => state.TypeSetControl).Render(args).FieldName;
        var revisionField = new ExpressionFieldDefinition<ObserverState, ObserverTypeSetRevision>(state => state.TypeSetControl!.Revision).Render(args).FieldName;
        var rendered = update.Render(args).AsBsonDocument;
        var unsupportedOperator = rendered.Names.FirstOrDefault(name => name != "$set");
        if (unsupportedOperator is not null)
        {
            throw new UnsupportedObserverStateUpdateOperator(unsupportedOperator);
        }

        var fields = rendered["$set"].AsBsonDocument;

        // Pipeline values are expressions, unlike ordinary $set values. Literal wrapping
        // preserves strings beginning with '$' and documents containing expression names.
        var set = new BsonDocument(fields.Select(element => new BsonElement(element.Name, new BsonDocument("$literal", element.Value))));
        set[field] = new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$lte", new BsonArray
            {
                new BsonDocument("$ifNull", new BsonArray { $"${revisionField}", control.Revision.Value }),
                control.Revision.Value
            }),
            new BsonDocument("$literal", control.ToBsonDocument()),
            $"${field}"
        });

        return new PipelineUpdateDefinition<ObserverState>(new BsonDocument[] { new("$set", set) });
    }
}
