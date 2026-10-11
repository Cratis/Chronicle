// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_a_target_contains_a_parsed_function : Specification
{
    PropertyPath _path;
    PropertyPath _result;

    void Establish() => _path = new PropertyPath("Meta.Week()");
    void Because() => _result = ProjectionPropertyPaths.Resolve(_path, new CamelCaseNamingPolicy(), ProjectionPathRole.Target);

    [Fact] void should_name_the_function_as_a_member() => _result.Path.ShouldEqual("meta.week");
    [Fact] void should_only_contain_member_segments() => _result.Segments.All(segment => segment is PropertyName).ShouldBeTrue();
}
