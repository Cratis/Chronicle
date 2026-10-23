// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_saving_projection_futures : given.a_cold_start_database
{
    int _count;
    string _id;
    readonly ProjectionFutureId _futureId = ProjectionFutureId.New();

    async Task Because()
    {
        var storage = new ProjectionFuturesStorage(Store, Namespace, _database, new JsonSerializerOptions());
        var future = new ProjectionFuture(_futureId, new ProjectionId("projection"), new AppendedEvent(EventContext.Empty, new ExpandoObject()), PropertyPath.Root, new PropertyPath("children"), new PropertyPath("id"), new PropertyPath("parentId"), new Key("parent", ArrayIndexers.NoIndexers), DateTimeOffset.Parse("2026-01-01T12:00:00Z"));
        await Race(() => storage.Save(future.ProjectionId, future));
        _count = await _namespace.ProjectionFutures.CountAsync();
        _id = (await _namespace.ProjectionFutures.SingleAsync()).Id;
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_future_identity() => _id.ShouldEqual(_futureId.Value.ToString());
}
