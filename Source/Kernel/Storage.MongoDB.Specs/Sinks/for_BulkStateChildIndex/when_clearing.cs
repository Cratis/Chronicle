// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_clearing : given.an_index
{
    void Establish()
    {
        Set("root", Children("child"));
        Find("[children].id", "child");
    }

    void Because()
    {
        _states.Clear();
        _index.Clear();
        Set("new", Children("new-child"));
        _matches = Find("[children].id", "child");
    }

    [Fact] void should_not_keep_old_entries() => _matches.ShouldBeEmpty();
}
