// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Aksio.Cratis.Compliance.for_JsonComplianceManager
{
    public class when_applying_without_any_applicable_value_handlers : given.no_value_handlers_and_a_type_with_one_property
    {
        JObject result;

        async Task Because() => result = await manager.Apply(schema, string.Empty, input);

        [Fact] void should_be_a_different_instance() => result.GetHashCode().ShouldNotBeSame(input.GetHashCode());
        [Fact] void should_have_equal_objects() => result.ToString().ShouldEqual(input.ToString());
    }
}
