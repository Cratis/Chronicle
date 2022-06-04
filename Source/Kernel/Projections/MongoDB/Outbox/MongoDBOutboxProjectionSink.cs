// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSink"/> for projecting to the event outbox, MongoDB based.
/// </summary>
public class MongoDBOutboxProjectionSink : IProjectionSink, IDisposable
{
    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.Outbox;

    /// <inheritdoc/>
    public ProjectionSinkTypeName Name => "MongoDB Outbox";

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task BeginReplay() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndReplay() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<ExpandoObject> FindOrDefault(Key key)
    {
        return Task.FromResult(new ExpandoObject());
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;
}
