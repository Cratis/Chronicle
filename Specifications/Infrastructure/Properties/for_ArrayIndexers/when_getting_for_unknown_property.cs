// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties.for_ArrayIndexers;

public class when_getting_for_unknown_property : given.no_array_indexers
{
    Exception result;
    void Because() => result = Catch.Exception(() => indexers.GetFor(string.Empty));

    [Fact] void should_throw_missing_array_indexer_for_property_path() => result.ShouldBeOfExactType<MissingArrayIndexerForPropertyPath>();
}
