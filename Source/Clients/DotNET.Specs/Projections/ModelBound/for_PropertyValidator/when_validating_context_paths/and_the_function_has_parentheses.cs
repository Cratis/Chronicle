// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator.when_validating_context_paths;

public class and_the_function_has_parentheses : Specification
{
    string _result;

    void Because() => _result = PropertyValidator.ValidateContextPropertyPath<EventContext>("Occurred.Week()");

    [Fact] void should_preserve_the_canonical_function() => _result.ShouldEqual("Occurred.Week()");
}
