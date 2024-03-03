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
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypes"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
public class EventTypes(IStorage storage) : IEventTypes
{
    /// <inheritdoc/>
    public async Task Register(RegisterEventTypesRequest request)
    {
        foreach (var eventType in request.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema);
            await storage.GetEventStore(request.EventStoreName).EventTypes.Register(eventType.Type.ToKernel(), eventType.FriendlyName, schema);
        }
    }
}
