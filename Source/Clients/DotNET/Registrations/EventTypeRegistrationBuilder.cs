// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Collects supplied event facets without introducing defaults.
/// </summary>
/// <param name="type">The explicitly nominated type.</param>
internal sealed class EventTypeRegistrationBuilder(Type type) : IEventTypeRegistrationBuilder
{
    EventTypeRegistration _registration = new(type);

    /// <inheritdoc/>
    public IEventTypeRegistrationBuilder WithId(EventTypeId id)
    {
        _registration = _registration with { Id = Merge(nameof(WithId), _registration.Id, id) };
        return this;
    }

    /// <inheritdoc/>
    public IEventTypeRegistrationBuilder Generation(EventTypeGeneration generation)
    {
        _registration = _registration with { Generation = Merge(nameof(Generation), _registration.Generation, generation) };
        return this;
    }

    /// <inheritdoc/>
    public IEventTypeRegistrationBuilder Public() => Visibility(EventTypeVisibility.Public);

    /// <inheritdoc/>
    public IEventTypeRegistrationBuilder Private() => Visibility(EventTypeVisibility.Private);

    /// <inheritdoc/>
    public IEventTypeRegistrationBuilder ForEventStore(EventStoreName eventStore)
    {
        _registration = _registration with { EventStore = Merge(nameof(ForEventStore), _registration.EventStore, eventStore) };
        return this;
    }

    /// <inheritdoc/>
    public IEventTypeRegistrationBuilder PublishTo(EventSequenceId sequence)
    {
        _registration = _registration with { Sequence = Merge(nameof(PublishTo), _registration.Sequence, sequence) };
        return this;
    }

    /// <summary>
    /// Captures the current immutable declaration with its call ordinal.
    /// </summary>
    /// <param name="ordinal">The options registration's ordinal.</param>
    /// <returns>The immutable declaration.</returns>
    internal EventTypeRegistration Build(int ordinal) => _registration with
    {
        Id = WithSource(_registration.Id, ordinal),
        Generation = WithSource(_registration.Generation, ordinal),
        Visibility = WithSource(_registration.Visibility, ordinal),
        EventStore = WithSource(_registration.EventStore, ordinal),
        Sequence = WithSource(_registration.Sequence, ordinal)
    };

    static DeclaredFacet<T>? WithSource<T>(DeclaredFacet<T>? facet, int ordinal) =>
        facet is null ? null : facet with { Source = $"explicit registration #{ordinal}" };

    EventTypeRegistrationBuilder Visibility(EventTypeVisibility visibility)
    {
        _registration = _registration with { Visibility = Merge(nameof(Visibility), _registration.Visibility, visibility) };
        return this;
    }

    DeclaredFacet<T>? Merge<T>(string facet, DeclaredFacet<T>? previous, T value) =>
        DeclaredFacet<T>.Merge(type, facet, previous, new(value, "explicit callback"));
}
