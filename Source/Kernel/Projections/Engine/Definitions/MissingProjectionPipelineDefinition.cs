// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Exception that gets thrown when a <see cref="ProjectionPipelineDefinition"/> is missing in the system.
    /// </summary>
    public class MissingProjectionPipelineDefinition : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingProjectionPipelineDefinition"/> class.
        /// </summary>
        /// <param name="identifier"><see cref="ProjectionId"/> of the missing identifier.</param>
        public MissingProjectionPipelineDefinition(ProjectionId identifier) : base($"Missing projection pipeline definition for projection with id '{identifier}'")
        {
        }
    }
}
