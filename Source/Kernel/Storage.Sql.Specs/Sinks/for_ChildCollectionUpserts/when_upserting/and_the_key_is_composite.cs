// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildCollectionUpserts.when_upserting;

public class and_the_key_is_composite : given.composite_children
{
    void Because()
    {
        var replacement = Child("tenant", "target");
        ((IDictionary<string, object?>)replacement)["name"] = "updated";
        ChildCollectionUpserts.UpsertChildInList(_children, new ChildValue("id", Key(), replacement));
    }

    [Fact] void should_update_the_target() => ((IDictionary<string, object?>)_target)["name"].ShouldEqual("updated");
    [Fact] void should_keep_the_same_tenant_sibling() => ((IDictionary<string, object?>)_sameTenant)["name"].ShouldEqual("original");
    [Fact] void should_keep_the_same_target_sibling() => ((IDictionary<string, object?>)_sameTarget)["name"].ShouldEqual("original");
    [Fact] void should_not_add_a_duplicate() => _children.Count.ShouldEqual(3);
}
