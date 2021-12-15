// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Events.Projections.Definitions;
using Cratis.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents an implementation of <see cref="IJsonProjectionSerializer"/>.
    /// </summary>
    public class JsonProjectionSerializer : IJsonProjectionSerializer
    {
        readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonProjectionSerializer"/>.
        /// </summary>
        public JsonProjectionSerializer()
        {
            _serializer = new JsonSerializer
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            _serializer.Converters.Add(new FromDefinitionsConverter());
            _serializer.Converters.Add(new PropertyPathJsonConverter());
            _serializer.Converters.Add(new PropertyExpressionDictionaryJsonConverter());
            _serializer.Converters.Add(new PropertyPathChildrenDefinitionDictionaryJsonConverter());
            _serializer.Converters.Add(new ConceptAsJsonConverter());
            _serializer.Converters.Add(new ConceptAsDictionaryJsonConverter());
        }

        /// <inheritdoc/>
        public string Serialize(ProjectionDefinition definition)
        {
            var writer = new StringWriter();
            _serializer.Serialize(writer, definition);
            return writer.ToString();
        }

        /// <inheritdoc/>
        public ProjectionDefinition Deserialize(string json) => _serializer.Deserialize<ProjectionDefinition>(new JsonTextReader(new StringReader(json)))!;
    }
}
