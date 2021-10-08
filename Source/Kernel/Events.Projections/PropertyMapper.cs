// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the delegate of an operation that maps data into a target object.
    /// </summary>
    /// <param name="event"><see cref="Event"/> in context.</param>
    /// <param name="target"><see cref="ExpandoObject"/> target to write to.</param>
    public delegate void PropertyMapper(Event @event, ExpandoObject target);
}
