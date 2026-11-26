// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Orleans.CodeGeneration;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Defines the event store level view of the reducers registered by clients.
/// </summary>
[Version(2)]
public interface IReducersManager : IGrainWithStringKey
{
    /// <summary>
    /// Check whether a stored reducer targets the read model.
    /// </summary>
    /// <param name="readModel">The read model identifier.</param>
    /// <returns>Whether a reducer targets the read model.</returns>
    [AlwaysInterleave]
    Task<bool> HasReducerForReadModel(ReadModelIdentifier readModel);
}
