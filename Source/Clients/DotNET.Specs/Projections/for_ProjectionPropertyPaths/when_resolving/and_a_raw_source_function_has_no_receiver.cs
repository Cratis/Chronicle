// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_a_raw_source_function_has_no_receiver : Specification
{
    PropertyPath _result;

    void Because() => _result = ProjectionPropertyPaths.Resolve("Week()", new CamelCaseNamingPolicy(), ProjectionPathRole.Source);

    [Fact] void should_name_the_first_segment_as_a_member() => _result.Path.ShouldEqual("week");
    [Fact] void should_not_resolve_a_function() => _result.LastSegment.ShouldBeOfExactType<PropertyName>();
}
