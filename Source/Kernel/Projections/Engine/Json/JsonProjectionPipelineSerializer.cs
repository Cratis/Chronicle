// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Aksio.Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents an implementation of <see cref="IJsonProjectionSerializer"/>.
    /// </summary>
    public class JsonProjectionPipelineSerializer : IJsonProjectionPipelineSerializer
    {
        readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonProjectionPipelineSerializer"/>.
        /// </summary>
        public JsonProjectionPipelineSerializer()
        {
            _serializer = new JsonSerializer
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            _serializer.Converters.Add(new ConceptAsJsonConverter());
            _serializer.Converters.Add(new ConceptAsDictionaryJsonConverter());
        }

        /// <inheritdoc/>
        public string Serialize(ProjectionPipelineDefinition definition)
        {
            var writer = new StringWriter();
            _serializer.Serialize(writer, definition);
            return writer.ToString();
        }

        /// <inheritdoc/>
        public ProjectionPipelineDefinition Deserialize(string json) => _serializer.Deserialize<ProjectionPipelineDefinition>(new JsonTextReader(new StringReader(json)))!;
    }
}
