// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Exception that gets thrown when an unknown <see cref="IProjectionResultStore"/> is used.
    /// </summary>
    public class UnknownProjectionResultStore : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownProjectionResultStore"/> class.
        /// </summary>
        /// <param name="typeId">The unknown <see cref="ProjectionResultStoreTypeId"/>.</param>
        public UnknownProjectionResultStore(ProjectionResultStoreTypeId typeId) : base($"Projection result store type of '{typeId}' is unknown.")
        {
        }
    }
}
