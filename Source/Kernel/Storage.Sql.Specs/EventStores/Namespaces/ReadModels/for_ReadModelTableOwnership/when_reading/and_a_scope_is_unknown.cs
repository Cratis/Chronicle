// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableOwnership.when_reading;

public class and_a_scope_is_unknown : given.persisted_metadata
{
    ReadModelTableClaims _read;

    void Establish() => _scopes.Add(ReadModelTableMetadataScope.Unknown("other-store", null, "visibility", "catalog visibility not established"));

    async Task Because() => _read = await _ownership.Read();

    [Fact] void should_report_the_unknown_scope() => _read.Unknown.Single().EventStoreId.ShouldEqual("other-store");
    [Fact] void should_keep_the_known_claims() => _read.Known.Any(claim => claim.Registered).ShouldBeTrue();
}
