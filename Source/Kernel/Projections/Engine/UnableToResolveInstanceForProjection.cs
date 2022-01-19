// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Exception that gets thrown when an instance is not possible to resolve.
    /// </summary>
    public class UnableToResolveInstanceForProjection : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnableToResolveInstanceForProjection"/> class.
        /// </summary>
        /// <param name="projectionPath">Path within the projection.</param>
        public UnableToResolveInstanceForProjection(ProjectionPath projectionPath) : base($"Projection with path '{projectionPath.Value}' can't resolve the instance to project to.")
        {
        }
    }
}
