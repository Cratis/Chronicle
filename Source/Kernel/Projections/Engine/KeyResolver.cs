// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents the delegate for providing a value from an object.
    /// </summary>
    /// <param name="eventProvider"><see cref="IProjectionEventProvider"/> used.</param>
    /// <param name="event">The <see cref="AppendedEvent"/> to resolve from.</param>
    /// <returns>Resolved key.</returns>
    public delegate Task<Key> KeyResolver(IProjectionEventProvider eventProvider, AppendedEvent @event);
}
