// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the delegate for resolving a key from an <see cref="Event"/>.
    /// </summary>
    /// <param name="event"><see cref="Event"/> to resolve from.</param>
    /// <returns>The key.</returns>
    public delegate object KeyResolver(Event @event);
}
