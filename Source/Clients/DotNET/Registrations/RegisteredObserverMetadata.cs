// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Reads observer metadata after explicit intent or catalog membership, without changing declaration-only public helpers.
/// </summary>
internal static class RegisteredObserverMetadata
{
    /// <summary>
    /// Resolves an effective reactor identity, including an explicit alias.
    /// </summary>
    /// <param name="provider">The store's artifact provider.</param>
    /// <param name="type">The registered CLR type.</param>
    /// <returns>The reactor identity.</returns>
    internal static ReactorId ReactorIdFor(IClientArtifactsProvider provider, Type type) =>
        ExplicitArtifactCatalog.From(provider)?.GetReactorIdFor(type) ?? new ReactorId(ArtifactIdentity.ObserverIdFor(type, type.GetCustomAttribute<ReactorAttribute>()?.Id.Value));

    /// <summary>
    /// Resolves an effective reducer identity, including an explicit alias.
    /// </summary>
    /// <param name="provider">The store's artifact provider.</param>
    /// <param name="type">The registered CLR type.</param>
    /// <returns>The reducer identity.</returns>
    internal static ReducerId ReducerIdFor(IClientArtifactsProvider provider, Type type) =>
        ExplicitArtifactCatalog.From(provider)?.GetReducerIdFor(type) ?? new ReducerId(ArtifactIdentity.ObserverIdFor(type, type.GetCustomAttribute<ReducerAttribute>()?.Id.Value));

    /// <summary>
    /// Checks declared sequence selection without requiring an observer marker.
    /// </summary>
    /// <param name="type">The registered CLR type.</param>
    /// <param name="observerSequence">The sequence declared on its observer attribute.</param>
    /// <returns>Whether an explicit sequence is supplied.</returns>
    internal static bool HasExplicitSequence(Type type, EventSequenceId? observerSequence) =>
        type.IsDefined(typeof(EventSequenceAttribute)) || observerSequence is not null;

    /// <summary>
    /// Resolves the existing metadata priorities against the owning event registry.
    /// </summary>
    /// <param name="type">The registered CLR type.</param>
    /// <param name="observerSequence">The sequence declared on its observer attribute.</param>
    /// <param name="eventTypes">The owning registry.</param>
    /// <param name="currentStore">The current store.</param>
    /// <param name="getHandledEventTypes">The owning invoker's accepted event identities, resolved only when inference is needed.</param>
    /// <returns>The resolved event sequence.</returns>
    /// <exception cref="EventStoreCannotBeCombinedWithExplicitEventSequence">A store and explicit sequence are both supplied.</exception>
    internal static EventSequenceId SequenceFor(Type type, EventSequenceId? observerSequence, IEventTypes eventTypes, EventStoreName currentStore, Func<IEnumerable<EventType>> getHandledEventTypes)
    {
        var store = type.GetCustomAttribute<EventStoreAttribute>();
        var sequence = type.GetCustomAttribute<EventSequenceAttribute>()?.Sequence;
        if (store is not null)
        {
            if ((sequence ?? observerSequence) is { } explicitSequence)
            {
                throw new EventStoreCannotBeCombinedWithExplicitEventSequence(type, store.EventStore, explicitSequence);
            }

            return new($"{EventSequenceId.InboxPrefix}{store.EventStore}");
        }

        return sequence ?? observerSequence ?? ObserverEventTypes.InferSequence(type, eventTypes, currentStore, getHandledEventTypes());
    }

    /// <summary>
    /// Validates type-based explicit observer intent without classifying unrelated shapes.
    /// </summary>
    /// <param name="type">The nominated observer type.</param>
    /// <param name="kind">The requested observer kind.</param>
    /// <param name="provider">Optional store declarations to check for contradictory explicit kind membership.</param>
    /// <exception cref="InvalidExplicitArtifact">The type is abstract, open or not concrete.</exception>
    /// <exception cref="ConflictingArtifactKind">The requested observer contradicts its marker-defined kind.</exception>
    internal static void Validate(Type type, ExplicitArtifactKind kind, IClientArtifactsProvider? provider = null)
    {
        if (type.IsAbstract || type.ContainsGenericParameters || type.IsInterface || (!type.IsClass && !type.IsValueType))
        {
            throw new InvalidExplicitArtifact(type, kind.ToString(), "A concrete closed observer type is required.");
        }
        var catalog = provider is null ? null : ExplicitArtifactCatalog.From(provider);
        ValidateKinds(
            type,
            kind == ExplicitArtifactKind.Reactor || catalog?.IsExplicit(type, ExplicitArtifactKind.Reactor) == true,
            kind == ExplicitArtifactKind.Reducer || catalog?.IsExplicit(type, ExplicitArtifactKind.Reducer) == true);
    }

    /// <summary>
    /// Validates observer kind intent against every observer marker, including non-generic reducers.
    /// </summary>
    /// <param name="type">The nominated type.</param>
    /// <param name="isReactor">Whether reactor intent was declared.</param>
    /// <param name="isReducer">Whether reducer intent was declared.</param>
    /// <exception cref="ConflictingArtifactKind">Both observer kinds are declared.</exception>
    internal static void ValidateKinds(Type type, bool isReactor, bool isReducer)
    {
        var markedReactor = type.IsAssignableTo(typeof(IReactor)) || type.IsDefined(typeof(ReactorAttribute));
        var markedReducer = type.IsAssignableTo(typeof(IReducer)) || type.IsDefined(typeof(ReducerAttribute));
        if ((isReactor || markedReactor) && (isReducer || markedReducer))
        {
            throw new ConflictingArtifactKind(type, nameof(ExplicitArtifactKind.Reactor), nameof(ExplicitArtifactKind.Reducer));
        }
    }
}
