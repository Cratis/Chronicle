// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_releasing_without_any_applicable_value_handlers : given.no_value_handlers_and_a_type_with_one_property
{
    JsonObject _result;

    async Task Because() => _result = await manager.Release(string.Empty, string.Empty, _schema, string.Empty, _input);

    [Fact] void should_be_same_instance() => _result.GetHashCode().ShouldEqual(_input.GetHashCode());
    [Fact] void should_have_equal_objects() => _result.ToString().ShouldEqual(_input.ToString());
}
