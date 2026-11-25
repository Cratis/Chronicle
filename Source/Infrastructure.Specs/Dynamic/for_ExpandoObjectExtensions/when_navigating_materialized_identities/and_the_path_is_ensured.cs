// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_navigating_materialized_identities;

public class and_the_path_is_ensured : given.children_with_a_subset_sibling
{
    ExpandoObject _result;

    void Because() => _result = _state.EnsurePath(_path, _indexers);

    [Fact] void should_choose_the_full_identity() => _result.ShouldBeSame(_target);
    [Fact] void should_not_duplicate_the_existing_child() => _children.Count.ShouldEqual(2);
}
