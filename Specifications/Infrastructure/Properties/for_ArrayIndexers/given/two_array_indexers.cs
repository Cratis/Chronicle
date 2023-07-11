// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties.for_ArrayIndexers.given;

public class two_array_indexers : Specification
{
    protected ArrayIndexers indexers;
    protected PropertyPath first_indexer_property = new("first");
    protected PropertyPath second_indexer_property = new("second");

    protected ArrayIndexer first_indexer;
    protected ArrayIndexer second_indexer;

    void Establish()
    {
        first_indexer = new(first_indexer_property, new(string.Empty), new object());
        second_indexer = new(second_indexer_property, new(string.Empty), new object());
        indexers = new ArrayIndexers(new[] { first_indexer, second_indexer });
    }
}
