// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents the stored position of a <see cref="IProjection"/>.
    /// </summary>
    /// <param name="Id">The unique identifier - the <see cref="IProjection"/> identifier.</param>
    /// <param name="Position">The position.</param>
    public record ProjectionPosition(Guid Id, uint Position);
}
