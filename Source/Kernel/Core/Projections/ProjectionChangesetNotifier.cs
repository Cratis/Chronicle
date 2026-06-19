// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionChangesetNotifier"/> using
/// <see cref="ObserverManager{T}"/> to track and dispatch to subscribed observers.
/// </summary>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
public class ProjectionChangesetNotifier(ILogger<ProjectionChangesetNotifier> logger) : Grain, IProjectionChangesetNotifier
{
    /// <summary>
    /// Observer expiration is kept very long because clients legitimately hold long-lived Watch
    /// subscriptions; expiration is driven by explicit Unsubscribe via the gRPC cancellation
    /// token rather than time, and stale observer references that the underlying transport has
    /// already torn down surface as Notify exceptions which ObserverManager handles internally.
    /// </summary>
    readonly ObserverManager<IProjectionChangesetObserver> _observers = new(TimeSpan.FromDays(365 * 4), logger);

    /// <inheritdoc/>
    public Task Subscribe(IProjectionChangesetObserver observer)
    {
        _observers.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(IProjectionChangesetObserver observer)
    {
        _observers.Unsubscribe(observer);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Notify(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change) =>
        await _observers.Notify(o => o.OnChangeset(namespaceName, readModelKey, readModel, change));
}
