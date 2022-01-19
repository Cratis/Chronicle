// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;

namespace Aksio.Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IPerformBootProcedure"/> for setting up defaults and bindings for MongoDB.
    /// </summary>
    public class BootProcedure : IPerformBootProcedure
    {
        readonly MongoDBDefaults _defaults;

        /// <summary>
        /// Initializes a new instance of the <see cref="BootProcedure"/> class.
        /// </summary>
        /// <param name="defaults"><see cref="MongoDBDefaults"/> to initialize defaults.</param>
        public BootProcedure(MongoDBDefaults defaults)
        {
            _defaults = defaults;
        }

        /// <inheritdoc/>
        public void Perform()
        {
            _defaults.Initialize();
        }
    }
}
