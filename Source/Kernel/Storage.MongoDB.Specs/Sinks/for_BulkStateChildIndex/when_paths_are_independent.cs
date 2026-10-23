// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_paths_are_independent : given.an_index
{
    void Establish()
    {
        Set("root", Document(("id", "root-id"), ("children", new[] { Document(("id", "child-id")) })));
        Find("id", "root-id");
        Find("[children].id", "child-id");
    }

    void Because()
    {
        Set("root", Children("new-child"));
        _matches = Find("id", "root-id");
    }

    [Fact] void should_update_every_materialized_path() => _matches.ShouldBeEmpty();
}
