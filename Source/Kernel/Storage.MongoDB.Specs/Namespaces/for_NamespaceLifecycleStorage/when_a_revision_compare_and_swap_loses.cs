// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceLifecycleStorage;

public class when_a_revision_compare_and_swap_loses : given.bson_storage
{
    bool _won;

    void Establish()
    {
        _matches = 0;
        _modified = 0;
    }

    async Task Because() => _won = await new NamespaceLifecycleStorage(_collection).TryWrite(_name, 41, new NamespaceLifecycle(_name, _identity, DateTimeOffset.UtcNow) { Revision = 42 });

    [Fact] void should_not_claim_the_transition() => _won.ShouldBeFalse();
    [Fact] void should_compare_the_exact_revision() => _filter["revision"].AsInt64.ShouldEqual(41L);
    [Fact] void should_share_the_control_key_across_case_variants() => _filter["_id"].AsString.ShouldEqual("TENANT");
    [Fact] void should_never_recreate_a_missing_control_row_on_update() => _upsert.ShouldBeFalse();
}
