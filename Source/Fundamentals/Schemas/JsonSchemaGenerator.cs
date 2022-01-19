// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Aksio.Cratis.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Aksio.Cratis.Schemas
{
    /// <summary>
    /// Represents an implementation of <see cref="IJsonSchemaGenerator"/>.
    /// </summary>
    [Singleton]
    public class JsonSchemaGenerator : IJsonSchemaGenerator
    {
        /// <summary>
        /// The key of the compliance extension data.
        /// </summary>
        public const string ComplianceKey = "compliance";

        readonly NJsonSchemaGenerator _generator;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSchemaGenerator"/> class.
        /// </summary>
        /// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata.</param>
        public JsonSchemaGenerator(IComplianceMetadataResolver metadataResolver)
        {
            var settings = new JsonSchemaGeneratorSettings
            {
                SerializerSettings = new()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    DateParseHandling = DateParseHandling.DateTimeOffset
                },
                ReflectionService = new ReflectionService(),
            };
            settings.SchemaProcessors.Add(new ComplianceMetadataSchemaProcessor(metadataResolver));
            settings.SchemaProcessors.Add(new TypeFormatSchemaProcessor());
            _generator = new NJsonSchemaGenerator(settings);
        }

        /// <inheritdoc/>
        public JsonSchema Generate(Type type) => _generator.Generate(type);
    }
}
