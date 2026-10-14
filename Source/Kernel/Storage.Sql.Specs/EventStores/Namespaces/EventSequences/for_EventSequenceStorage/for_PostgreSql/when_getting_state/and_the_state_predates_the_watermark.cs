// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.for_PostgreSql.when_getting_state;

[Collection(PostgreSqlCollection.Name)]
public class and_the_state_predates_the_watermark(PostgreSqlFixture fixture) : given.a_postgresql_state_storage(fixture)
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    async Task Establish()
    {
        await using var scope = await _database.Namespace(_eventStore, _namespace);
        scope.DbContext.EventSequences.Add(new EventSequenceState { EventSequenceId = EventSequenceId.Log.Value, SequenceNumber = 42 });
        await scope.DbContext.SaveChangesAsync();
    }

    async Task Because() => _result = await _storage.GetState();

    [Fact] void should_preserve_the_sequence_number() => _result.SequenceNumber.Value.ShouldEqual(42UL);
    [Fact] void should_not_invent_a_watermark() => _result.ConstraintIndexWatermark.ShouldBeNull();
    [Fact] void should_not_invent_a_boundary() => _result.ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] void should_not_invent_a_refusal() => _result.ConstraintIndexCatchUpRefusing.ShouldBeNull();
}
