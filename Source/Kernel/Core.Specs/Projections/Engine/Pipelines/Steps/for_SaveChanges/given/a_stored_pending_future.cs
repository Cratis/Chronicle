// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.InMemory.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges.given;

public class a_stored_pending_future : a_pending_future_save
{
    protected ProjectionFuturesStorage _futureStorage;

    async Task Establish()
    {
        _futureStorage = new ProjectionFuturesStorage();
        var future = new ProjectionFuture(
            _futureId,
            _projection.Identifier,
            _context.Event,
            PropertyPath.Root,
            new PropertyPath("children"),
            new PropertyPath("id"),
            new PropertyPath("parentId"),
            _context.Key,
            DateTimeOffset.UtcNow);
        await _futureStorage.Save(_projection.Identifier, future);
        _futures.ResolveFuture(_futureId).Returns(_ => _futureStorage.Remove(_projection.Identifier, _futureId));
    }
}
