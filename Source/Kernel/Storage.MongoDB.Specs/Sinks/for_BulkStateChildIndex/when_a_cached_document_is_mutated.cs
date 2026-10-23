// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_a_cached_document_is_mutated : given.an_index
{
    void Establish()
    {
        Set("root", Children("old"));
        Find("[children].id", "old");
    }

    void Because()
    {
        ((IDictionary<string, object?>)_states["root"])["children"] = new[] { Document(("id", "new")) };
        Set("root", _states["root"]);
        _matches = Find("[children].id", "old");
    }

    [Fact] void should_remove_the_previously_indexed_value() => _matches.ShouldBeEmpty();
}
