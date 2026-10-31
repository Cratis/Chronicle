// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_the_path_contains_a_derived_function : Specification
{
    PropertyPath _result;

    void Because() => _result = ProjectionPropertyPaths.Resolve("Occurred.Week()", new CamelCaseNamingPolicy(), ProjectionPathRole.Source);

    [Fact] void should_name_members_without_changing_the_function() => _result.Path.ShouldEqual("occurred.Week");
    [Fact] void should_resolve_a_function() => _result.LastSegment.ShouldBeOfExactType<DerivedPropertyFunctionSegment>();
}
