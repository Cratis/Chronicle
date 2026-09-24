// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Properties.for_ExpressionExtensions.when_getting_a_validated_property_path;

public class and_accessor_is_a_week_derived_property : given.an_event
{
    bool _result;
    PropertyPath _path = PropertyPath.NotSet;

    void Because()
    {
        Expression<Func<SomeEvent, object>> accessor = e => e.Occurred.Week();
        _result = accessor.TryGetPropertyPath(out _path);
    }

    [Fact] void should_succeed() => _result.ShouldBeTrue();
    [Fact] void should_extract_the_property_path_and_render_it_as_a_property_without_parens() => _path.Path.ShouldEqual("Occurred.Week");
}
