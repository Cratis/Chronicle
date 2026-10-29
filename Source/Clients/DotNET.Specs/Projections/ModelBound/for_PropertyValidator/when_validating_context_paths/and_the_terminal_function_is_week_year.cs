// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator.when_validating_context_paths;

public class and_the_terminal_function_is_week_year : Specification
{
    string _bare;
    string _explicit;

    void Because()
    {
        _bare = PropertyValidator.ValidateContextPropertyPath<EventContext>("Occurred.WeekYear");
        _explicit = PropertyValidator.ValidateContextPropertyPath<EventContext>("Occurred.WeekYear()");
    }

    [Fact] void should_canonicalize_the_bare_function() => _bare.ShouldEqual("Occurred.WeekYear()");
    [Fact] void should_preserve_the_explicit_function() => _explicit.ShouldEqual("Occurred.WeekYear()");
}
