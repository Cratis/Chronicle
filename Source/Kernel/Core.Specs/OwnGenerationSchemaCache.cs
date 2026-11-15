// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle;

/// <summary>
/// Keeps single-generation unit fixtures independent of event type storage.
/// Inherited-protection specifications use the real cache instead.
/// </summary>
public class OwnGenerationSchemaCache : IEventTypeSchemaCache
{
    /// <inheritdoc/>
    public ValueTask<JsonSchema> GetProtectionSchemaFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema ownSchema) => new(ownSchema);

    /// <inheritdoc/>
    public string GetSchemaJsonFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Invalidate(EventStoreName eventStore, EventTypeId eventTypeId)
    {
    }
}
