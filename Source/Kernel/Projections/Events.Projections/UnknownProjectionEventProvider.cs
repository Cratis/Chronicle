// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Exception that gets thrown when an unknown <see cref="IProjectionResultStore"/> is used.
    /// </summary>
    public class UnknownProjectionEventProvider : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownProjectionEventProvider"/> class.
        /// </summary>
        /// <param name="typeId">The unknown <see cref="ProjectionEventProviderTypeId"/>.</param>
        public UnknownProjectionEventProvider(ProjectionEventProviderTypeId typeId) : base($"Projection event provider type of '{typeId}' is unknown.")
        {
        }
    }
}
