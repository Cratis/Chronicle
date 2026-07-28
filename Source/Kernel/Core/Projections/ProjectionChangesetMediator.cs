// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionChangesetMediator"/>.
/// </summary>
[Singleton]
public class ProjectionChangesetMediator : IProjectionChangesetMediator
{
    readonly ConcurrentDictionary<Guid, ChangesetForwarder> _forwarders = new();

    /// <inheritdoc/>
    public void Subscribe(Guid subscriptionId, ChangesetForwarder forwarder) => _forwarders[subscriptionId] = forwarder;

    /// <inheritdoc/>
    public void Unsubscribe(Guid subscriptionId) => _forwarders.TryRemove(subscriptionId, out _);

    /// <inheritdoc/>
    public Task OnChangeset(Guid subscriptionId, EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change) =>
        _forwarders.TryGetValue(subscriptionId, out var forwarder)
            ? forwarder(namespaceName, readModelKey, readModel, change)
            : Task.CompletedTask;
}
