// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_the_differences_are_null : a_collapse_specification
{
    IReadOnlyList<PropertyDifference> _result;

    void Because()
    {
        var state = Expando(("contacts", Collection(Expando(("name", "Jane")))));
        _result = ((IEnumerable<PropertyDifference>)null!).Collapse(state, state);
    }

    [Fact] void should_produce_no_differences() => _result.ShouldBeEmpty();
}
