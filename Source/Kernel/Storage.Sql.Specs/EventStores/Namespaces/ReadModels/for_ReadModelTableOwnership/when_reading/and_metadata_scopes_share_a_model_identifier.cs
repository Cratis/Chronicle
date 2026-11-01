// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableOwnership.when_reading;

public class and_metadata_scopes_share_a_model_identifier : given.persisted_metadata
{
    void Establish()
    {
        _scopes.Add(new("store", "another-namespace", _definitions, _history));
        _scopes.Add(new("another-store", "default", _definitions, _history));
    }

    async Task Because() => _claims = await _ownership.Read();

    [Fact] void should_preserve_three_distinct_owners() => _claims.Where(claim => claim.Registered).Select(claim => claim.Owner.Id).Distinct().Count().ShouldEqual(3);
    [Fact] void should_keep_the_originating_namespace() => _claims.Any(claim => claim.Owner.Namespace == "another-namespace" && claim.Owner.EventStoreId == "store").ShouldBeTrue();
    [Fact] void should_keep_the_originating_store() => _claims.Any(claim => claim.Owner.EventStoreId == "another-store" && claim.Owner.Namespace == "default").ShouldBeTrue();
}
