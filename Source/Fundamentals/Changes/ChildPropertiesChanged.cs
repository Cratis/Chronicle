// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Properties;

namespace Cratis.Changes
{
    /// <summary>
    /// Represents properties that has been changed on a child.
    /// </summary>
    /// <param name="State">State after change applied.</param>
    /// <param name="ChildrenProperty">The property holding the children in the parent object.</param>
    /// <param name="IdentifiedByProperty">The property that identifies the key on the child object.</param>
    /// <param name="Key">Key of the object.</param>
    /// <param name="Differences">The differences between initial state and a change.</param>
    public record ChildPropertiesChanged(ExpandoObject State, Property ChildrenProperty, Property IdentifiedByProperty, object Key, IEnumerable<PropertyDifference> Differences) : Change(State);
}
