// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents the stored position of a <see cref="IProjection"/>.
    /// </summary>
    /// <param name="Id">The unique identifier.</param>
    /// <param name="Position">The position.</param>
    public record ProjectionPosition(string Id, uint Position);
}
