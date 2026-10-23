// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationFloorStorage.given;

public class a_postgresql_floor_storage(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    protected IGenerationFloorStorage _storage;
    protected GenerationFloor _floor = new(new(1), new(1), [GenerationCapability.FloorAdmission], new(Guid.Parse("ef74f125-67aa-4f7c-b365-6c8737a989f0")), 0, DateTimeOffset.MinValue, "", "");
    protected GenerationFloorAdvance Request(GenerationFloor? floor = null, ulong revision = 0) => new(floor ?? _floor, revision, "spec", "explicit request", new(true, "spec members"));
    void Establish() => _storage = CreateStorage();
    protected IGenerationFloorStorage CreateStorage() => new EventStores.Generations.GenerationFloorStorage(() => Database.EventStore(Store));
}
