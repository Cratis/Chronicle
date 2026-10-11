// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_a_source_starts_with_a_parsed_function : Specification
{
    PropertyPath _path;
    PropertyPath _result;

    void Establish() => _path = new PropertyPath("Week");
    void Because() => _result = ProjectionPropertyPaths.Resolve(_path, new CamelCaseNamingPolicy(), ProjectionPathRole.Source);

    [Fact] void should_name_the_first_segment_as_a_member() => _result.Path.ShouldEqual("week");
    [Fact] void should_resolve_a_member_without_a_receiver() => _result.LastSegment.ShouldBeOfExactType<PropertyName>();
}
