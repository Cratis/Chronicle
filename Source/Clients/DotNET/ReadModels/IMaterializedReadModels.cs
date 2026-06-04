// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a system for working with materialized read model instances.
/// </summary>
public interface IMaterializedReadModels
{
    /// <summary>
    /// Get paginated instances of a read model from the sink.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="skip">Number of instances to skip.</param>
    /// <param name="take">Number of instances to retrieve.</param>
    /// <returns>Collection of read model instances.</returns>
    Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(InstanceCountToSkip skip, InstanceCount take);

    /// <summary>
    /// Observe changes for a paginated window of read model instances.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to observe changes for.</typeparam>
    /// <param name="skip">Number of instances to skip.</param>
    /// <param name="take">Number of instances to observe.</param>
    /// <returns>An observable of <see cref="IEnumerable{TReadModel}"/>.</returns>
    IObservable<IEnumerable<TReadModel>> ObserveInstances<TReadModel>(InstanceCountToSkip skip, InstanceCount take);
}
