// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildIdentifier.when_matching;

public class and_the_key_has_materialized_concept_leaves : given.materialized_children
{
    int _index;

    void Because() => _index = ChildIdentifier.For("id", _key).FindIndex(_children, false);

    [Fact] void should_select_only_the_full_identity() => _index.ShouldEqual(2);
}
