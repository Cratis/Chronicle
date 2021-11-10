// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the context of an event when being handled by a <see cref="IProjection"/>.
    /// </summary>
    /// <param name="Event">The <see cref="Event"/> that occurred.</param>
    /// <param name="Changeset">The <see cref="Changeset{Event, ExpandoObject}"/> to build on.</param>
    public record EventContext(Event Event, Changeset<Event, ExpandoObject> Changeset);
}
