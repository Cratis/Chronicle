// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Discovers and validates read models that declare an event stream.
/// </summary>
internal static class EventStreamReadModel
{
    /// <summary>
    /// Gets the model's non-inherited stream declaration.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <returns>The declaration, if present.</returns>
    internal static IEventStreamReadModelDeclaration? DeclarationFor(Type model) =>
        model.GetCustomAttributes(false).OfType<IEventStreamReadModelDeclaration>().SingleOrDefault();

    /// <summary>
    /// Refuses a key-only read of a declared stream model.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <exception cref="DecisionReadRefused">The model requires stream scope.</exception>
    internal static void RequireStreamScope(Type model)
    {
        if (DeclarationFor(model) is not null)
        {
            throw new DecisionReadRefused(DecisionReadRefusalReason.ReadModelRequiresStreamScope, model);
        }
    }

    /// <summary>
    /// Refuses stream declarations on reducers and reducer-backed models.
    /// </summary>
    /// <param name="model">The model or reducer type.</param>
    /// <exception cref="InvalidEventStreamReadModel">The model declares a stream.</exception>
    internal static void RefuseReducer(Type model)
    {
        if (DeclarationFor(model) is not null)
        {
            throw new InvalidEventStreamReadModel(model, "Reducers do not support stream-scoped folds.");
        }
    }

    /// <summary>
    /// Validates a stream declaration and the projection's root keys.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <param name="definition">The built projection definition.</param>
    /// <exception cref="InvalidEventStreamReadModel">The declaration or root key is invalid.</exception>
    internal static void ValidateProjection(Type model, ProjectionDefinition definition)
    {
        if (DeclarationFor(model) is not { } declaration) return;
        ValidateDeclaration(model, declaration, EventSources.EventSources.Describe(declaration.EventSource));
        static bool IsSource(string? key) => string.IsNullOrEmpty(key) || key == "$eventSourceId";
        if (definition.From.Values.Any(from => !IsSource(from.Key)) ||
            definition.RemovedWith.Values.Any(removed => !IsSource(removed.Key)) || !IsSource(definition.All.Key))
        {
            throw new InvalidEventStreamReadModel(model, "The model must be keyed by event source id, not by a stream id or event property.");
        }
    }

    /// <summary>
    /// Resolves a query scope from the registered event source definition.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <param name="sources">The registered event sources.</param>
    /// <param name="source">The owning source id.</param>
    /// <param name="streamId">The requested stream id.</param>
    /// <returns>The stream scope.</returns>
    /// <exception cref="InvalidEventStreamReadModel">The model has no valid declaration.</exception>
    /// <exception cref="DecisionReadRefused">The scope has an invalid key.</exception>
    internal static DecisionReadStreamScope ScopeFor(Type model, IEventSources sources, EventSourceId source, EventStreamId streamId)
    {
        var declaration = DeclarationFor(model) ?? throw new InvalidEventStreamReadModel(model, "The stream instance overload requires [EventStream<TSource>].");
        var definition = sources.GetFor(declaration.EventSource);
        ValidateDeclaration(model, declaration, definition);
        var scope = new DecisionReadStreamScope(source, declaration.Stream, streamId, definition.EventSourceType);
        ValidateScope(model, scope);
        return scope;
    }

    /// <summary>
    /// Checks that a decision read's route matches the declared route by value.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <param name="sources">The registered event sources.</param>
    /// <param name="scope">The supplied decision scope.</param>
    /// <exception cref="DecisionReadRefused">The route does not match.</exception>
    internal static void ValidateMatch(Type model, IEventSources sources, DecisionReadStreamScope scope)
    {
        if (DeclarationFor(model) is not { } declaration) return;
        var definition = sources.GetFor(declaration.EventSource);
        ValidateDeclaration(model, declaration, definition);
        if (scope.EventStreamType.Value != declaration.Stream || scope.EventSourceType != definition.EventSourceType)
        {
            throw new DecisionReadRefused(DecisionReadRefusalReason.StreamScopeDoesNotMatchReadModel, model);
        }
    }

    static void ValidateDeclaration(Type model, IEventStreamReadModelDeclaration declaration, EventSourceDefinition source)
    {
        if (typeof(IEventSource).IsAssignableFrom(model))
        {
            throw new InvalidEventStreamReadModel(model, "Declare the generic attribute on a read model, not an event source definition.");
        }
        if (string.IsNullOrWhiteSpace(declaration.Stream) || ((EventStreamType)declaration.Stream).IsAll || source.FindStream(declaration.Stream) is null)
        {
            throw new InvalidEventStreamReadModel(model, $"'{declaration.Stream}' must name one stream declared by '{declaration.EventSource.FullName}'.");
        }
    }

    static void ValidateScope(Type model, DecisionReadStreamScope scope)
    {
        if (scope.EventSourceId is null || string.IsNullOrWhiteSpace(scope.EventSourceId.Value) ||
            scope.EventSourceId.Value == "*" || scope.EventSourceId.Value.Contains('#') || scope.EventSourceId.Value != scope.EventSourceId.Value.Trim() ||
            scope.EventStreamId is null || string.IsNullOrWhiteSpace(scope.EventStreamId.Value))
        {
            throw new DecisionReadRefused(DecisionReadRefusalReason.InvalidKey, model);
        }
    }
}
