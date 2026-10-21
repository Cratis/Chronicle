// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_the_path_contains_an_array : Specification
{
    PropertyPath _result;

    void Because() => _result = ProjectionPropertyPaths.Resolve("[Items].Meta.Topic", new CamelCaseNamingPolicy(), ProjectionPathRole.Target);

    [Fact] void should_preserve_array_markers_and_name_each_segment() => _result.Path.ShouldEqual("[items].meta.topic");
}
