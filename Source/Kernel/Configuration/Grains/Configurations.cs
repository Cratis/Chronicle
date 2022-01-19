// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Configuration.Grains
{
    /// <summary>
    /// Defines a system for working with configurations.
    /// </summary>
    public class Configurations : Grain, IConfigurations
    {
        readonly Storage _storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configurations"/> class.
        /// </summary>
        /// <param name="storage"><see cref="Storage"/> configuration.</param>
        public Configurations(Storage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// Gets the <see cref="Storage"/> configuration.
        /// </summary>
        /// <returns><see cref="Storage"/> configuration instance.</returns>
        public Task<Storage> GetStorage() => Task.FromResult(_storage);
    }
}
