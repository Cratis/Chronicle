// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Changes
{
    /// <summary>
    /// Represents properties that has been changed.
    /// </summary>
    /// <param name="State">State after change applied.</param>
    /// <param name="Differences">The differences between initial state and a change.</param>
    public record PropertiesChanged(ExpandoObject State, IEnumerable<PropertyDifference> Differences) : Change(State);
}
