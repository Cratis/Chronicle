// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_the_path_is_already_parsed : Specification
{
    PropertyPath _path;
    PropertyPath _result;

    void Establish() => _path = new PropertyPath("[Items].$this.Occurred.Week()");
    void Because() => _result = ProjectionPropertyPaths.Resolve(_path, new CamelCaseNamingPolicy(), ProjectionPathRole.Source);

    [Fact] void should_preserve_array_this_and_function_segments() => _result.Path.ShouldEqual("[items].$this.occurred.Week");
    [Fact] void should_resolve_a_function() => _result.LastSegment.ShouldBeOfExactType<DerivedPropertyFunctionSegment>();
}
