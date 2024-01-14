// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Aksio.Cratis.Kernel.Compliance.for_JsonComplianceManager;

public class when_applying_without_any_applicable_value_handlers : given.no_value_handlers_and_a_type_with_one_property
{
    JsonObject result;

    async Task Because() => result = await manager.Apply(string.Empty, string.Empty, schema, string.Empty, input);

    [Fact] void should_be_same_instance() => result.GetHashCode().ShouldEqual(input.GetHashCode());
    [Fact] void should_have_equal_objects() => result.ToString().ShouldEqual(input.ToString());
}
