// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.MongoDB.Observation.Indexes;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
public class MongoDBObserverKeyIndexes : IObserverKeyIndexes
{
    /// <inheritdoc/>
    public Task<IObserverKeyIndex> GetFor(
        MicroserviceId microserviceId,
        TenantId tenantId,
        ObserverId observerId,
        EventSequenceId eventSequenceId)
    {
        throw new NotImplementedException();
    }
}
