// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ChildKeyComparison;

public class when_comparing_values_of_the_same_type_that_differ : Specification
{
    bool _result;

    void Because() => _result = ChildKeyComparison.IsSameKey("a", "b");

    [Fact] void should_not_be_the_same_key() => _result.ShouldBeFalse();
}
