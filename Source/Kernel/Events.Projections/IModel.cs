// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines the model used by a projection to project to.
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// Gets the name of the model.
        /// </summary>
        string Name { get; }
    }
}
