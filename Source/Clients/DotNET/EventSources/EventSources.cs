// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventSources;

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Represents an implementation of <see cref="IEventSources"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> the event sources belong to. Can be null for discovery only, in which case <see cref="Register"/> is not possible.</param>
/// <param name="clientArtifacts">The <see cref="IClientArtifactsProvider"/> providing the event source types.</param>
public class EventSources(IEventStore? eventStore, IClientArtifactsProvider clientArtifacts) : IEventSources
{
    FrozenDictionary<Type, EventSourceDefinition> _byType = FrozenDictionary<Type, EventSourceDefinition>.Empty;
    FrozenDictionary<string, EventSourceDefinition> _byName = FrozenDictionary<string, EventSourceDefinition>.Empty;

    /// <inheritdoc/>
    public IReadOnlyList<EventSourceDefinition> All => [.. _byType.Values];

    /// <inheritdoc/>
    public Task Discover()
    {
        var definitions = clientArtifacts.EventSources.Select(Describe).ToArray();

        var duplicate = definitions
            .GroupBy(_ => _.Name, StringComparer.Ordinal)
            .FirstOrDefault(_ => _.Count() > 1);
        if (duplicate is not null)
        {
            throw new DuplicateEventSourceName(duplicate.Key, duplicate.Select(_ => _.ClrType));
        }

        _byType = definitions.ToFrozenDictionary(_ => _.ClrType);
        _byName = definitions.ToFrozenDictionary(_ => _.Name, StringComparer.Ordinal);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        if (_byType.Count == 0)
        {
            return;
        }

        if (eventStore is null)
        {
            throw new InvalidOperationException("Event sources can only be registered when they belong to an event store.");
        }

        var servicesAccessor = (IChronicleServicesAccessor)eventStore.Connection;
        await servicesAccessor.Services.EventSources.RegisterEventSources(new RegisterEventSourcesRequest
        {
            EventStore = eventStore.Name.Value,
            Sources = [.. All.Select(_ => _.ToContract())]
        }).EnsureSuccess();
    }

    /// <inheritdoc/>
    public EventSourceDefinition GetFor(Type type) =>
        _byType.TryGetValue(type, out var definition) ? definition : throw new UnknownEventSource(type.FullName ?? type.Name);

    /// <inheritdoc/>
    public EventSourceDefinition GetFor(string name) =>
        _byName.TryGetValue(name, out var definition) ? definition : throw new UnknownEventSource(name);

    /// <summary>
    /// Describes the definition carried by a type.
    /// </summary>
    /// <param name="type">The type carrying the definition.</param>
    /// <returns>The <see cref="EventSourceDefinition"/>.</returns>
    /// <exception cref="DuplicateEventStreamName">The type declares the same stream twice.</exception>
    internal static EventSourceDefinition Describe(Type type)
    {
        var attribute = type.GetCustomAttribute<EventSourceAttribute>();
        var name = attribute?.Name ?? DefaultNameFor(type);
        var streams = type.GetCustomAttributes<EventStreamAttribute>()
            .Select(_ => new EventStream(_.Name, _.Description, _.Concurrency))
            .ToArray();

        var duplicateStream = streams.GroupBy(_ => _.Name, StringComparer.Ordinal).FirstOrDefault(_ => _.Count() > 1);
        if (duplicateStream is not null)
        {
            throw new DuplicateEventStreamName(type, duplicateStream.Key);
        }

        return new EventSourceDefinition(type, name, attribute?.Description ?? string.Empty, attribute?.Concurrency ?? ConcurrencyDimensions.None, streams);
    }

    static string DefaultNameFor(Type type) =>
        type.Name.Length > "EventSource".Length && type.Name.EndsWith("EventSource", StringComparison.Ordinal)
            ? type.Name[..^"EventSource".Length]
            : type.Name;
}
