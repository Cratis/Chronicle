// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Schemas;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents an implementation of <see cref="ISchemas"/>.
    /// </summary>
    public class Schemas : ISchemas
    {
        protected readonly IEnumerable<EventSchemaDefinition> _definitions;
        readonly IEventTypes _eventTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="Schemas"/> class.
        /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/></param>
        /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating schemas for event types.</param>
        public Schemas(
            IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator)
        {
            _eventTypes = eventTypes;
            _definitions = eventTypes.All.Select(_ =>
            {
                var type = _eventTypes.GetClrTypeFor(_.EventTypeId)!;
                return new EventSchemaDefinition(
                    _,
                    type.Name,
                    schemaGenerator.Generate(type));
            });
        }

        /// <inheritdoc/>
        public virtual void RegisterAll()
        {
        }
    }
}
