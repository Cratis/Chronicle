// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Exception that is thrown when an instance is not possible to resolve.
    /// </summary>
    public class UnableToResolveInstanceForProjection : Exception
    {
        public UnableToResolveInstanceForProjection(ProjectionPath projectionPath) : base($"Projection with path '{projectionPath.Value}' can't resolve the instance to project to.")
        {
        }
    }
}
