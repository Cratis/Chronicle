// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_a_raw_source_member_has_a_function_name_and_a_receiver : Specification
{
    PropertyPath _result;

    void Because() => _result = ProjectionPropertyPaths.Resolve("Brief.Week", new CamelCaseNamingPolicy(), ProjectionPathRole.Source);

    [Fact] void should_name_both_members() => _result.Path.ShouldEqual("brief.week");
    [Fact] void should_not_resolve_a_function() => _result.LastSegment.ShouldBeOfExactType<PropertyName>();
}
