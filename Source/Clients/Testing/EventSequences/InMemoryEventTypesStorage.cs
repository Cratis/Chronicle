// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Reactive.Subjects;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using KernelConcepts::Cratis.Chronicle.Concepts.Events;
using ClientEventTypeId = Cratis.Chronicle.Events.EventTypeId;
using ClientEventTypes = Cratis.Chronicle.Events.IEventTypes;
using KernelEventTypes = KernelConcepts::Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventTypesStorage"/> for testing that answers with
/// the schema the client generated for an event type.
/// </summary>
/// <remarks>
/// <para>
/// The schema is not incidental — the kernel gates compliance on it. A <c>[PII]</c> value is only encrypted
/// when the event's schema says it carries compliance metadata, so answering every lookup with an empty
/// <see cref="JsonSchema"/> (as this used to) made every marked value pass through in plaintext no matter
/// how the compliance manager was wired.
/// </para>
/// <para>
/// The schema is derived rather than stored, because nothing in the harness registers one: it is read from
/// the client's own <see cref="ClientEventTypes"/>, which is the same registry the appending side serializes
/// through. An event type the client does not know still answers with an empty schema, which keeps the
/// <see cref="Json.ExpandoObjectConverter"/> on its generic unknown-type conversion for anything appended
/// outside the registry.
/// </para>
/// <para>
/// Migrations are still not applied, and nothing is registered — <c>Register</c> stays a no-op, because the
/// registry the schemas come from is the client's own and is already populated by discovery.
/// </para>
/// </remarks>
/// <param name="eventTypes">The client <see cref="ClientEventTypes"/> holding the generated schemas.</param>
internal sealed class InMemoryEventTypesStorage(ClientEventTypes eventTypes) : IEventTypesStorage
{
    /// <inheritdoc/>
    public Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code) =>
        Task.FromResult(false);

    /// <inheritdoc/>
    public Task<bool> Register(EventTypeDefinition definition) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetLatestForAllEventTypes() =>
        Task.FromResult(Enumerable.Empty<KernelEventTypes::EventTypeSchema>());

    /// <inheritdoc/>
    public ISubject<IEnumerable<KernelEventTypes::EventTypeSchema>> ObserveLatestForAllEventTypes() =>
        new Subject<IEnumerable<KernelEventTypes::EventTypeSchema>>();

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions() =>
        Task.FromResult(Enumerable.Empty<EventTypeDefinition>());

    /// <inheritdoc/>
    public Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId) =>
        Task.FromResult(new EventTypeDefinition(
            eventTypeId,
            EventTypeOwner.Client,
            false,
            [new EventTypeGenerationDefinition(EventTypeGeneration.First, SchemaFor(eventTypeId))],
            []));

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType) =>
        Task.FromResult(Enumerable.Empty<KernelEventTypes::EventTypeSchema>());

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds) =>
        Task.FromResult<IEnumerable<KernelEventTypes::EventTypeSchema>>(
            [.. eventTypeIds.Select(eventTypeId => SchemaFor(new EventType(eventTypeId, EventTypeGeneration.First)))]);

    /// <inheritdoc/>
    /// <remarks>
    /// This is what the read path resolves compliance from, so leaving it empty released nothing — an event
    /// encrypted on the way in came back out as its ciphertext.
    /// </remarks>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes) =>
        Task.FromResult<IEnumerable<KernelEventTypes::EventTypeSchema>>([.. eventTypes.Select(SchemaFor)]);

    /// <inheritdoc/>
    public Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        Task.FromResult(true);

    /// <inheritdoc/>
    public Task<KernelEventTypes::EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        Task.FromResult(SchemaFor(new EventType(type, generation ?? EventTypeGeneration.First)));

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId)
    {
    }

    KernelEventTypes::EventTypeSchema SchemaFor(EventType eventType) =>
        new(eventType, EventTypeOwner.Client, EventTypeSource.Code, SchemaFor(eventType.Id));

    JsonSchema SchemaFor(EventTypeId eventTypeId)
    {
        // An event type the client registry does not know keeps the empty schema, which is what leaves the
        // ExpandoObjectConverter on its generic unknown-type conversion so content appended outside the
        // registry survives verbatim.
        var clientEventTypeId = new ClientEventTypeId(eventTypeId.Value);
        return eventTypes.HasFor(clientEventTypeId)
            ? eventTypes.GetSchemaFor(clientEventTypeId)
            : new JsonSchema();
    }
}
