// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_setting_into_a_dictionary_declaring_double_values : given.a_typed_dictionary_projection
{
    object? _result;

    async Task Because() => _result = await Project("amounts.$eventContent.slot", "amount");

    [Fact] void should_set_the_declared_value_type() => _result.ShouldBeOfExactType<double>();
}
