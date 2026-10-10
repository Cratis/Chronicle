// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.given;

public abstract class persistent_registrations : registrations
{
    protected abstract bool UseMongoDB { get; }

    protected override ReactorDefinition RoundTrip(ReactorDefinition definition)
    {
        if (UseMongoDB)
        {
            var document = Storage.MongoDB.Observation.Reactors.ReactorDefinitionConverters.ToMongoDB(definition);
            var deserialized = BsonSerializer.Deserialize<Storage.MongoDB.Observation.Reactors.ReactorDefinition>(document.ToBson());
            return Storage.MongoDB.Observation.Reactors.ReactorDefinitionConverters.ToKernel(deserialized);
        }

        var row = Storage.Sql.EventStores.Reactors.ReactorDefinitionConverters.ToSql(definition);

        // These are the JSON column types used by the SQL storage, not kernel records.
        row.EventTypes = JsonSerializer.Deserialize<Storage.Sql.EventStores.EventTypeWithKeyExpression[]>(JsonSerializer.Serialize(row.EventTypes))!;
        row.Filters = JsonSerializer.Deserialize<Storage.Sql.EventStores.Observers.ObserverFilters>(JsonSerializer.Serialize(row.Filters))!;
        return Storage.Sql.EventStores.Reactors.ReactorDefinitionConverters.ToKernel(row);
    }
}
