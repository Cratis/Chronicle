// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Schemas;
using Cratis.Types;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Cratis.Compliance
{
    /// <summary>
    /// Represents an implementation of <see cref="IJsonComplianceManager"/>.
    /// </summary>
    public class JsonComplianceManager : IJsonComplianceManager
    {
        readonly Dictionary<ComplianceMetadataType, IJsonCompliancePropertyValueHandler> _propertyValueHandlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonComplianceManager"/> class.
        /// </summary>
        /// <param name="propertyValueHandlers">Instances of <see cref="IJsonCompliancePropertyValueHandler"/>.</param>
        public JsonComplianceManager(IInstancesOf<IJsonCompliancePropertyValueHandler> propertyValueHandlers)
        {
            _propertyValueHandlers = propertyValueHandlers.ToDictionary(_ => _.Type, _ => _);
        }

        /// <inheritdoc/>
        public async Task<JObject> Apply(JsonSchema schema, string identifier, JObject json)
        {
            var result = (json.DeepClone() as JObject)!;
            await HandleActionFor(schema, identifier, result, async (h, id, token) => await h.Apply(id, token));
            return result;
        }

        /// <inheritdoc/>
        public async Task<JObject> Release(JsonSchema schema, string identifier, JObject json)
        {
            var result = (json.DeepClone() as JObject)!;
            await HandleActionFor(schema, identifier, result, async (h, id, token) => await h.Release(id, token));
            return result;
        }

        async Task HandleActionFor(JsonSchema schema, string identifier, JContainer json, Func<IJsonCompliancePropertyValueHandler, string, JToken, Task<JToken>> action)
        {
            var complianceMetadataForContainer = GetMetadata(schema);
            foreach (var property in json.Children().Where(_ => _.Type == JTokenType.Property).Cast<JProperty>())
            {
                if (schema.Properties != default)
                {
                    var propertySchema = schema.Properties.Single(_ => _.Key == property.Name).Value;
                    foreach (var metadata in GetMetadata(propertySchema).Concat(complianceMetadataForContainer).DistinctBy(_ => _.type))
                    {
                        if (_propertyValueHandlers.ContainsKey(metadata.type))
                        {
                            property.Value = await action(_propertyValueHandlers[metadata.type], identifier, property.Value);
                        }
                    }

                    await HandleActionFor(propertySchema, identifier, property, action);
                }
            }
        }

        IEnumerable<ComplianceSchemaMetadata> GetMetadata(JsonSchema schema)
        {
            if ((schema.ExtensionData?.ContainsKey(JsonSchemaGenerator.ComplianceKey) ?? false) &&
                schema.ExtensionData[JsonSchemaGenerator.ComplianceKey] is IEnumerable<ComplianceSchemaMetadata> allMetadata)
            {
                return allMetadata;
            }
            return Array.Empty<ComplianceSchemaMetadata>();
        }
    }
}
