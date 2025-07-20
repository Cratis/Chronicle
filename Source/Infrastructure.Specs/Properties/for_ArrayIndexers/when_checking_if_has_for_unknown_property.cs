// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_ArrayIndexers;

public class when_checking_if_has_for_unknown_property : given.no_array_indexers
{
    bool _result;
    void Because() => _result = indexers.HasFor(string.Empty);

    [Fact] void should_not_have_the_indexer() => _result.ShouldBeFalse();
}
