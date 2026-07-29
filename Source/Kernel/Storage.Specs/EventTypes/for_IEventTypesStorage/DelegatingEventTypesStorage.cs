// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes.for_IEventTypesStorage;

/// <summary>
/// Implements every member of <see cref="IEventTypesStorage"/> by delegating to another instance, deliberately
/// leaving the batched Register to the interface's default implementation so it is the thing under test.
/// </summary>
/// <param name="inner">The <see cref="IEventTypesStorage"/> to delegate to.</param>
public class DelegatingEventTypesStorage(IEventTypesStorage inner) : IEventTypesStorage
{
    public Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code) =>
        inner.Register(type, schema, owner, source);

    public Task<bool> Register(EventTypeDefinition definition) => inner.Register(definition);

    public Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes() => inner.GetLatestForAllEventTypes();

    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() => inner.ObserveLatestForAllEventTypes();

    public Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions() => inner.GetAllDefinitions();

    public Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId) => inner.GetDefinition(eventTypeId);

    public Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType) => inner.GetAllGenerationsForEventType(eventType);

    public Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default) => inner.HasFor(type, generation);

    public Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default) => inner.GetFor(type, generation);

    public Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds) => inner.GetFor(eventTypeIds);

    public Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes) => inner.GetFor(eventTypes);

    public void Invalidate(EventTypeId eventTypeId) => inner.Invalidate(eventTypeId);
}
