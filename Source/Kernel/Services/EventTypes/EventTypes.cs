// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Services.Events;

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
        foreach (var eventType in payload.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema.ToJsonString());
            await _storage.GetEventStore((string)microserviceId).EventTypes.Register(eventType.Type, eventType.FriendlyName, schema);
        }
    }
}
