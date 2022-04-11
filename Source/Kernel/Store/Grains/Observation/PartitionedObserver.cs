// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Orleans;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IPartitionedObserver"/>.
/// </summary>
public class PartitionedObserver : Grain, IPartitionedObserver
{
    ObserverId _observerId = ObserverId.Unspecified;
    IAsyncStream<AppendedEvent>? _stream;
    string _connectionId = string.Empty;

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var _);
        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task OnNext(AppendedEvent @event)
    {
        await _stream!.OnNextAsync(@event);
    }

    /// <inheritdoc/>
    public Task SetConnectionId(string connectionId)
    {
        if (_connectionId != connectionId || _stream == default)
        {
            var streamProvider = GetStreamProvider(WellKnownProviders.ObserverHandlersStreamProvider);
            _stream = streamProvider.GetStream<AppendedEvent>(_observerId, connectionId);
        }
        _connectionId = connectionId!;

        return Task.CompletedTask;
    }
}
