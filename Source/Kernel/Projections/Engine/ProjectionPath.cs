// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents the path for a <see cref="Projection"/>.
    /// </summary>
    /// <param name="Path">The path string.</param>
    public record ProjectionPath(string Path) : ConceptAs<string>(Path)
    {
        /// <summary>
        /// Implicitly convert from <see cref="string"/> to <see cref="ProjectionPath"/>.
        /// </summary>
        /// <param name="path">String path.</param>
        public static implicit operator ProjectionPath(string path) => new(path);

        /// <summary>
        /// Get the root path for a projection.
        /// </summary>
        /// <param name="projectionId">Identifier of the projection.</param>
        /// <returns>A root <see cref="ProjectionPath"/>.</returns>
        public static ProjectionPath GetRootFor(ProjectionId projectionId) => $"Root({projectionId})";
    }
}
