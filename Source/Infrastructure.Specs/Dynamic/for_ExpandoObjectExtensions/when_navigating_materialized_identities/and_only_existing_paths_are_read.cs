// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_navigating_materialized_identities;

public class and_only_existing_paths_are_read : given.children_with_a_subset_sibling
{
    ExpandoObject? _result;

    void Because() => _result = _state.TryGetExistingPath(_path, _indexers);

    [Fact] void should_choose_the_full_identity() => _result.ShouldBeSame(_target);
    [Fact] void should_leave_the_siblings_untouched() => _children.Count.ShouldEqual(2);
}
