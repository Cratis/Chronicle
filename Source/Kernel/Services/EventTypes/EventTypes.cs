// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Kernel.Contracts.Events;
using Cratis.Kernel.Storage;
using NJsonSchema;

namespace Cratis.Kernel.Services.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
public class EventTypes : IEventTypes
{
    readonly IStorage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypes"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
    public EventTypes(IStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc/>
    public async Task Register(RegisterEventTypesRequest request)
    {
        foreach (var eventType in request.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema);
            await _storage.GetEventStore(request.EventStoreName).EventTypes.Register(eventType.Type.ToKernel(), eventType.FriendlyName, schema);
        }
    }
}
