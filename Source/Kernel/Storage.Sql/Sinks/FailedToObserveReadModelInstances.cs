// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The exception that is thrown when the SQL sink cannot observe read-model instances.
/// </summary>
/// <param name="sink">The sink that failed.</param>
/// <param name="readModel">The read model being observed.</param>
/// <param name="container">The container being read.</param>
/// <param name="innerException">The underlying failure.</param>
public class FailedToObserveReadModelInstances(SinkTypeId sink, ReadModelIdentifier readModel, ReadModelContainerName container, Exception innerException)
    : Exception($"Sink '{sink}' failed to observe read model '{readModel}' in container '{container}'.", innerException);
