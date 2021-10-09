// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IModel"/>.
    /// </summary>
    public class Model : IModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Model"/> class.
        /// </summary>
        /// <param name="name">The name of model.</param>
        public Model(string name)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public string Name { get; }
    }
}
