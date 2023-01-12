// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Schemas.MongoDB;

/// <summary>
/// Exception that gets thrown when an event type is missing from the schema store.
/// </summary>
public class MissingEventSchemaForEventType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingEventSchemaForEventType"/>.
    /// </summary>
    /// <param name="type">The <see cref="EventTypeId"/> missing.</param>
    /// <param name="generation">The <see cref="EventGeneration"/> that is missing.</param>
    public MissingEventSchemaForEventType(EventTypeId type, EventGeneration generation) : base($"Event type '{type}' with generation '{generation}' is missing from the event schema store")
    {
    }
}
