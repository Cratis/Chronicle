// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator.when_validating_context_paths;

public class and_the_terminal_segment_is_a_derived_function : Specification
{
    string _result;

    void Because() => _result = PropertyValidator.ValidateContextPropertyPath<EventContext>("occurred.Week");

    [Fact] void should_canonicalize_the_receiver_and_function() => _result.ShouldEqual("Occurred.Week()");
}
