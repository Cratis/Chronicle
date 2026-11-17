// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ChildKeyComparison;

public class when_comparing_two_nulls : Specification
{
    bool _result;

    void Because() => _result = ChildKeyComparison.IsSameKey(null, null);

    [Fact] void should_be_the_same_key() => _result.ShouldBeTrue();
}
