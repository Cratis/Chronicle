// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_titles_match_but_property_names_differ : given.two_projection_types
{
    void Establish()
    {
        var identifier = _childSchema.Properties["id"];
        _childSchema.Properties.Remove("id");
        _childSchema.Properties.Add("name", identifier);
    }

    void Because() => _result = RecursiveChildProjections.Of(_parent).ToArray();

    [Fact] void should_not_treat_the_child_as_recursive() => _result.ShouldBeEmpty();
}
