// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Changes
{
    /// <summary>
    /// Represents properties that has been changed.
    /// </summary>
    /// <typeparam name="TTarget">Target type.</typeparam>
    /// <param name="State">State after change applied.</param>
    /// <param name="Differences">The differences between initial state and a change.</param>
    public record PropertiesChanged<TTarget>(object State, IEnumerable<PropertyDifference<TTarget>> Differences) : Change(State);
}
