// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

public class and_a_non_identity_unique_index_is_violated : given.a_keyed_batch_storage
{
    Exception _error;

    async Task Establish()
    {
        await using var context = CreateContext();
        var sql = $"CREATE UNIQUE INDEX \"unique_source_{Guid.NewGuid():N}\" ON \"{_tableName}\" (\"EventSourceId\")";
        await context.Database.ExecuteSqlRawAsync(sql);
        (await _storage.AppendPublication(_publication, _event)).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => _error = await Catch.Exception(() => _storage.AppendManyWithIdentities([_event with { SequenceNumber = 1, Identity = new EventPublication("new", "intent") }]));

    [Fact] void should_surface_the_unrelated_unique_violation() => _error.ShouldBeOfExactType<DbUpdateException>();
    [Fact] async Task should_not_record_the_attempted_identity() => (await _storage.TryGetPublications(["new"])).Count.ShouldEqual(0);
}
