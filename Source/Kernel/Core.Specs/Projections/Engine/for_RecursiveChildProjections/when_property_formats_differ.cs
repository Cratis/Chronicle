// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_property_formats_differ : given.two_projection_types
{
    void Establish() => _childSchema.Properties["id"].Format = "date-time";

    void Because() => _result = RecursiveChildProjections.Of(_parent).ToArray();

    [Fact] void should_not_treat_the_child_as_recursive() => _result.ShouldBeEmpty();
}
