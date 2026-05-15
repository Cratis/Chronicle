// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_ArrayIndexers;

public class when_getting_for_property_with_multiple_indexers : Specification
{
    PropertyPath _arrayProperty;
    ArrayIndexer _firstIndexer;
    ArrayIndexer _secondIndexer;
    ArrayIndexers _indexers;

    ArrayIndexer _result;

    void Establish()
    {
        _arrayProperty = "children";
        _firstIndexer = new(_arrayProperty, "id", "first");
        _secondIndexer = new(_arrayProperty, "otherId", "second");
        _indexers = new ArrayIndexers([_firstIndexer, _secondIndexer]);
    }

    void Because() => _result = _indexers.GetFor(_arrayProperty);

    [Fact] void should_keep_all_indexers() => _indexers.Count.ShouldEqual(2);
    [Fact] void should_return_the_last_matching_indexer() => _result.ShouldEqual(_secondIndexer);
}
