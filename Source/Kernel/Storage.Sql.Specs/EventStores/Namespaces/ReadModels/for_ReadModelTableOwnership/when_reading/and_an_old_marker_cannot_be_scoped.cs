// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableOwnership.when_reading;

public class and_an_old_marker_cannot_be_scoped : given.persisted_metadata
{
    Exception _error;

    async Task Establish()
    {
        await AddOccurrence();
        _scopes.Add(new("store", "another-namespace", _definitions, _history));
        _context.ReplayPromotions.Add(new(ReplayPromotion.IdentifierFor("original-orders", "original-orders-old"), "original-orders", "original-orders-old", "old-physical"));
        await _context.SaveChangesAsync();
    }

    async Task Because() => _error = await Catch.Exception(_ownership.Read);

    [Fact] void should_require_recovery_instead_of_inventing_scope() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_preserve_the_marker_owner() => (await _context.ReplayPromotions.AsNoTracking().SingleAsync()).BackupTableName.ShouldEqual("old-physical");
}
