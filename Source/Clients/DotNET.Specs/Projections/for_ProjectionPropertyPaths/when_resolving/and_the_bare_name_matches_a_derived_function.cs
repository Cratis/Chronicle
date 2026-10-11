// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_the_bare_name_matches_a_derived_function : Specification
{
    PropertyPath _result;

    void Because() => _result = ProjectionPropertyPaths.Resolve("Week", new CamelCaseNamingPolicy(), ProjectionPathRole.Source);

    [Fact] void should_name_the_property() => _result.Path.ShouldEqual("week");
    [Fact] void should_resolve_a_property_not_a_function() => _result.LastSegment.ShouldBeOfExactType<PropertyName>();
}
