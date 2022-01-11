// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias SDK;

using Cratis.Events.Schemas;
using Cratis.Schemas;

namespace Cratis.Extensions.Dolittle.Schemas
{
    public class Schemas : SDK::Cratis.Events.Schemas.ISchemas
    {
        readonly IEnumerable<SDK::Cratis.Events.Schemas.EventSchemaDefinition> _definitions;
        readonly SDK::Cratis.Events.IEventTypes _eventTypes;
        readonly ISchemaStore _schemaStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="Schemas"/> class.
        /// </summary>
        /// <param name="eventTypes"><see cref="SDK::Cratis.Events.IEventTypes"/> to use.</param>
        /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas..</param>
        /// <param name="schemaStore">The underlying <see cref="ISchemaStore"/>.</param>
        public Schemas(
            SDK::Cratis.Events.IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator,
            ISchemaStore schemaStore)
        {
            _eventTypes = eventTypes;
            _definitions = eventTypes.All.Select(_ =>
            {
                var type = _eventTypes.GetClrTypeFor(_.Id)!;
                return new SDK::Cratis.Events.Schemas.EventSchemaDefinition(
                    _,
                    type.Name,
                    schemaGenerator.Generate(type));
            });

            _schemaStore = schemaStore;
        }

        /// <inheritdoc/>
        public void RegisterAll()
        {
            foreach (var schemaDefinition in _definitions)
            {
                _schemaStore.Register(
                    schemaDefinition.Type,
                    schemaDefinition.FriendlyName,
                    schemaDefinition.Schema);
            }
        }
    }
}
