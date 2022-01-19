// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Compliance.GDPR
{
    /// <summary>
    /// Represents an implementation of <see cref="IPIIManager"/>.
    /// </summary>
    public class PIIManager : IPIIManager
    {
        readonly IClusterClient _clusterClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="IClusterClient"/> class.
        /// </summary>
        /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
        public PIIManager(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        /// <inheritdoc/>
        public Task CreateAndRegisterKeyFor(EncryptionKeyIdentifier identifier) => _clusterClient.GetGrain<Grains.IPIIManager>(Guid.Empty).CreateAndRegisterKeyFor(identifier);

        /// <inheritdoc/>
        public Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier) => _clusterClient.GetGrain<Grains.IPIIManager>(Guid.Empty).DeleteEncryptionKeyFor(identifier);
    }
}
