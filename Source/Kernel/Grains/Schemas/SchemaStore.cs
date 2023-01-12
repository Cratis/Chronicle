// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Shared.Events;
using NJsonSchema;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Schemas;

/// <summary>
/// Represents an implementation of <see cref="ISchemaStore"/>.
/// </summary>
public class SchemaStore : Grain, ISchemaStore
{
    readonly ProviderFor<Shared.Schemas.ISchemaStore> _underlyingSchemaStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaStore"/> class.
    /// </summary>
    /// <param name="underlyingSchemaStore"><see cref="Shared.Schemas.ISchemaStore"/> underlying schema store.</param>
    public SchemaStore(ProviderFor<Shared.Schemas.ISchemaStore> underlyingSchemaStore)
    {
        _underlyingSchemaStore = underlyingSchemaStore;
    }

    /// <inheritdoc/>
    public async Task Register(EventType type, string friendlyName, string schema)
    {
        var jsonSchema = await JsonSchema.FromJsonAsync(schema);
        jsonSchema.EnsureCorrectMetadata();
        await _underlyingSchemaStore().Register(type, friendlyName, jsonSchema);
    }
}
