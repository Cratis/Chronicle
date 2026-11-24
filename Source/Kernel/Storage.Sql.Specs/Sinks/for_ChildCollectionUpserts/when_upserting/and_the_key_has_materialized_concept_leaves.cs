// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildCollectionUpserts.when_upserting;

public class and_the_key_has_materialized_concept_leaves : given.materialized_children
{
    ChildValue _replacement;

    void Establish() => _replacement = new("id", _key, new { name = "updated" }.AsExpandoObject());

    void Because() => ChildCollectionUpserts.UpsertChildInList(_children, _replacement);

    [Fact] void should_update_only_the_target() => ((IDictionary<string, object?>)_target)["name"].ShouldEqual("updated");
    [Fact] void should_not_append_a_duplicate() => _children.Count.ShouldEqual(4);
    [Fact] void should_preserve_every_sibling() => new[] { _subset, _ambiguous, _different }.Select(child => ((IDictionary<string, object?>)child)["name"]).ShouldEachConformTo(name => Equals(name, "original"));
}
