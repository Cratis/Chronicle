// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_a_target_is_named_like_a_function : Specification
{
    PropertyPath _result;

    void Because() => _result = ProjectionPropertyPaths.Resolve("Week", new CamelCaseNamingPolicy(), ProjectionPathRole.Target);

    [Fact] void should_name_the_member() => _result.Path.ShouldEqual("week");
    [Fact] void should_resolve_a_member() => _result.LastSegment.ShouldBeOfExactType<PropertyName>();
}
