// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Keys;

namespace Aksio.Cratis.Kernel.Engines.Observation.Indexing;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/>.
/// </summary>
public class ObserverKeyIndex : IObserverKeyIndex
{
    /// <inheritdoc/>
    public Task Add(MicroserviceId microserviceId, TenantId tenantId, Key key) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<IObserverKeys> GetKeysFor(MicroserviceId microserviceId, TenantId tenantId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Rebuild() => throw new NotImplementedException();
}
