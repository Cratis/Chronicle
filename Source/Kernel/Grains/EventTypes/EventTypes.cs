// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Persistence.EventTypes;
using Aksio.Cratis.Schemas;
using Aksio.DependencyInversion;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Grains.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
public class EventTypes : Grain, IEventTypes
{
    readonly ProviderFor<IEventTypesStorage> _underlyingSchemaStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypes"/> class.
    /// </summary>
    /// <param name="underlyingSchemaStore"><see cref="IEventTypesStorage"/> underlying schema store.</param>
    public EventTypes(ProviderFor<IEventTypesStorage> underlyingSchemaStore)
    {
        _underlyingSchemaStore = underlyingSchemaStore;
    }

    /// <inheritdoc/>
    public async Task Register(EventType type, string friendlyName, string schema)
    {
        var jsonSchema = await JsonSchema.FromJsonAsync(schema);
        jsonSchema.EnsureComplianceMetadata();
        await _underlyingSchemaStore().Register(type, friendlyName, jsonSchema);
    }
}
