// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationAdmission.when_resetting_an_initialized_database;

[Collection(SqlServerCollection.Name)]
public class on_sqlserver(SqlServerFixture fixture) : given.a_sqlserver_provider(fixture)
{
    Exception _error;

    async Task Establish()
    {
        await Advance(1);
        var audit = new EventStores.Generations.GenerationInitializationStorage(() => Provider.EventStore(Name));
        await audit.TryRecord(new(Name, GenerationInitializationStep.FloorVerified, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.FromUnixTimeSeconds(10000)));
    }

    async Task Because() => _error = await Catch.Exception(() => new SqlKernelStateResetHandler(OptionsForProvider, Provider).Reset());

    [Fact] void should_refuse_the_whole_reset() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_keep_the_floor() => (await Provider.ReadGenerationFloors())[Name].Revision.ShouldEqual(1UL);
    [Fact] async Task should_keep_the_receipt() => (await new EventStores.Generations.GenerationInitializationStorage(() => Provider.EventStore(Name)).GetReceipts()).Count.ShouldEqual(1);
}
#endif
