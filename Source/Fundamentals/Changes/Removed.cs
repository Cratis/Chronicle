// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Changes
{
    /// <summary>
    /// Represents the removal of an item.
    /// </summary>
    /// <param name="State">State of the object being removed.</param>
    public record Removed(object State) : Change(State);
}
