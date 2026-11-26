// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceDbContext;

public class when_mapping_pending_catch_up_repairs : Specification
{
    string _columnType;
    NpgsqlDbType _nullParameterType;
    NpgsqlDbType _repairParameterType;

    void Because()
    {
        using var context = new NamespaceDbContext(SpecDbContextOptions.Kernel<NamespaceDbContext>("Host=localhost;Database=catch_up_repairs;Username=test;Password=test"));
        var property = context.Model.FindEntityType(typeof(ObserverState))!.FindProperty(nameof(ObserverState.PendingCatchUpRepairs))!;
        _columnType = property.GetColumnType()!;
        var mapping = property.GetRelationalTypeMapping();
        using var command = new NpgsqlCommand();
        _nullParameterType = ((NpgsqlParameter)mapping.CreateParameter(command, "repairs", null)).NpgsqlDbType;
        _repairParameterType = ((NpgsqlParameter)mapping.CreateParameter(command, "repairs", "[{\"partition\":\"P\",\"from\":10,\"to\":13}]")).NpgsqlDbType;
    }

    [Fact] void should_match_the_migrated_column_type() => _columnType.ShouldEqual("jsonb");
    [Fact] void should_bind_null_repairs_as_jsonb() => _nullParameterType.ShouldEqual(NpgsqlDbType.Jsonb);
    [Fact] void should_bind_pending_repairs_as_jsonb() => _repairParameterType.ShouldEqual(NpgsqlDbType.Jsonb);
}
