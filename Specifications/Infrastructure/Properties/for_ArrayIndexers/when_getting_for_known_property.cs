// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties.for_ArrayIndexers;

public class when_getting_for_known_property : given.two_array_indexers
{
    ArrayIndexer result;
    void Because() => result = indexers.GetFor(second_indexer_property);

    [Fact] void should_return_the_correct_indexer() => result.ShouldEqual(second_indexer);
}
