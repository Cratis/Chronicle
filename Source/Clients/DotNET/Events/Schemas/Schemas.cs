// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents an implementation of <see cref="ISchemas"/>.
    /// </summary>
    public class Schemas : ISchemas
    {
        static readonly JSchemaGenerator _generator;
        protected readonly IEnumerable<EventSchemaDefinition> _definitions;
        readonly IEventTypes _eventTypes;

        static Schemas()
        {
            _generator = new JSchemaGenerator
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };
            _generator.GenerationProviders.Add(new StringEnumGenerationProvider());
            _generator.GenerationProviders.Add(new ConceptAsGenerationProvider());
            _generator.GenerationProviders.Add(new FormatSchemaGenerationProvider());
            _generator.GenerationProviders.Add(new PIISchemaGenerationProvider());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Schemas"/> class.
        /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/></param>
        public Schemas(IEventTypes eventTypes)
        {
            _eventTypes = eventTypes;
            _definitions = eventTypes.All.Select(_ => new EventSchemaDefinition(_, GenerateFor(_)));
        }

        /// <inheritdoc/>
        public virtual void RegisterAll()
        {
        }

        JSchema GenerateFor(EventType type) => _generator.Generate(_eventTypes.GetClrTypeFor(type.EventTypeId));
    }
}
