// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Events.Store.Grains;

/// <summary>
/// Defines an observer of <see cref="IEventLog"/>.
/// </summary>
public interface IEventLogObserver : IGrainObserver
{
    /// <summary>
    /// Handle next <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
    void Next(AppendedEvent @event);
}
