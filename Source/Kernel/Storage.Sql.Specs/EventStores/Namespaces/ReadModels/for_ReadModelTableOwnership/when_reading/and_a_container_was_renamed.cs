// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableOwnership.when_reading;

public class and_a_container_was_renamed : given.persisted_metadata
{
    async Task Establish() => await AddOccurrence();

    async Task Because()
    {
        var read = await _ownership.Read();
        _claims = read.Known;
        _unknown = read.Unknown;
    }

    [Fact] void should_keep_the_original_backup_owner() => _claims.Single(claim => claim.Occurred).Owner.ShouldEqual(
        new ReadModelTableOwner("store", "default", "model", "original-orders", ReadModelTableRole.Backup, "original-orders-old"));
    [Fact] void should_also_include_the_current_registered_live_owner() => _claims.Single(claim => claim.Registered).Owner.ContainerName.ShouldEqual("orders");
}
