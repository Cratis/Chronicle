// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections.for_ProjectionFuturesStorage;

public class when_upgrading_a_legacy_future : given.a_futures_storage
{
    EventContext _read;

    async Task Establish()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("DROP TABLE \"ProjectionFutures\"");
        var original = new Migrations.v15_0_0 { ActiveProvider = context.Database.ProviderName! };
        foreach (var command in context.GetService<IMigrationsSqlGenerator>().Generate(original.UpOperations))
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await context.Database.ExecuteSqlRawAsync(context.GetService<IHistoryRepository>().GetDeleteScript("NS-ProjectionFutures-v19_37_6"));
        await context.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO "ProjectionFutures"
                ("Id", "ProjectionId", "EventSequenceNumber", "EventTypeId", "EventTypeGeneration", "EventSourceId",
                 "EventContentJson", "ParentPath", "ChildPath", "IdentifiedByProperty", "ParentIdentifiedByProperty", "ParentKeyJson", "Created")
            VALUES ({{_future.Id.Value.ToString()}}, 'projection', 42, 'ChildCreated', 3, 'child-42',
                    '{}', '', 'children', 'id', 'parentId', '"parent"', {{DateTimeOffset.Parse("2026-05-12T14:00:00Z")}})
            """);
    }

    async Task Because()
    {
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }
        _read = (await _storage.GetForProjection(_future.ProjectionId)).Single().Event.Context;
    }

    [Fact] void should_restore_the_legacy_context() => _read.ShouldEqual(EventContext.Empty with
    {
        SequenceNumber = 42,
        EventType = new EventType("ChildCreated", 3),
        EventSourceId = "child-42"
    });
}
