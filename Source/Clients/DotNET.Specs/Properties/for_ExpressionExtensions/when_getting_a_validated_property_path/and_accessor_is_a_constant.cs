// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Properties.for_ExpressionExtensions.when_getting_a_validated_property_path;

public class and_accessor_is_a_constant : given.an_event
{
    bool _result;
    PropertyPath _path = PropertyPath.NotSet;

    void Because()
    {
        Expression<Func<SomeEvent, string>> accessor = _ => "constant";
        _result = accessor.TryGetPropertyPath(out _path);
    }

    [Fact] void should_not_succeed() => _result.ShouldBeFalse();
    [Fact] void should_not_extract_a_property_path() => _path.IsSet.ShouldBeFalse();
}
