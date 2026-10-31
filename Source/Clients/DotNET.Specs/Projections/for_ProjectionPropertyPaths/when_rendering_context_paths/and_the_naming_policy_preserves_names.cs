// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_rendering_context_paths;

public class and_the_naming_policy_preserves_names : Specification
{
    string[] _paths;
    string[] _results;

    void Establish() => _paths = [
        PropertyValidator.ValidateContextPropertyPath<EventContext>("Occurred.Week"),
        PropertyValidator.ValidateContextPropertyPath<EventContext>("Occurred.WeekYear()"),
        PropertyValidator.ValidateContextPropertyPath<EventContext>("Occurred.Year")
    ];

    void Because() => _results = _paths.Select(path => ProjectionPropertyPaths.RenderContextPath(path, new DefaultNamingPolicy())).ToArray();

    [Fact] void should_preserve_the_week_call() => _results[0].ShouldEqual("Occurred.Week()");
    [Fact] void should_preserve_the_week_year_call() => _results[1].ShouldEqual("Occurred.WeekYear()");
    [Fact] void should_preserve_plain_members() => _results[2].ShouldEqual("Occurred.Year");
}
