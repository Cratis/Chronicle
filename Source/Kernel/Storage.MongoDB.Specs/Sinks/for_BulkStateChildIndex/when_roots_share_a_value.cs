// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_roots_share_a_value : an_index_over_a_cache
{
    [Fact] void should_return_candidates_in_cache_enumeration_order()
    {
        foreach (var key in new[] { "k3", "k1", "k2", "k5", "k4" })
        {
            Put(key, Doc(("joinKey", "shared")));
        }

        var found = Find("joinKey", "shared");
        found.ShouldEqual(Scan("joinKey", "shared"));
        found.Length.ShouldEqual(5);
    }
}
