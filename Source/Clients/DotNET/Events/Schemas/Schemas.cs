// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents an implementation of <see cref="ISchemas"/>.
    /// </summary>
    public class Schemas : ISchemas
    {
        JSchemaGenerator? _generator;
        protected readonly IEnumerable<EventSchemaDefinition> _definitions;
        readonly IEventTypes _eventTypes;
        readonly IComplianceMetadataResolver _metadataResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="Schemas"/> class.
        /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/></param>
        /// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata for compliance.</param>
        public Schemas(
            IEventTypes eventTypes,
            IComplianceMetadataResolver metadataResolver)
        {
            _eventTypes = eventTypes;
            _metadataResolver = metadataResolver;
            _definitions = eventTypes.All.Select(_ =>
            {
                var type = _eventTypes.GetClrTypeFor(_.EventTypeId)!;
                return new EventSchemaDefinition(
                    _,
                    type.Name,
                    Generator.Generate(type));
            });
        }

        /// <inheritdoc/>
        public virtual void RegisterAll()
        {
        }

        JSchemaGenerator Generator
        {
            get
            {
                if (_generator == null)
                {
                    _generator = new JSchemaGenerator
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    };

                    _generator.GenerationProviders.Add(new CompositeSchemaGenerationProvider(_metadataResolver));
                    _generator.GenerationProviders.Add(new StringEnumGenerationProvider());
                }

                return _generator;
            }
        }
    }
}
