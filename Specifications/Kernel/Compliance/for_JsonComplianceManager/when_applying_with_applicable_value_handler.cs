// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Cratis.Compliance.for_JsonComplianceManager
{
    public class when_applying_with_applicable_value_handler : given.a_value_handler_and_a_type_with_one_property
    {
        const string identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
        const string changed_value = "FortyTwo";
        JObject result;
        JToken property_value;

        void Establish()
        {
            property_value = JToken.FromObject(changed_value);
            value_handler.Setup(_ => _.Apply(identifier, IsAny<JToken>())).Returns(Task.FromResult(property_value));
        }

        async Task Because() => result = await manager.Apply(schema, identifier, input);

        [Fact] void should_return_instance_with_altered_property() => result.Properties().First().Value.ShouldEqual(property_value);
    }
}
