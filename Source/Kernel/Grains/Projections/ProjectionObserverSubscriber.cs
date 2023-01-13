// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
public class ProjectionObserverSubscriber : Grain, IProjectionObserverSubscriber
{
    /// <inheritdoc/>
    public Task OnNext(AppendedEvent @event)
    {
        var primaryKey = this.GetPrimaryKey(out var extensions);
        Console.WriteLine($"{primaryKey} - {extensions}");
        return Task.CompletedTask;
    }
}
