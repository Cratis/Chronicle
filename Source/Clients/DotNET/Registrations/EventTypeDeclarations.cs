// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Sinks;

using EventTypeVisibility = Cratis.Chronicle.Contracts.Events.EventTypeVisibility;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Merges supplied event facets before applying defaults or resolving historical references.
/// </summary>
internal sealed class EventTypeDeclarations
{
    readonly FrozenDictionary<Type, EventTypeRegistration> _declarations;

    /// <summary>
    /// Initializes a new instance of <see cref="EventTypeDeclarations"/>.
    /// </summary>
    /// <param name="types">The union of discovered and nominated events.</param>
    /// <param name="explicitDeclarations">The options snapshot's declarations.</param>
    internal EventTypeDeclarations(IEnumerable<Type> types, IEnumerable<EventTypeRegistration> explicitDeclarations)
    {
        var recipes = explicitDeclarations.ToLookup(_ => _.Type);
        _declarations = types.Distinct().ToFrozenDictionary(type => type, type =>
        {
            var declaration = FromMarkers(type);
            foreach (var peer in recipes[type])
            {
                declaration = Merge(declaration, peer);
            }

            return declaration;
        });

        // Validate every identity, including historical references, before publishing any descriptors.
        foreach (var type in _declarations.Keys)
        {
            ResolveIdentity(type, []);
        }

        var duplicate = _declarations.Keys.Select(type => (Type: type, Identity: ResolveIdentity(type, [])))
            .GroupBy(_ => _.Identity)
            .Where(_ => _.Count() > 1)
            .OrderBy(_ => _.Key.Id.Value)
            .ThenBy(_ => _.Key.Generation.Value)
            .FirstOrDefault();
        if (duplicate is not null)
        {
            throw new MultipleEventTypesWithSameIdFound(duplicate.Key, duplicate.Select(_ => _.Type));
        }
    }

    /// <summary>
    /// Resolves ownership once for all descriptor consumers, inheriting a historical type's current-generation owner only when omitted.
    /// </summary>
    /// <param name="type">The event CLR type.</param>
    /// <param name="getDeclaredStore">The peer-merged or legacy declared ownership lookup.</param>
    /// <returns>The effective owning store, or null when none is declared.</returns>
    /// <exception cref="InvalidExplicitArtifact">Historical references form a cycle.</exception>
    internal static string? ResolveEventStore(Type type, Func<Type, string?> getDeclaredStore)
    {
        var originalType = type;
        var visited = new HashSet<Type>();
        while (visited.Add(type))
        {
            if (getDeclaredStore(type) is { } store) return store;
            if (type.GetCustomAttribute<EventTypeGenerationForAttribute>() is not { } historical) return null;
            type = historical.EventTypeClrType;
        }

        throw new InvalidExplicitArtifact(originalType, "event type", "Historical generation references form a cycle.");
    }

    /// <summary>
    /// Resolves independent registration visibility and publication eligibility for a store.
    /// </summary>
    /// <param name="type">The registered CLR type.</param>
    /// <param name="currentStore">The owning client store.</param>
    /// <returns>The effective event descriptor.</returns>
    internal EventTypeDescriptor GetDescriptorFor(Type type, EventStoreName currentStore)
    {
        var declaration = _declarations[type];
        var store = ResolveEventStore(type, candidate =>
            (_declarations.TryGetValue(candidate, out var registered) ? registered : FromMarkers(candidate)).EventStore?.Value.Value);
        var publicDeclaration = declaration.Visibility?.Value == EventTypeVisibility.Public;
        return new()
        {
            ClrType = type,
            EventType = ResolveIdentity(type, []),
            EventStore = store,
            RegistrationVisibility = declaration.Visibility?.Value ??
                (store is not null ? EventTypeVisibility.Public : EventTypeVisibility.Private),
            PublicationEligibility = type.IsPublicEventType(currentStore) || publicDeclaration || store == currentStore.Value,
            PublishingSequence = declaration.Sequence?.Value ?? EventSequenceId.Outbox
        };
    }

    static EventTypeRegistration FromMarkers(Type type)
    {
        var attribute = type.GetCustomAttribute<EventTypeAttribute>();
        var historical = type.GetCustomAttribute<EventTypeGenerationForAttribute>();
        if (attribute is not null && historical is not null)
        {
            throw new EventTypeGenerationForCannotBeCombinedWithEventType(type);
        }

        var store = type.GetEventStoreName();
        var sequence = type.GetCustomAttribute<PublishToAttribute>();
        DeclaredFacet<EventTypeGeneration>? generation = (attribute, historical) switch
        {
            ({ } current, _) => new(current.Generation, nameof(EventTypeAttribute)),
            (_, { } previous) => new(previous.Generation, nameof(EventTypeGenerationForAttribute)),
            _ => null
        };
        return new(
            type,
            attribute is null ? null : new(ArtifactIdentity.EventTypeFor(type).Id, nameof(EventTypeAttribute)),
            generation,
            type.IsDefined(typeof(PublicAttribute), false) ? new(EventTypeVisibility.Public, nameof(PublicAttribute)) : null,
            store is null ? null : new(new(store), type.IsDefined(typeof(EventStoreAttribute), true) ? "type EventStoreAttribute" : "assembly EventStoreAttribute"),
            sequence is null ? null : new(sequence.Sequence, nameof(PublishToAttribute)));
    }

    static EventTypeRegistration Merge(EventTypeRegistration first, EventTypeRegistration second) => new(
        first.Type,
        DeclaredFacet<EventTypeId>.Merge(first.Type, nameof(EventTypeRegistration.Id), first.Id, second.Id),
        DeclaredFacet<EventTypeGeneration>.Merge(first.Type, nameof(EventTypeRegistration.Generation), first.Generation, second.Generation),
        DeclaredFacet<EventTypeVisibility>.Merge(first.Type, nameof(EventTypeRegistration.Visibility), first.Visibility, second.Visibility),
        DeclaredFacet<EventStoreName>.Merge(first.Type, nameof(EventTypeRegistration.EventStore), first.EventStore, second.EventStore),
        DeclaredFacet<EventSequenceId>.Merge(first.Type, nameof(EventTypeRegistration.Sequence), first.Sequence, second.Sequence));

    EventType ResolveIdentity(Type type, HashSet<Type> visiting)
    {
        if (!visiting.Add(type))
        {
            throw new InvalidExplicitArtifact(type, "event type", "Historical generation references form a cycle.");
        }

        var declaration = _declarations.TryGetValue(type, out var registered) ? registered : FromMarkers(type);
        var id = declaration.Id;
        if (type.GetCustomAttribute<EventTypeGenerationForAttribute>() is { } historical)
        {
            var referencedType = historical.EventTypeClrType;
            if (!_declarations.ContainsKey(referencedType) && !referencedType.IsDefined(typeof(EventTypeAttribute)))
            {
                throw new EventTypeGenerationReferencesNonEventType(type, referencedType);
            }

            var referencedId = ResolveIdentity(referencedType, visiting).Id;
            id = DeclaredFacet<EventTypeId>.Merge(
                type,
                nameof(EventTypeRegistration.Id),
                id,
                new(referencedId, $"{nameof(EventTypeGenerationForAttribute)}<{referencedType.FullName}>"));
        }
        visiting.Remove(type);

        return new(id?.Value ?? (EventTypeId)type.Name, declaration.Generation?.Value ?? EventTypeGeneration.First);
    }
}
