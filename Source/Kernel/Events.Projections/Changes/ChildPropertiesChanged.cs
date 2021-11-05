// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections.Changes
{
    /// <summary>
    /// Represents properties that has been changed on a child.
    /// </summary>
    /// <param name="State">State after change applied.</param>
    /// <param name="childrenProperty">The property holding the children in the parent object.</param>
    /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
    /// <param name="key">Key of the object.</param>
    /// <param name="Differences">The differences between initial state and a change.</param>
    public record ChildPropertiesChanged(ExpandoObject State, Property childrenProperty, Property identifiedByProperty, object key, IEnumerable<PropertyDifference> Differences) : Change(State);
}
