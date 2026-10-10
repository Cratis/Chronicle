// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges.given;

public class a_pending_future_save : a_save_changes_step
{
    protected IProjectionFutures _futures;
    protected ISink _writingSink;
    protected SaveChanges _step;
    protected ProjectionFutureId _futureId;
    protected Key _futureKey;
    protected IChangeset<AppendedEvent, ExpandoObject> _futureChangeset;
    protected ProjectionEventContext _result;
    protected readonly List<string> _operations = [];

    protected virtual bool FoldIntoMainWrite => false;

    void Establish()
    {
        _futures = Substitute.For<IProjectionFutures>();
        _writingSink = Substitute.For<ISink>();
        _futureId = ProjectionFutureId.New();
        _futureKey = FoldIntoMainWrite ? _context.Key : new Key("future-key", ArrayIndexers.NoIndexers);
        _futureChangeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _futureChangeset.HasChanges.Returns(true);
        _futureChangeset.Changes.Returns([]);
        _context.Changeset.HasChanges.Returns(false);
        _context.AddPendingFutureSave(_futureKey, _futureChangeset, _futureId);
        _writingSink.ApplyChanges(_futureKey, _futureChangeset, _context.EventSequenceNumber).Returns(_ =>
        {
            _operations.Add("write");
            return Task.FromResult<IEnumerable<FailedPartition>>([]);
        });
        _futures.ResolveFuture(_futureId).Returns(_ =>
        {
            _operations.Add("resolve");
            return Task.CompletedTask;
        });
        _step = new SaveChanges(_writingSink, Substitute.For<IChangesetStorage>(), true, Substitute.For<ILogger<SaveChanges>>(), _futures);
    }
}
