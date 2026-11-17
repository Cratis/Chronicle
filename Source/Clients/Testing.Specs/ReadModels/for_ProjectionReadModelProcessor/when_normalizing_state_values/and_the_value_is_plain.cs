// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_normalizing_state_values;

public class and_the_value_is_plain : Specification
{
    object? _result;

    void Because() => _result = ProjectionReadModelProcessor.NormalizeStateValue("plain");

    [Fact] void should_return_the_value_itself() => _result.ShouldEqual("plain");
}
