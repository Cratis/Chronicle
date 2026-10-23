// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_a_child_moves : given.an_index
{
    void Establish()
    {
        Set("old", Children("child"));
        Find("[children].id", "child");
    }

    void Because()
    {
        Set("old", Children());
        Set("new", Children("child"));
        _matches = Find("[children].id", "child");
    }

    [Fact] void should_find_only_the_new_root() => _matches.ShouldContainOnly("new");
}
