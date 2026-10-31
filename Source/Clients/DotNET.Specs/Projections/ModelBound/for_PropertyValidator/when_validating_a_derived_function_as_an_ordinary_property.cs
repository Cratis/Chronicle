// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator;

public class when_validating_a_derived_function_as_an_ordinary_property : Specification
{
    Exception? _result;

    void Because() => _result = Catch.Exception(() => PropertyValidator.ValidatePropertyExists<EventContext>("Occurred.Week"));

    [Fact] void should_still_require_a_real_property() => _result.ShouldBeOfExactType<InvalidPropertyForType>();
}
