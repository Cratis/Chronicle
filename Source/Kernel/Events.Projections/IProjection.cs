// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a projection.
    /// </summary>
    public interface IProjection
    {
        /// <summary>
        /// Gets the <see cref="IObservable{T}">observable</see> <see cref="EventContext">event</see>.
        /// </summary>
        IObservable<EventContext> Event { get; }
    }
}
