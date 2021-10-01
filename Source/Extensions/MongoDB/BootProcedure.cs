// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Boot;

namespace Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IPerformBootProcedure"/> for setting up defaults and bindings for MongoDB
    /// </summary>
    public class BootProcedure : IPerformBootProcedure
    {
        /// <inheritdoc/>
        public void Perform()
        {
            MongoDBDefaults.Initialize();
        }
    }
}
