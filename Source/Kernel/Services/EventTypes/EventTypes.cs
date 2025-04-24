// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Storage;
using NJsonSchema;

namespace Cratis.Chronicle.Services.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypes"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
internal class EventTypes(IStorage storage) : IEventTypes
{
    /// <inheritdoc/>
    public async Task Register(RegisterEventTypesRequest request)
    {
        foreach (var eventType in request.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema);
            await storage.GetEventStore(request.EventStore).EventTypes.Register(eventType.Type.ToChronicle(), schema);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Events.EventType>> GetAll(GetAllEventTypesRequest request)
    {
        var eventTypes = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();
        return eventTypes.Select(_ => _.Type.ToContract());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeRegistration>> GetAllRegistrations(GetAllEventTypesRequest request)
    {
        var eventTypes = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();
        return eventTypes.Select(_ => new EventTypeRegistration
        {
            Type = _.Type.ToContract(),
            Schema = _.Schema.ToJson()
        });
    }
}
