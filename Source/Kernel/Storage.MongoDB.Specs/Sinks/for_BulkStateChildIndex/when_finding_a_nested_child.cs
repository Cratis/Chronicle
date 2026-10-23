// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_finding_a_nested_child : given.an_index
{
    void Establish()
    {
        Set("root", Document(("children", new[] { Document(("children", new[] { Document(("id", "child")) })) })));
    }

    void Because()
    {
        _matches = Find("[children].[children].id", "child");
    }

    [Fact] void should_find_the_root() => _matches.ShouldContainOnly("root");
}
