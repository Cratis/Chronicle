// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_normalizing_state_values;

public class and_the_value_has_a_non_boolean_specified_member : Specification
{
    NotAWrapper _value;
    object? _result;

    void Establish() => _value = new NotAWrapper("yes", "inner");

    void Because() => _result = ProjectionReadModelProcessor.NormalizeStateValue(_value);

    [Fact] void should_return_the_value_itself() => _result.ShouldEqual(_value);

    record NotAWrapper(string IsSpecified, string Value);
}
