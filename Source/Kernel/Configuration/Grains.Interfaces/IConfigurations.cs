// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Configuration.Grains
{
    /// <summary>
    /// Defines a system for working with configurations.
    /// </summary>
    public interface IConfigurations : IGrainWithGuidKey
    {
        /// <summary>
        /// Gets the <see cref="Storage"/> configuration.
        /// </summary>
        /// <returns><see cref="Storage"/> configuration instance.</returns>
        Task<Storage> GetStorage();
    }
}
