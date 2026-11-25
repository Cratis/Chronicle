// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_ChangesetStateNavigator;

public class when_getting_an_element_by_index_beyond_the_collection : Specification
{
    bool _result;

    void Because()
    {
        dynamic state = new ExpandoObject();
        state.items = new List<object> { "first" };
        _result = ChangesetStateNavigator.TryGetValueAtPath(
            (ExpandoObject)state,
            new PropertyPath("[items]"),
            new ArrayIndexers([new ArrayIndexer(new PropertyPath("[items]"), PropertyPath.NotSet, 3)]),
            out _);
    }

    [Fact] void should_not_find_the_value() => _result.ShouldBeFalse();
}
