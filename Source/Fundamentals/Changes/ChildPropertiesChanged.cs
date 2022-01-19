// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Changes
{
    /// <summary>
    /// Represents properties that has been changed on a child.
    /// </summary>
    /// <typeparam name="TTarget">Target type.</typeparam>
    /// <param name="State">State after change applied.</param>
    /// <param name="ChildrenProperty">The property holding the children in the parent object.</param>
    /// <param name="IdentifiedByProperty">The property that identifies the key on the child object.</param>
    /// <param name="Key">Key of the object.</param>
    /// <param name="Differences">The differences between initial state and a change.</param>
    public record ChildPropertiesChanged<TTarget>(object State, PropertyPath ChildrenProperty, PropertyPath IdentifiedByProperty, object Key, IEnumerable<PropertyDifference<TTarget>> Differences) : Change(State);
}
