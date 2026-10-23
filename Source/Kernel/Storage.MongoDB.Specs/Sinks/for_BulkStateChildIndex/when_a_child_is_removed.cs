// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_a_child_is_removed : given.an_index
{
    void Establish()
    {
        Set("root", Children("child"));
        Find("[children].id", "child");
    }

    void Because()
    {
        Set("root", Children());
        _matches = Find("[children].id", "child");
    }

    [Fact] void should_not_find_the_child() => _matches.ShouldBeEmpty();
}
