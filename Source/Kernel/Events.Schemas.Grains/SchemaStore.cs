// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;
using Orleans;

namespace Cratis.Events.Schemas.Grains
{
    /// <summary>
    /// Represents an implementation of <see cref="ISchemaStore"/>.
    /// </summary>
    public class SchemaStore : Grain, ISchemaStore
    {
        readonly Schemas.ISchemaStore _underlyingSchemaStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaStore"/> class.
        /// </summary>
        /// <param name="underlyingSchemaStore"><see cref="Schemas.ISchemaStore"/> underlying schema store.</param>
        public SchemaStore(Schemas.ISchemaStore underlyingSchemaStore)
        {
            _underlyingSchemaStore = underlyingSchemaStore;
        }

        /// <inheritdoc/>
        public async Task Register(EventType type, string friendlyName, string schema)
        {
            var jsonSchema = await JsonSchema.FromJsonAsync(schema);
            await _underlyingSchemaStore.Register(type, friendlyName, jsonSchema);
        }
    }
}
