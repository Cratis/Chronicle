// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines something that can define a projection.
    /// </summary>
    public interface IProjectionDefinition
    {
        /// <summary>
        /// Defines the projection.
        /// </summary>
        /// <param name="builder"><see cref="IProjectionDefinitionBuilder"/> to use for building the definition.</param>
        void Define(IProjectionDefinitionBuilder builder);
    }

}
