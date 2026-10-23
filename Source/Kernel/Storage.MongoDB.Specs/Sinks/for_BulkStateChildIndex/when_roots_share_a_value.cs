// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_roots_share_a_value : given.an_index
{
    void Establish()
    {
        Set("first", Children("child"));
        Set("second", Children("child"));
        Find("[children].id", "child");
    }

    void Because()
    {
        Remove("first");
        _matches = Find("[children].id", "child");
    }

    [Fact] void should_keep_the_remaining_root() => _matches.ShouldContainOnly("second");
}
