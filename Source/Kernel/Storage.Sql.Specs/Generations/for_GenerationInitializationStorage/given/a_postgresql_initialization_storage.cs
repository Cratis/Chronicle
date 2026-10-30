// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationInitializationStorage.given;

public class a_postgresql_initialization_storage(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    protected IGenerationInitializationStorage _audit;

    protected GenerationInitializationReceipt Receipt(string evidence = "original") => new(new("orders"), GenerationInitializationStep.FloorVerified, "store", evidence, new(Guid.Parse("93091777-2010-4eba-802b-2dcc773c2d06")), 1, DateTimeOffset.FromUnixTimeSeconds(30000));
    protected GenerationInitializationBlocker Blocker(DateTimeOffset at) => new(new("orders"), GenerationInitializationBlockerKind.ExclusionNotProven, "store", "not proven", at, null);

    void Establish() => _audit = new EventStores.Generations.GenerationInitializationStorage(() => Database.EventStore(Store));
}
