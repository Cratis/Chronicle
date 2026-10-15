// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Events;

/// <summary>Selects handler-declared content before any lossy public event conversion.</summary>
internal static class DecisionReadEventConverters
{
    /// <summary>Converts one replay event without falling back to a different generation.</summary>
    /// <param name="input">The complete transport event, including revisions and generational content.</param>
    /// <param name="store">The owning store.</param>
    /// <param name="declared">The handler-declared event identity.</param>
    /// <param name="model">The model being reduced.</param>
    /// <param name="options">The owning serializer options.</param>
    /// <returns>The exact declared event and its context.</returns>
    /// <exception cref="DecisionReadRefused">The requested generation cannot safely be supplied.</exception>
    internal static EventAndContext ForDecisionRead(this Contracts.Sequences.AppendedEventResponse input, IEventStore store, EventType declared, Type model, JsonSerializerOptions options)
    {
        var context = input.Context.ToClient(store.Name, store.Namespace);

        // Storage that revises in place keeps no revision list, only the marker of the revised generation.
        if ((input.RevisedGeneration is { } revised && revised != declared.Generation.Value) ||
            input.Revisions.Any(revision => revision.Generation != declared.Generation.Value))
        {
            throw new DecisionReadRefused(DecisionReadRefusalReason.ReducerGeneration, model);
        }
        var revision = input.Revisions.LastOrDefault();
        var content = revision?.Content ?? input.Content;
        if (revision is null && context.EventType.Generation != declared.Generation)
        {
            content = input.GenerationalContent.FirstOrDefault(pair => pair.Key == declared.Generation.Value).Value
                ?? throw new DecisionReadRefused(DecisionReadRefusalReason.ReducerGeneration, model);
        }
        if (!store.EventTypes.HasFor(declared.Id)) throw new DecisionReadRefused(DecisionReadRefusalReason.ReducerGeneration, model);
        var clrType = store.EventTypes.GetClrTypeFor(declared.Id, declared.Generation);
        if (store.EventTypes.GetEventTypeFor(clrType) != declared)
        {
            throw new DecisionReadRefused(DecisionReadRefusalReason.ReducerGeneration, model);
        }
        var instance = JsonSerializer.Deserialize(content, clrType, options)
            ?? throw new DecisionReadRefused(DecisionReadRefusalReason.ReducerGeneration, model);
        return new(instance, context with { EventType = declared });
    }
}
