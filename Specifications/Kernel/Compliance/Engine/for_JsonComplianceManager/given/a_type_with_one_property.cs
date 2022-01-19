// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Aksio.Cratis.Compliance.for_JsonComplianceManager.given
{
    public class a_type_with_one_property : Specification
    {
        protected JsonSchema schema;
        protected JObject input;

        void Establish()
        {
            var instance = new { Something = 42 };
            var settings = new JsonSchemaGeneratorSettings();
            var generator = new JsonSchemaGenerator(settings);
            schema = generator.Generate(instance.GetType());
            input = JObject.FromObject(instance);
        }
    }
}
