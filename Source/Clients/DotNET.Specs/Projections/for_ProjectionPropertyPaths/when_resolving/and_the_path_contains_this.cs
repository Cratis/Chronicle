// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_the_path_contains_this : Specification
{
    PropertyPath _result;

    void Because() => _result = ProjectionPropertyPaths.Resolve("[Items].$this", new CamelCaseNamingPolicy(), ProjectionPathRole.Target);

    [Fact] void should_preserve_the_this_accessor() => _result.Path.ShouldEqual("[items].$this");
    [Fact] void should_keep_the_accessor_segment_type() => _result.LastSegment.ShouldBeOfExactType<ThisAccessor>();
}
