// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties.for_ArrayIndexers;

public class when_checking_if_has_for_known_property : given.two_array_indexers
{
    bool result;
    void Because() => result = indexers.HasFor(second_indexer_property);

    [Fact] void should_have_the_indexer() => result.ShouldBeTrue();
}
